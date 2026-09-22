import { act, cleanup, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { DesignerDraft } from './AdvancedFormDesigner'
import { createFieldBlock, createTableBlock, createTextBlock } from './formDocumentModel'
import { clearDraftRecovery, formDraftRecoveryKey, readDraftRecovery, useFormDraftRecovery, writeDraftRecovery } from './useFormDraftRecovery'

const baseline: DesignerDraft = {
  name: 'Kontrol', description: '', category: 'Genel', kind: 'Standard', changeSummary: 'İlk sürüm',
  output: { title: '', footerText: '', primaryColor: '#0f766e', includeEmptyFields: false, includeAuditTrail: true, includeSignatures: true },
  schema: { engineVersion: 1, sections: [{ key: 'genel', title: 'Genel', order: 1, columns: 2, fields: [{ key: 'a', label: 'Alan', type: 'shortText', order: 1, width: 12, required: false, options: [] }] }] },
}
const serverJson = JSON.stringify(baseline)
const key = formDraftRecoveryKey('user1', 'form1', 'version1', 1)
const edited = { ...baseline, name: 'Düzenlenen kontrol' }
const initialProps = { identity: 'user1:form1', recoveryKey: key, serverDraft: baseline, enabled: true }

afterEach(() => { cleanup(); sessionStorage.clear(); vi.restoreAllMocks() })

describe('form draft recovery', () => {
  it('restores only the same user/form/version/row and exact baseline', () => {
    writeDraftRecovery(key, serverJson, edited)
    expect(readDraftRecovery(key, serverJson)).toEqual(edited)
    expect(readDraftRecovery(key, JSON.stringify({ ...baseline, category: 'Yeni kategori' }))).toBeNull()
    expect(readDraftRecovery(formDraftRecoveryKey('user2', 'form1', 'version1', 1), serverJson)).toBeNull()
    expect(readDraftRecovery(formDraftRecoveryKey('user1', 'form1', 'version1', 2), serverJson)).toBeNull()
    clearDraftRecovery(key)
    expect(readDraftRecovery(key, serverJson)).toBeNull()
  })

  it('preserves rich text runs, merged cell geometry and column widths', () => {
    const draft = structuredClone(edited)
    const table = createTableBlock([], 2, 2)
    table.cells[0].colSpan = 2
    table.cells[0].rowSpan = 1
    table.columnWidths = [35, 65]
    table.position = { x: 8, y: 15, width: 100, height: 45 }
    const paragraph = createTextBlock([])
    if (paragraph.type !== 'text') throw new Error('Expected text fixture')
    paragraph.runs = [{ text: 'Kalın', style: { ...paragraph.style, bold: true } }]
    paragraph.position = { x: 0, y: 0, width: 80, height: 12 }
    table.cells[0].runs = paragraph.runs
    draft.schema.sections[0].blocks = [{ ...createFieldBlock('a', []), position: { x: 110, y: 0, width: 50, height: 12 } }, paragraph, table]
    writeDraftRecovery(key, serverJson, draft)

    expect(readDraftRecovery(key, serverJson)).toEqual(draft)
  })

  it('ignores malformed storage and unavailable browser storage', () => {
    sessionStorage.setItem(key, '{')
    expect(readDraftRecovery(key, serverJson)).toBeNull()
    sessionStorage.setItem(key, JSON.stringify({ version: 1, baseline: serverJson, draft: { ...edited, schema: { sections: [] } } }))
    expect(readDraftRecovery(key, serverJson)).toBeNull()
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('Full') })
    expect(() => writeDraftRecovery(key, serverJson, edited)).not.toThrow()
  })

  it('retains a dirty in-memory draft across same-version refetch and component remount', () => {
    const { result, rerender, unmount } = renderHook(props => useFormDraftRecovery(props), { initialProps })
    act(() => result.current.setDraft(edited))
    rerender({ ...initialProps, serverDraft: structuredClone(baseline) })

    expect(result.current.draft).toEqual(edited)
    expect(result.current.dirty).toBe(true)
    expect(result.current.stale).toBe(false)
    unmount()
    const remounted = renderHook(() => useFormDraftRecovery(initialProps))
    expect(remounted.result.current.draft).toEqual(edited)
    expect(remounted.result.current.recovered).toBe(true)
  })

  it('keeps unsaved edits when the server revision changes and flags the conflict', () => {
    const { result, rerender } = renderHook(props => useFormDraftRecovery(props), { initialProps })
    act(() => result.current.setDraft(edited))
    const nextBaseline = { ...baseline, description: 'Sunucudaki yeni içerik' }
    const nextKey = formDraftRecoveryKey('user1', 'form1', 'version1', 2)
    rerender({ ...initialProps, recoveryKey: nextKey, serverDraft: nextBaseline })

    expect(result.current.draft).toEqual(edited)
    expect(result.current.stale).toBe(true)
    expect(readDraftRecovery(nextKey, JSON.stringify(nextBaseline))).toBeNull()
    act(() => result.current.reloadServer())
    expect(result.current.draft).toEqual(nextBaseline)
    expect(result.current.dirty).toBe(false)
    expect(result.current.stale).toBe(false)
  })

  it('clears saved recovery and uses the response as the new clean baseline', () => {
    const { result, rerender } = renderHook(props => useFormDraftRecovery(props), { initialProps })
    act(() => result.current.setDraft(edited))
    const nextKey = formDraftRecoveryKey('user1', 'form1', 'version1', 2)
    act(() => result.current.markSaved(edited, nextKey))
    rerender({ ...initialProps, recoveryKey: nextKey, serverDraft: edited })

    expect(sessionStorage.getItem(key)).toBeNull()
    expect(result.current.draft).toEqual(edited)
    expect(result.current.dirty).toBe(false)
    expect(result.current.recovered).toBe(false)
  })

  it('does not recover another identity or read-only session', () => {
    writeDraftRecovery(key, serverJson, edited)
    const { result, rerender } = renderHook(props => useFormDraftRecovery(props), { initialProps: { ...initialProps, enabled: false } })
    expect(result.current.draft).toEqual(baseline)
    expect(result.current.recovered).toBe(false)
    rerender({ ...initialProps, identity: 'user2:form1', recoveryKey: formDraftRecoveryKey('user2', 'form1', 'version1', 1), enabled: true })
    expect(result.current.draft).toEqual(baseline)
    expect(result.current.recovered).toBe(false)
  })

  it('does not let a completed save from a previously opened form overwrite the current draft', () => {
    const { result, rerender } = renderHook(props => useFormDraftRecovery(props), { initialProps })
    act(() => result.current.setDraft(edited))
    const finishOldSave = result.current.markSaved
    const secondKey = formDraftRecoveryKey('user1', 'form2', 'version2', 1)
    const secondBaseline = { ...baseline, name: 'İkinci form' }
    rerender({ ...initialProps, identity: 'user1:form2', recoveryKey: secondKey, serverDraft: secondBaseline })
    const secondDraft = { ...secondBaseline, description: 'Yeni değişiklik' }
    act(() => result.current.setDraft(secondDraft))
    act(() => finishOldSave(edited, formDraftRecoveryKey('user1', 'form1', 'version1', 2)))

    expect(result.current.draft).toEqual(secondDraft)
    expect(result.current.dirty).toBe(true)
    expect(readDraftRecovery(secondKey, JSON.stringify(secondBaseline))).toEqual(secondDraft)
    expect(sessionStorage.getItem(key)).toBeNull()
  })
})
