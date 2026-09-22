import { act, cleanup, fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ElectronicFormSchema, FormField, FormTextBlock } from '../../api/electronicForms'
import { DynamicForm, previewPageSx } from './ElectronicFormsWorkspace'
import { createFieldBlock, createTableBlock, defaultDocumentSettings, defaultTextStyle } from './formDocumentModel'

function field(key: string, overrides: Partial<FormField> = {}): FormField {
  return { key, label: key, type: 'shortText', required: false, width: 12, order: 1, options: [], ...overrides }
}
function schema(...fields: FormField[]): ElectronicFormSchema {
  return { engineVersion: 1, sections: [{ key: 'section', title: 'Bilgiler', order: 1, columns: 2, fields }] }
}

function expectMillimeters(element: HTMLElement, property: string, expected: number) {
  const value = getComputedStyle(element).getPropertyValue(property)
  const millimeters = Number.parseFloat(value) * (value.endsWith('px') ? 25.4 / 96 : 1)
  expect(millimeters).toBeCloseTo(expected, 2)
}

function expectPosition(element: HTMLElement, x: number, y: number, width: number) {
  expect(getComputedStyle(element).position).toBe('absolute')
  expectMillimeters(element, 'left', x)
  expectMillimeters(element, 'top', y)
  expectMillimeters(element, 'width', width)
}

afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.unstubAllGlobals() })

