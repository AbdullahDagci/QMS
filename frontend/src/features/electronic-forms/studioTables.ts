import type { FormTableBlock, FormTableCell } from '../../api/electronicForms'
import { resizeTable } from './formDocumentModel'

export function visibleTableCells(table: FormTableBlock): FormTableCell[] {
  return table.cells.filter(cell => !table.cells.some(anchor => anchor.key !== cell.key
    && cell.row >= anchor.row && cell.row < anchor.row + (anchor.rowSpan ?? 1)
    && cell.column >= anchor.column && cell.column < anchor.column + (anchor.colSpan ?? 1)))
    .sort((a, b) => a.row - b.row || a.column - b.column)
}

export function tableColumnWidths(table: FormTableBlock): number[] {
  return table.columnWidths?.length === table.columns ? table.columnWidths : Array.from({ length: table.columns }, () => 100 / table.columns)
}

export function mergeTableCell(table: FormTableBlock, cellKey: string, direction: 'right' | 'down'): FormTableBlock {
  const next = structuredClone(table)
  const cell = next.cells.find(item => item.key === cellKey)
  if (!cell || cell.fieldKey) return table
  const adjacent = visibleTableCells(next).find(item => direction === 'right'
    ? item.row === cell.row && item.column === cell.column + (cell.colSpan ?? 1) && (item.rowSpan ?? 1) === (cell.rowSpan ?? 1)
    : item.column === cell.column && item.row === cell.row + (cell.rowSpan ?? 1) && (item.colSpan ?? 1) === (cell.colSpan ?? 1))
  if (!adjacent || adjacent.fieldKey) return table
  // Merging across a repeating header boundary cannot be represented in printed tables.
  if (cell.row < table.headerRows && adjacent.row >= table.headerRows) return table
  const separator = cell.text && adjacent.text ? '\n' : ''
  if (cell.text.length + separator.length + adjacent.text.length > 2000) return table
  if (cell.runs?.length || adjacent.runs?.length) cell.runs = [
    ...(cell.runs?.length ? cell.runs : cell.text ? [{ text: cell.text, style: cell.style }] : []),
    ...(separator ? [{ text: separator, style: cell.style }] : []),
    ...(adjacent.runs?.length ? adjacent.runs : adjacent.text ? [{ text: adjacent.text, style: adjacent.style }] : []),
  ]
  cell.text += separator + adjacent.text
  if (direction === 'right') cell.colSpan = (cell.colSpan ?? 1) + (adjacent.colSpan ?? 1)
  else cell.rowSpan = (cell.rowSpan ?? 1) + (adjacent.rowSpan ?? 1)
  adjacent.colSpan = 1
  adjacent.rowSpan = 1
  adjacent.text = ''
  delete adjacent.runs
  return next
}

export function setTableHeaderRows(table: FormTableBlock, headerRows: number): FormTableBlock {
  if (!Number.isInteger(headerRows) || headerRows < 0 || headerRows > table.rows) return table
  if (visibleTableCells(table).some(cell => cell.row < headerRows && cell.row + (cell.rowSpan ?? 1) > headerRows)) return table
  return table.headerRows === headerRows ? table : { ...table, headerRows }
}

export function splitTableCell(table: FormTableBlock, cellKey: string): FormTableBlock {
  return { ...table, cells: table.cells.map(cell => cell.key === cellKey ? { ...cell, rowSpan: 1, colSpan: 1 } : cell) }
}

export function resizeStudioTable(table: FormTableBlock, rows: number, columns: number): FormTableBlock {
  const next = resizeTable(table, Math.trunc(rows) || 1, Math.trunc(columns) || 1)
  next.cells = next.cells.map(cell => ({ ...cell, rowSpan: Math.min(cell.rowSpan ?? 1, next.rows - cell.row), colSpan: Math.min(cell.colSpan ?? 1, next.columns - cell.column) }))
  next.columnWidths = next.columns === table.columns ? tableColumnWidths(table) : Array.from({ length: next.columns }, () => 100 / next.columns)
  return next
}
