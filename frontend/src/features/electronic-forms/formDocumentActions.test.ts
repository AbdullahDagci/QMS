import { describe, expect, it } from 'vitest'
import type { ElectronicFormSchema, FormField, FormFieldBlock, FormSection, FormTableBlock } from '../../api/electronicForms'
import { bindDocumentField, moveDocumentPlacement } from './formDocumentActions'
import { createFieldBlock, createTableBlock, defaultTextStyle } from './formDocumentModel'

function field(key: string): FormField {
  return { key, label: key, type: 'shortText', required: false, width: 12, order: 1, options: [] }
}

function section(key: string, fieldKeys: string[]): FormSection {
  return {
    key, title: key, order: 1, columns: 2,
    fields: fieldKeys.map(field),
    blocks: fieldKeys.map((fieldKey, index) => ({ ...createFieldBlock(fieldKey, []), order: index + 1 })),
  }
}

function schema(...sections: FormSection[]): ElectronicFormSchema {
  return { engineVersion: 1, sections }
}

function addTable(target: FormSection, key = 'table'): FormTableBlock {
  const table = { ...createTableBlock([], 2, 2), key, order: target.blocks!.length + 1 }
  target.blocks!.push(table)
  return table
}

function bindFixture(target: FormSection, table: FormTableBlock, fieldKey: string, cellIndex: number) {
  const block = target.blocks!.find(item => item.type === 'field' && item.fieldKey === fieldKey) as FormFieldBlock
  target.blocks = target.blocks!.filter(item => item !== block)
  table.cells[cellIndex].fieldKey = fieldKey
  table.cells[cellIndex].text = ''
  table.cells[cellIndex].style = block.style
  target.blocks.forEach((item, index) => { item.order = index + 1 })
}

function placedKeys(target: FormSection): string[] {
  return target.blocks!.flatMap(block => block.type === 'field' ? [block.fieldKey] : block.type === 'table' ? block.cells.flatMap(cell => cell.fieldKey ? [cell.fieldKey] : []) : [])
}

