import { useEffect, useState } from 'react'
import type { DesignerDraft } from './AdvancedFormDesigner'

interface DraftState {
  identity: string
  key: string
  baseline: string
  draft: DesignerDraft
  recovered: boolean
}

export function formDraftRecoveryKey(userId: string, formId: string, versionId: string, rowVersion: number) {
  return `qms:form-draft:v1:${[userId, formId, versionId, String(rowVersion)].map(encodeURIComponent).join(':')}`
}

export function readDraftRecovery(key: string, baseline: string): DesignerDraft | null {
  try {
    const raw = sessionStorage.getItem(key)
    if (!raw) return null
    const stored: unknown = JSON.parse(raw)
    if (!object(stored) || stored.version !== 1 || stored.baseline !== baseline || !validDraft(stored.draft)) return null
    return stored.draft
  } catch { return null }
}

export function writeDraftRecovery(key: string, baseline: string, draft: DesignerDraft) {
  try {
    if (JSON.stringify(draft) === baseline) sessionStorage.removeItem(key)
    else sessionStorage.setItem(key, JSON.stringify({ version: 1, baseline, draft }))
  } catch { /* An unavailable or full tab store must not interrupt editing. */ }
}

export function clearDraftRecovery(key: string) {
  try { sessionStorage.removeItem(key) } catch { /* Editing also works without tab storage. */ }
}

export function useFormDraftRecovery({ identity, recoveryKey, serverDraft, enabled }: {
  identity: string | null
  recoveryKey: string | null
  serverDraft: DesignerDraft | null
  enabled: boolean
}) {
  const [state, setState] = useState<DraftState | null>(null)
  const serverJson = serverDraft ? JSON.stringify(serverDraft) : ''

  useEffect(() => {
    if (!identity || !recoveryKey || !serverJson) {
      setState(null)
      return
    }
    setState(previous => {
      if (previous?.identity === identity) {
        if (JSON.stringify(previous.draft) !== previous.baseline) return previous
        if (previous.key === recoveryKey && previous.baseline === serverJson) return previous
      }
      const restored = enabled ? readDraftRecovery(recoveryKey, serverJson) : null
      return { identity, key: recoveryKey, baseline: serverJson, draft: restored ?? JSON.parse(serverJson) as DesignerDraft, recovered: Boolean(restored) }
    })
  }, [identity, recoveryKey, serverJson, enabled])

  const setDraft = (draft: DesignerDraft) => setState(previous => {
    if (!previous || previous.identity !== identity || !enabled) return previous
    writeDraftRecovery(previous.key, previous.baseline, draft)
    return { ...previous, draft }
  })

  const markSaved = (draft: DesignerDraft, key: string) => setState(previous => {
    if (recoveryKey) clearDraftRecovery(recoveryKey)
    clearDraftRecovery(key)
    if (!previous || previous.identity !== identity || previous.key !== recoveryKey) return previous
    clearDraftRecovery(previous.key)
    return { ...previous, key, baseline: JSON.stringify(draft), draft, recovered: false }
  })

  const reloadServer = () => {
    if (!identity || !recoveryKey || !serverDraft) return
    setState(previous => {
      if (previous) clearDraftRecovery(previous.key)
      return { identity, key: recoveryKey, baseline: serverJson, draft: structuredClone(serverDraft), recovered: false }
    })
  }

  const dirty = Boolean(state && state.identity === identity && JSON.stringify(state.draft) !== state.baseline)
  return {
    draft: state?.identity === identity ? state.draft : null,
    baseline: state?.baseline ?? '',
    dirty,
    recovered: state?.recovered ?? false,
    stale: Boolean(state && state.identity === identity && dirty && (state.key !== recoveryKey || state.baseline !== serverJson)),
    setDraft,
    markSaved,
    reloadServer,
  }
}

function object(value: unknown): value is Record<string, unknown> { return Boolean(value) && typeof value === 'object' && !Array.isArray(value) }
function number(value: unknown): value is number { return typeof value === 'number' && Number.isFinite(value) }
function optionalText(value: unknown) { return value == null || typeof value === 'string' }
function optionalNumber(value: unknown) { return value == null || number(value) }
function strings(value: Record<string, unknown>, names: string[]) { return names.every(name => typeof value[name] === 'string') }
function numbers(value: Record<string, unknown>, names: string[]) { return names.every(name => number(value[name])) }
function booleans(value: Record<string, unknown>, names: string[]) { return names.every(name => typeof value[name] === 'boolean') }

