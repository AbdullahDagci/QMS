import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { TableProperties } from './AdvancedFormDesigner'
import { createTableBlock } from './formDocumentModel'
import { mergeTableCell } from './studioTables'

afterEach(cleanup)

describe('table header properties', () => {
  it('explains a blocked merged-header boundary and clears the error after a valid change', () => {
    const table = createTableBlock([])
    const merged = mergeTableCell(table, table.cells[3].key, 'down')
    const onChange = vi.fn()
    render(<TableProperties block={merged} editable onResize={vi.fn()} onChange={onChange} onDuplicate={vi.fn()} onDelete={vi.fn()} />)

    const headerRows = screen.getByLabelText('Başlık satırı')
    fireEvent.change(headerRows, { target: { value: '2' } })
    expect(onChange).not.toHaveBeenCalled()
    expect(headerRows).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByText('Önce başlık sınırındaki birleştirilmiş hücreyi bölün.')).toBeInTheDocument()
    fireEvent.change(headerRows, { target: { value: '3' } })
    expect(onChange).toHaveBeenCalledWith({ ...merged, headerRows: 3 })
    expect(headerRows).toHaveAttribute('aria-invalid', 'false')
    expect(screen.queryByText('Önce başlık sınırındaki birleştirilmiş hücreyi bölün.')).not.toBeInTheDocument()
  })
})