describe('document placement actions', () => {
  it('moves down by one insertion boundary without skipping a field or resetting its identity/style', () => {
    const draft = schema(section('first', ['a', 'b', 'c']))
    const first = draft.sections[0].blocks![0] as FormFieldBlock
    first.style = defaultTextStyle({ fontFamily: 'Georgia', fontSize: 24, italic: true })
    first.position = { x: 20, y: 30, width: 70, height: 15 }
    const snapshot = structuredClone(draft)
    const moved = moveDocumentPlacement(draft, { kind: 'placement', blockKey: first.key, fieldKey: 'a' }, 0, 2)

    expect(placedKeys(moved.sections[0])).toEqual(['b', 'a', 'c'])
    expect(moved.sections[0].blocks![1]).toEqual({ ...first, order: 2 })
    expect(moved.sections[0].blocks!.map(block => block.order)).toEqual([1, 2, 3])
    expect(draft).toEqual(snapshot)
    const returned = moveDocumentPlacement(moved, { kind: 'placement', blockKey: first.key, fieldKey: 'a' }, 0, 0)
    expect(placedKeys(returned.sections[0])).toEqual(['a', 'b', 'c'])
  })

  it('moves an existing field into an occupied cell and releases the prior field with its formatting', () => {
    const first = section('first', ['a', 'b', 'c'])
    const table = addTable(first)
    bindFixture(first, table, 'b', 0)
    table.cells[0].style = defaultTextStyle({ bold: true, fontSize: 18, color: '#123456' })
    const incoming = first.blocks![0] as FormFieldBlock
    incoming.style = defaultTextStyle({ italic: true, fontFamily: 'Georgia' })
    const original = schema(first)
    const snapshot = structuredClone(original)
    const bound = bindDocumentField(original, table.key, table.cells[0].key, 'a')
    const nextTable = bound.sections[0].blocks!.find(block => block.type === 'table') as FormTableBlock
    const released = bound.sections[0].blocks!.at(-1) as FormFieldBlock

    expect(nextTable.cells[0].fieldKey).toBe('a')
    expect(nextTable.cells[0].style).toEqual(incoming.style)
    expect(released.fieldKey).toBe('b')
    expect(released.style).toEqual(table.cells[0].style)
    expect(placedKeys(bound.sections[0]).sort()).toEqual(['a', 'b', 'c'])
    expect(original).toEqual(snapshot)
  })

  it('moves a bound field between cells without losing either table reference', () => {
    const first = section('first', ['a', 'b'])
    const table = addTable(first)
    bindFixture(first, table, 'a', 0)
    const bound = bindDocumentField(schema(first), table.key, table.cells[3].key, 'a')
    const nextTable = bound.sections[0].blocks!.find(block => block.type === 'table') as FormTableBlock

    expect(nextTable.cells[0].fieldKey).toBeNull()
    expect(nextTable.cells[3].fieldKey).toBe('a')
    expect(placedKeys(bound.sections[0]).sort()).toEqual(['a', 'b'])
  })

  it('moves a cell field to the document preserving typography and retaining its source table', () => {
    const first = section('first', ['a', 'b'])
    const table = addTable(first)
    bindFixture(first, table, 'a', 0)
    table.cells[0].style = defaultTextStyle({ fontSize: 16, underline: true })
    const moved = moveDocumentPlacement(schema(first), { kind: 'placement', blockKey: table.key, fieldKey: 'a', cellKey: table.cells[0].key }, 0, 2)
    const blocks = moved.sections[0].blocks!

    expect(blocks.map(block => block.type)).toEqual(['field', 'table', 'field'])
    expect((blocks[1] as FormTableBlock).cells[0].fieldKey).toBeNull()
    expect((blocks[2] as FormFieldBlock).style).toEqual(table.cells[0].style)
    expect(placedKeys(moved.sections[0])).toEqual(['b', 'a'])
  })

  it('unbinds a cell by placing its field immediately after the table', () => {
    const first = section('first', ['a', 'b'])
    const table = addTable(first)
    bindFixture(first, table, 'a', 0)
    const released = bindDocumentField(schema(first), table.key, table.cells[0].key, null)
    const blocks = released.sections[0].blocks!

    expect((blocks[1] as FormTableBlock).cells[0].fieldKey).toBeNull()
    expect((blocks[2] as FormFieldBlock).fieldKey).toBe('a')
    expect((blocks[2] as FormFieldBlock).style).toEqual(table.cells[0].style)
  })

  it('transfers field ownership when binding across sections', () => {
    const first = section('first', ['a', 'b'])
    const second = section('second', ['c'])
    const table = addTable(second)
    const bound = bindDocumentField(schema(first, second), table.key, table.cells[0].key, 'a')

    expect(bound.sections[0].fields.map(item => item.key)).toEqual(['b'])
    expect(bound.sections[1].fields.map(item => item.key)).toEqual(['c', 'a'])
    expect(placedKeys(bound.sections[0])).toEqual(['b'])
    expect(placedKeys(bound.sections[1])).toEqual(['c', 'a'])
    expect(bound.sections.map(item => item.order)).toEqual([1, 2])
  })

  it('moves a table and all its bound fields to another section atomically', () => {
    const first = section('first', ['a', 'b', 'c'])
    const table = addTable(first)
    bindFixture(first, table, 'a', 0)
    bindFixture(first, table, 'b', 1)
    const second = section('second', ['d'])
    const moved = moveDocumentPlacement(schema(first, second), { kind: 'block', blockKey: table.key }, 1, 0)

    expect(moved.sections[0].fields.map(item => item.key)).toEqual(['c'])
    expect(moved.sections[1].fields.map(item => item.key)).toEqual(['d', 'a', 'b'])
    expect(moved.sections[1].blocks![0]).toEqual({ ...table, order: 1 })
    expect(placedKeys(moved.sections[1])).toEqual(['a', 'b', 'd'])
  })

  it('refuses cross-section moves or bindings that would empty a section', () => {
    const first = section('first', ['a'])
    const second = section('second', ['b'])
    const table = addTable(second)
    const original = schema(first, second)

    expect(moveDocumentPlacement(original, { kind: 'placement', blockKey: first.blocks![0].key, fieldKey: 'a' }, 1, 0)).toBe(original)
    expect(bindDocumentField(original, table.key, table.cells[0].key, 'a')).toBe(original)
    bindFixture(first, addTable(first, 'sourceTable'), 'a', 0)
    expect(moveDocumentPlacement(original, { kind: 'block', blockKey: 'sourceTable' }, 1, 0)).toBe(original)
  })

  it('refuses stale payloads and capacity overflow without modifying the source', () => {
    const first = section('first', ['a', 'b'])
    const second = section('second', Array.from({ length: 50 }, (_, index) => `target${index}`))
    const table = addTable(second)
    const original = schema(first, second)

    expect(moveDocumentPlacement(original, { kind: 'placement', blockKey: first.blocks![0].key, fieldKey: 'b' }, 0, 2)).toBe(original)
    expect(moveDocumentPlacement(original, { kind: 'placement', blockKey: first.blocks![0].key, fieldKey: 'a' }, 1, 0)).toBe(original)
    expect(bindDocumentField(original, table.key, table.cells[0].key, 'a')).toBe(original)
  })

  it('materializes legacy fields before changing their placement', () => {
    const first = section('first', ['a', 'b'])
    const key = first.blocks![0].key
    delete first.blocks
    const moved = moveDocumentPlacement(schema(first), { kind: 'placement', blockKey: key, fieldKey: 'a' }, 0, 2)

    expect(placedKeys(moved.sections[0])).toEqual(['b', 'a'])
    expect(moved.document?.pageSize).toBe('A4')
    expect(first.blocks).toBeUndefined()
  })
})
