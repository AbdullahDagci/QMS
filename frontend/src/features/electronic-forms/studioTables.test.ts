import { describe, expect, it } from 'vitest'
import { createTableBlock } from './formDocumentModel'
import { mergeTableCell, resizeStudioTable, setTableHeaderRows, splitTableCell, visibleTableCells } from './studioTables'

describe('document table geometry', () => {
  it('merges and splits without losing cell text', () => {
    const table = createTableBlock([])
    const merged = mergeTableCell(table, table.cells[0].key, 'right')
    expect(merged.cells[0].colSpan).toBe(2)
    expect(merged.cells[0].text).toBe('Başlık 1\nBaşlık 2')
    expect(merged.cells[1].text).toBe('')
    expect(visibleTableCells(merged)).toHaveLength(8)
    expect(visibleTableCells(splitTableCell(merged, table.cells[0].key))).toHaveLength(9)
  })
  it('does not merge over an interactive field or a repeating header boundary', () => {
    const table = createTableBlock([])
    table.cells[1].fieldKey = 'equipment'
    expect(mergeTableCell(table, table.cells[0].key, 'right')).toBe(table)
    expect(mergeTableCell(table, table.cells[0].key, 'down')).toBe(table)
  })
  it('clips spans and updates column widths when resizing', () => {
    const table = createTableBlock([])
    const merged = mergeTableCell(table, table.cells[0].key, 'right')
    const small = resizeStudioTable(merged, 2, 1)
    expect(small.cells).toHaveLength(2)
    expect(small.cells[0].colSpan).toBe(1)
    expect(small.columnWidths).toEqual([100])
  })
  it('does not move a repeating header boundary through a vertically merged cell', () => {
    const table = createTableBlock([])
    const merged = mergeTableCell(table, table.cells[3].key, 'down')
    expect(merged.cells[3].rowSpan).toBe(2)
    expect(setTableHeaderRows(merged, 2)).toBe(merged)
    expect(setTableHeaderRows(merged, 3).headerRows).toBe(3)
    expect(setTableHeaderRows(merged, 0).headerRows).toBe(0)
  })
  it('does not merge text beyond the persisted cell limit', () => {
    const table = createTableBlock([])
    table.cells[0].text = 'a'.repeat(1000)
    table.cells[1].text = 'b'.repeat(1000)
    expect(mergeTableCell(table, table.cells[0].key, 'right')).toBe(table)
    table.cells[1].text = 'b'.repeat(999)
    expect(mergeTableCell(table, table.cells[0].key, 'right').cells[0].text).toHaveLength(2000)
  })
})