function validStyle(value: unknown) {
  return object(value) && strings(value, ['fontFamily', 'color', 'backgroundColor']) && number(value.fontSize)
    && booleans(value, ['bold', 'italic', 'underline']) && ['left', 'center', 'right', 'justify'].includes(String(value.alignment))
}
function validRuns(value: unknown) {
  return value === undefined || Array.isArray(value) && value.every(run => object(run) && typeof run.text === 'string' && validStyle(run.style))
}
function validCondition(value: unknown) {
  return value == null || object(value) && typeof value.fieldKey === 'string' && ['equals', 'notEquals', 'hasValue'].includes(String(value.operator))
}
function validField(value: unknown) {
  return object(value) && strings(value, ['key', 'label']) && typeof value.required === 'boolean' && number(value.order)
    && [6, 12].includes(Number(value.width))
    && ['shortText', 'longText', 'number', 'date', 'dateTime', 'singleSelect', 'multiSelect', 'checkbox'].includes(String(value.type))
    && ['helpText', 'placeholder', 'unit'].every(key => optionalText(value[key]))
    && ['min', 'max', 'maxLength'].every(key => optionalNumber(value[key]))
    && Array.isArray(value.options) && value.options.every(option => object(option) && strings(option, ['value', 'label']))
    && validCondition(value.visibilityCondition) && validCondition(value.requiredCondition)
}
function validBlock(value: unknown) {
  if (!object(value) || typeof value.key !== 'string' || !number(value.order)) return false
  if (value.position !== undefined && (!object(value.position) || !numbers(value.position, ['x', 'y', 'width', 'height'])
    || Number(value.position.x) < 0 || Number(value.position.y) < 0 || Number(value.position.width) < 15 || Number(value.position.height) < 5)) return false
  if (value.type === 'field') return typeof value.fieldKey === 'string' && validStyle(value.style)
  if (value.type === 'text') return typeof value.text === 'string' && validStyle(value.style) && validRuns(value.runs)
  if (value.type !== 'table' || !numbers(value, ['rows', 'columns', 'headerRows', 'borderWidth']) || typeof value.borderColor !== 'string') return false
  return (value.columnWidths === undefined || Array.isArray(value.columnWidths) && value.columnWidths.every(number))
    && Array.isArray(value.cells) && value.cells.every(cell => object(cell) && strings(cell, ['key', 'text', 'backgroundColor'])
      && numbers(cell, ['row', 'column']) && optionalText(cell.fieldKey) && validStyle(cell.style)
      && optionalNumber(cell.colSpan) && optionalNumber(cell.rowSpan) && validRuns(cell.runs))
}
function validDraft(value: unknown): value is DesignerDraft {
  if (!object(value) || !strings(value, ['name', 'description', 'category', 'kind', 'changeSummary'])) return false
  if (!object(value.output) || !strings(value.output, ['title', 'footerText', 'primaryColor']) || !booleans(value.output, ['includeEmptyFields', 'includeAuditTrail', 'includeSignatures'])) return false
  if (!object(value.schema) || value.schema.engineVersion !== 1 || !Array.isArray(value.schema.sections) || !value.schema.sections.length) return false
  const document = value.schema.document
  if (document !== undefined && (!object(document) || document.pageSize !== 'A4' || !['portrait', 'landscape'].includes(String(document.orientation))
    || typeof document.defaultFontFamily !== 'string' || !numbers(document, ['defaultFontSize', 'marginTop', 'marginRight', 'marginBottom', 'marginLeft']))) return false
  return value.schema.sections.every(section => object(section) && strings(section, ['key', 'title']) && optionalText(section.description)
    && number(section.order) && [1, 2].includes(Number(section.columns)) && Array.isArray(section.fields) && section.fields.length > 0 && section.fields.every(validField)
    && (section.blocks === undefined || Array.isArray(section.blocks) && section.blocks.every(validBlock)))
}