describe('form document preview and filling', () => {
  it('keeps optional fields optional and applies required conditions only when they match', () => {
    const model = schema(field('optional'), field('conditional', { requiredCondition: { fieldKey: 'optional', operator: 'equals', value: 'evet' } }), field('always', { required: true }))
    const { rerender } = render(<DynamicForm schema={model} values={{}} onChange={vi.fn()} />)

    expect(screen.getByRole('textbox', { name: 'optional' })).not.toBeRequired()
    expect(screen.getByRole('textbox', { name: 'conditional' })).not.toBeRequired()
    expect(screen.getByRole('textbox', { name: /always/ })).toBeRequired()
    rerender(<DynamicForm schema={model} values={{ optional: 'evet' }} onChange={vi.fn()} />)
    expect(screen.getByRole('textbox', { name: /conditional/ })).toBeRequired()
  })

  it('renders inline text styling and physical font sizes without flattening runs', () => {
    const model = schema(field('a'))
    const style = defaultTextStyle({ fontSize: 12 })
    const paragraph: FormTextBlock = { key: 'paragraph', type: 'text', order: 1, text: 'Kalın Normal', style, runs: [{ text: 'Kalın', style: { ...style, bold: true } }, { text: ' Normal', style }] }
    model.sections[0].blocks = [paragraph, createFieldBlock('a', [])]
    render(<DynamicForm schema={model} values={{}} onChange={vi.fn()} />)

    expect(screen.getByText('Kalın').style.fontWeight).toBe('700')
    expect(screen.getByText('Kalın').style.fontSize).toBe('12pt')
    expect(screen.getByText('Normal').style.fontWeight).toBe('400')
    expect(screen.getByText('Normal').style.fontSize).toBe('12pt')
  })

  it('renders merged cell spans and unequal columns while omitting covered cells', () => {
    const model = schema(field('a'))
    const table = createTableBlock([], 3, 2)
    table.headerRows = 0
    table.columnWidths = [35, 65]
    table.cells[0] = { ...table.cells[0], text: 'Birleşik başlık', colSpan: 2, rowSpan: 2 }
    table.cells[1].text = 'Kapalı hücre'
    table.cells[2].text = 'Kapalı alt hücre'
    table.cells[3].text = 'Kapalı köşe'
    table.cells[4].fieldKey = 'a'
    table.cells[4].text = ''
    model.sections[0].blocks = [table]
    render(<DynamicForm schema={model} values={{ a: 'Değer' }} onChange={vi.fn()} />)

    expect(screen.queryByText(/Kapalı/)).not.toBeInTheDocument()
    const mergedCell = screen.getByText('Birleşik başlık').parentElement!.parentElement!
    expect(getComputedStyle(mergedCell).gridColumn.replace(/\s/g, '')).toBe('1/span2')
    expect(getComputedStyle(mergedCell).gridRow.replace(/\s/g, '')).toBe('1/span2')
    expect(getComputedStyle(mergedCell.parentElement!).gridTemplateColumns.replace(/\s/g, '')).toBe('minmax(0,35fr)minmax(0,65fr)')
    expect(screen.getByRole('textbox', { name: 'a' })).toHaveValue('Değer')
  })

  it('hides conditional table fields without exposing their placeholder cell text', () => {
    const conditional = field('detail', { visibilityCondition: { fieldKey: 'choice', operator: 'equals', value: 'yes' } })
    const model = schema(field('choice'), conditional)
    const table = createTableBlock([], 1, 1)
    table.cells[0].fieldKey = 'detail'
    table.cells[0].text = 'Eski hücre metni'
    model.sections[0].blocks = [createFieldBlock('choice', []), table]
    const { rerender } = render(<DynamicForm schema={model} values={{ choice: 'no' }} onChange={vi.fn()} />)

    expect(screen.queryByRole('textbox', { name: 'detail' })).not.toBeInTheDocument()
    expect(screen.queryByText('Eski hücre metni')).not.toBeInTheDocument()
    rerender(<DynamicForm schema={model} values={{ choice: 'yes', detail: 'Açıklama' }} onChange={vi.fn()} />)
    expect(screen.getByRole('textbox', { name: 'detail' })).toHaveValue('Açıklama')
  })

  it('associates multi-select labels and updates the selected option values', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<DynamicForm schema={schema(field('Kontroller', { type: 'multiSelect', options: [{ value: 'first', label: 'İlk kontrol' }] }))} values={{}} onChange={onChange} />)

    const select = screen.getByRole('combobox', { name: 'Kontroller' })
    expect(select).not.toHaveAttribute('aria-required', 'true')
    await user.click(select)
    await user.click(screen.getByRole('option', { name: 'İlk kontrol' }))
    expect(onChange).toHaveBeenCalledWith({ Kontroller: ['first'] })
  })

  it('displays stored instants in the local timezone and writes changed local values as ISO instants', () => {
    vi.spyOn(Date.prototype, 'getTimezoneOffset').mockReturnValue(-180)
    const onChange = vi.fn()
    render(<DynamicForm schema={schema(field('Kontrol zamanı', { type: 'dateTime' }))} values={{ 'Kontrol zamanı': '2026-09-07T07:30:00.000Z' }} onChange={onChange} />)

    const input = screen.getByLabelText('Kontrol zamanı')
    expect(input).toHaveValue('2026-09-07T10:30')
    fireEvent.change(input, { target: { value: '2026-09-07T11:30' } })
    expect(onChange).toHaveBeenCalledWith({ 'Kontrol zamanı': new Date('2026-09-07T11:30').toISOString() })
  })

  it('uses physical page orientation, margins and points for preview', () => {
    const model = schema(field('a'))
    model.document = { ...defaultDocumentSettings(), orientation: 'landscape', marginTop: 10, marginRight: 12, marginBottom: 14, marginLeft: 16, defaultFontSize: 13 }
    expect(previewPageSx(model)).toMatchObject({ width: '297mm', minHeight: '210mm', padding: '10mm 12mm 14mm 16mm', fontSize: '13pt' })
  })

  it('uses the shared printable-body origin for positioned blocks across sections and retains flow placeholders', () => {
    const model = schema(field('Kontrol kodu'), field('Akış alanı'))
    const positionedField = { ...createFieldBlock('Kontrol kodu', []), position: { x: 14, y: 38, width: 70, height: 22 } }
    model.sections[0].blocks = [positionedField, createFieldBlock('Akış alanı', [])]
    const paragraph: FormTextBlock = { key: 'second-note', type: 'text', order: 1, text: 'Sabit konumlu açıklama', style: defaultTextStyle(), position: { x: 91, y: 80, width: 60, height: 18 } }
    model.sections.push({ key: 'second', title: 'İkinci bölüm', order: 2, columns: 1, fields: [], blocks: [paragraph] })
    render(<DynamicForm schema={model} values={{ 'Kontrol kodu': 'QMS-21' }} onChange={vi.fn()} />)

    const input = screen.getByRole('textbox', { name: 'Kontrol kodu' })
    const fieldPosition = input.closest<HTMLElement>('[data-form-positioned]')!
    expectPosition(fieldPosition, 14, 38, 70)
    expectMillimeters(fieldPosition, 'min-height', 22)
    const body = input.closest<HTMLElement>('[data-form-body]')!
    expect(getComputedStyle(body).position).toBe('relative')
    expect(getComputedStyle(body).overflow).toBe('visible')
    expectMillimeters(body, 'width', 174)
    expectMillimeters(body, 'min-height', 227)
    const notePosition = screen.getByText('Sabit konumlu açıklama').closest<HTMLElement>('[data-form-positioned]')!
    expect(notePosition.closest('[data-form-body]')).toBe(body)
    expectPosition(notePosition, 91, 80, 60)
    const placeholder = body.querySelector<HTMLElement>(`[data-form-position-placeholder="${positionedField.key}"]`)!
    expect(placeholder).toHaveAttribute('aria-hidden', 'true')
    expectMillimeters(placeholder, 'min-height', 22)
    expect(screen.getByRole('textbox', { name: 'Akış alanı' }).closest('[data-form-positioned]')).toBeNull()
  })

  it('preserves input updates, conditions and read-only values inside free-positioned fields and tables', () => {
    const conditional = field('Detay', { visibilityCondition: { fieldKey: 'Görünür', operator: 'equals', value: true } })
    const model = schema(field('Kontrol'), conditional)
    const table = { ...createTableBlock([], 1, 1), position: { x: 12, y: 50, width: 145, height: 32 } }
    table.cells[0].fieldKey = 'Kontrol'
    model.sections[0].blocks = [table, { ...createFieldBlock('Detay', []), position: { x: 10, y: 95, width: 140, height: 22 } }]
    const onChange = vi.fn()
    const { rerender } = render(<DynamicForm schema={model} values={{ Kontrol: 'Eski değer', Görünür: false }} onChange={onChange} />)
    expect(screen.queryByRole('textbox', { name: 'Detay' })).not.toBeInTheDocument()
    fireEvent.change(screen.getByRole('textbox', { name: 'Kontrol' }), { target: { value: 'Yeni değer' } })
    expect(onChange).toHaveBeenCalledWith({ Kontrol: 'Yeni değer', Görünür: false })

    rerender(<DynamicForm schema={model} values={{ Kontrol: 'Yeni değer', Görünür: true, Detay: 'Gözlem' }} onChange={onChange} readOnly />)
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
    expectPosition(screen.getByText('Yeni değer').closest<HTMLElement>('[data-form-positioned]')!, 12, 50, 145)
    expectPosition(screen.getByText('Gözlem').closest<HTMLElement>('[data-form-positioned]')!, 10, 95, 140)
  })

  it('leaves legacy flow forms responsive and expands page minimums to include positioned content', () => {
    const model = schema(field('Akış'))
    const { rerender } = render(<DynamicForm schema={model} values={{}} onChange={vi.fn()} />)
    expect(document.querySelector('[data-form-body]')).toBeNull()
    expect(document.querySelector('[data-form-position-placeholder]')).toBeNull()
    model.sections[0].blocks = [{ ...createFieldBlock('Akış', []), position: { x: 0, y: 210, width: 150, height: 30 } }]
    rerender(<DynamicForm schema={model} values={{}} onChange={vi.fn()} />)
    expectMillimeters(document.querySelector<HTMLElement>('[data-form-body]')!, 'min-height', 240)
    expect(previewPageSx(model)).toMatchObject({ minHeight: '310mm', overflow: 'visible' })
  })

  it('extends the body when entered content grows beyond a positioned block’s saved height', () => {
    let notify: (() => void) | undefined
    const observe = vi.fn()
    vi.stubGlobal('ResizeObserver', class {
      constructor(callback: () => void) { notify = callback }
      observe = observe
      disconnect = vi.fn()
    })
    const model = schema(field('Notlar', { type: 'longText' }))
    model.sections[0].blocks = [{ ...createFieldBlock('Notlar', []), position: { x: 0, y: 180, width: 160, height: 35 } }]
    render(<DynamicForm schema={model} values={{ Notlar: 'Uzun açıklama' }} onChange={vi.fn()} />)
    const body = document.querySelector<HTMLElement>('[data-form-body]')!
    const positioned = screen.getByRole('textbox', { name: 'Notlar' }).closest<HTMLElement>('[data-form-positioned]')!
    expect(observe).toHaveBeenCalledWith(positioned)
    vi.spyOn(body, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 100, 600, 850))
    vi.spyOn(positioned, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 100 + 180 * 96 / 25.4, 600, 90 * 96 / 25.4))
    act(() => { notify!() })
    expectMillimeters(body, 'min-height', 270)
    expect(getComputedStyle(body).overflow).toBe('visible')
  })
})
