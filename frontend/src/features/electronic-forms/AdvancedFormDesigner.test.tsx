import { useState } from 'react'
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FormBlockPosition } from '../../api/electronicForms'
import { AdvancedFormDesigner, type DesignerDraft } from './AdvancedFormDesigner'

const initialDraft: DesignerDraft = {
  name: 'Kontrol formu',
  description: 'Saha kontrolü',
  category: 'Genel',
  kind: 'Standard',
  changeSummary: 'İlk sürüm',
  output: {
    title: '',
    footerText: 'Kontrollü kayıt',
    primaryColor: '#0f766e',
    includeEmptyFields: false,
    includeAuditTrail: true,
    includeSignatures: true,
  },
  schema: {
    engineVersion: 1,
    sections: [{
      key: 'genel',
      title: 'Genel bilgiler',
      description: '',
      order: 1,
      columns: 2,
      fields: [{
        key: 'aciklama',
        label: 'Açıklama',
        type: 'longText',
        required: false,
        width: 12,
        order: 1,
        maxLength: 1000,
        options: [],
        visibilityCondition: null,
        requiredCondition: null,
      }],
    }],
  },
}

function Harness({ onChange }: { onChange?: (draft: DesignerDraft) => void }) {
  const [draft, setDraft] = useState(initialDraft)
  return <AdvancedFormDesigner draft={draft} onChange={value => { setDraft(value); onChange?.(value) }} editable metadataEditable revisionKey="draft:1" />
}

const originalElementFromPoint = document.elementFromPoint
const originalRangeBounds = Object.getOwnPropertyDescriptor(Range.prototype, 'getBoundingClientRect')
const originalRangeRects = Object.getOwnPropertyDescriptor(Range.prototype, 'getClientRects')

beforeEach(() => {
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => null })
  Object.defineProperty(Range.prototype, 'getBoundingClientRect', { configurable: true, value: () => new DOMRect(0, 0, 100, 20) })
  Object.defineProperty(Range.prototype, 'getClientRects', { configurable: true, value: () => [new DOMRect(0, 0, 100, 20)] })
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: originalElementFromPoint })
  if (originalRangeBounds) Object.defineProperty(Range.prototype, 'getBoundingClientRect', originalRangeBounds)
  else Reflect.deleteProperty(Range.prototype, 'getBoundingClientRect')
  if (originalRangeRects) Object.defineProperty(Range.prototype, 'getClientRects', originalRangeRects)
  else Reflect.deleteProperty(Range.prototype, 'getClientRects')
})

function paletteButton(label: string) {
  return screen.getByText(label, { selector: '.studio-palette-label' }).closest('button')!
}

async function addTable(user: ReturnType<typeof userEvent.setup>, rows = 3, columns = 3) {
  await user.click(screen.getByRole('tab', { name: 'Ekle' }))
  await user.click(screen.getByRole('button', { name: 'Tablo' }))
  await user.click(screen.getByRole('button', { name: `${rows} satır ${columns} sütun tablo ekle` }))
}

function selectText(element: HTMLElement, from: number, to: number) {
  const node = document.createTreeWalker(element.querySelector('p')!, NodeFilter.SHOW_TEXT).nextNode()
  expect(node?.nodeType).toBe(Node.TEXT_NODE)
  act(() => {
    element.focus()
    const range = document.createRange()
    range.setStart(node!, from)
    range.setEnd(node!, to)
    const selection = window.getSelection()!
    selection.removeAllRanges()
    selection.addRange(range)
    fireEvent(document, new Event('selectionchange'))
  })
}

function pointAt(element: Element) {
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: () => element })
}

function pointerDrag(handle: Element, target: Element) {
  pointAt(target)
  fireEvent.pointerDown(handle, { pointerId: 7, button: 0, clientX: 40, clientY: 40 })
  fireEvent.pointerMove(handle, { pointerId: 7, buttons: 1, clientX: -10, clientY: -10 })
  fireEvent.pointerUp(handle, { pointerId: 7, button: 0, clientX: -10, clientY: -10 })
}

function freeCanvasGeometry() {
  const body = document.querySelector<HTMLElement>('[data-qms-layout-body]')!
  const block = body.querySelector<HTMLElement>('[data-qms-block-key]')!
  const viewport = body.closest<HTMLElement>('.studio-viewport')!
  const pixelsPerMm = 96 / 25.4 * .75
  vi.spyOn(body, 'getBoundingClientRect').mockImplementation(() => new DOMRect(100, 200, Number(body.dataset.bodyWidth) * pixelsPerMm, Number(body.dataset.bodyHeight) * pixelsPerMm))
  vi.spyOn(viewport, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 1600, 1200))
  vi.spyOn(block, 'getBoundingClientRect').mockImplementation(() => {
    const position: FormBlockPosition = block.dataset.qmsPosition ? JSON.parse(block.dataset.qmsPosition) : { x: 0, y: 20, width: 174, height: 30 }
    return new DOMRect(100 + position.x * pixelsPerMm, 200 + position.y * pixelsPerMm, position.width * pixelsPerMm, position.height * pixelsPerMm)
  })
  return { body, block, pixelsPerMm }
}

function freeDrag(handle: HTMLElement, block: HTMLElement, dx: number, dy: number, pixelsPerMm: number, beforeRelease?: () => void, resize = false) {
  const rect = block.getBoundingClientRect()
  const x = resize ? rect.right - 3 : rect.left - 8
  const y = resize ? rect.bottom - 3 : rect.top + 12
  fireEvent.pointerDown(handle, { pointerId: 42, button: 0, clientX: x, clientY: y })
  fireEvent.pointerMove(document, { pointerId: 42, buttons: 1, clientX: x + dx * pixelsPerMm / 2, clientY: y + dy * pixelsPerMm / 2 })
  fireEvent.pointerMove(document, { pointerId: 42, buttons: 1, clientX: x + dx * pixelsPerMm, clientY: y + dy * pixelsPerMm })
  beforeRelease?.()
  fireEvent.pointerUp(document, { pointerId: 42, button: 0, clientX: x + dx * pixelsPerMm, clientY: y + dy * pixelsPerMm })
}

function expectPhysicalStyle(element: HTMLElement, property: string, mm: number) {
  const value = getComputedStyle(element).getPropertyValue(property)
  expect(Number.parseFloat(value) * (value.endsWith('px') ? 25.4 / 96 : 1)).toBeCloseTo(mm, 2)
}

describe('AdvancedFormDesigner', () => {
  it('adds a palette field to the live canvas and can undo it', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    expect(screen.getByText('1 bölüm · 1 alan')).toBeInTheDocument()
    await user.click(screen.getByText('Sayı'))

    expect(screen.getByText('1 bölüm · 2 alan')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Geri al' })).toBeEnabled()
    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(screen.getByText('1 bölüm · 1 alan')).toBeInTheDocument()
  })

  it('renames and duplicates the selected canvas field', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const label = screen.getByLabelText('Alan etiketi')
    await user.clear(label)
    await user.type(label, 'Kontrol notu')
    expect(screen.getAllByText('Kontrol notu').length).toBeGreaterThan(0)

    await user.click(screen.getByRole('button', { name: /Kopyala/ }))
    expect(screen.getByText('1 bölüm · 2 alan')).toBeInTheDocument()
    expect(screen.getAllByText('Kontrol notu kopyası').length).toBeGreaterThan(0)
  })

  it('accepts a field dragged from the palette onto the canvas', () => {
    render(<Harness />)
    fireEvent.click(screen.getByRole('button', { name: 'Sıralama' }))
    const values = new Map<string, string>()
    const dataTransfer = {
      effectAllowed: 'all',
      dropEffect: 'copy',
      files: [],
      items: [],
      types: [],
      setData: (type: string, value: string) => values.set(type, value),
      getData: (type: string) => values.get(type) ?? '',
      clearData: () => values.clear(),
      setDragImage: () => undefined,
    } as unknown as DataTransfer
    const paletteField = paletteButton('Tarih')
    const dropHint = screen.getByText('Öğeleri tutamağından sürükleyerek yerleştirin')

    expect(paletteField).not.toBeNull()
    fireEvent.dragStart(paletteField!, { dataTransfer })
    fireEvent.dragOver(dropHint, { dataTransfer })
    fireEvent.drop(dropHint, { dataTransfer })

    expect(screen.getByText('1 bölüm · 2 alan')).toBeInTheDocument()
    expect(screen.getAllByText('Tarih').length).toBeGreaterThan(0)
  })

  it('moves a newly added field with the visible canvas controls', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    await user.click(screen.getByRole('button', { name: 'Sıralama' }))

    await user.click(paletteButton('Sayı'))
    await user.click(screen.getByRole('button', { name: 'Sayı öğesini yukarı taşı' }))

    let blocks = document.querySelectorAll('[data-qms-block-index]')
    expect(blocks[0]).toHaveTextContent('Sayı')
    expect(blocks[1]).toHaveTextContent('Açıklama')

    await user.click(screen.getByRole('button', { name: 'Sayı öğesini aşağı taşı' }))
    blocks = document.querySelectorAll('[data-qms-block-index]')
    expect(blocks[0]).toHaveTextContent('Açıklama')
    expect(blocks[1]).toHaveTextContent('Sayı')
  })

  it('reorders a field by dragging its dedicated handle', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    await user.click(screen.getByRole('button', { name: 'Sıralama' }))

    await user.click(paletteButton('Sayı'))
    const target = document.querySelectorAll('[data-qms-block-index]')[0]
    const handle = screen.getByRole('button', { name: 'Sayı öğesini sürükle' })
    pointerDrag(handle, target)

    const blocks = document.querySelectorAll('[data-qms-block-index]')
    expect(blocks[0]).toHaveTextContent('Sayı')
    expect(blocks[1]).toHaveTextContent('Açıklama')

    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    let restored = document.querySelectorAll('[data-qms-block-index]')
    expect(restored[0]).toHaveTextContent('Açıklama')
    expect(restored[1]).toHaveTextContent('Sayı')

    await user.click(screen.getByRole('button', { name: 'İleri al' }))
    restored = document.querySelectorAll('[data-qms-block-index]')
    expect(restored[0]).toHaveTextContent('Sayı')
  })

  it('can drag a field out of a table cell back onto the document', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    await user.click(screen.getByRole('button', { name: 'Sıralama' }))

    await addTable(user)
    await user.click(screen.getByRole('textbox', { name: 'Tablo hücresi 2-1' }))
    await user.click(paletteButton('Tarih'))
    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(8)

    const handle = screen.getByRole('button', { name: 'Tarih alanını sürükle' })
    const end = document.querySelector('[data-qms-drop-end]')
    expect(end).not.toBeNull()
    pointerDrag(handle, end!)

    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(9)
    const blocks = [...document.querySelectorAll('[data-qms-block-index]')]
    expect(blocks.at(-1)).toHaveTextContent('Tarih')

    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(8)
  })

  it('commits both axes after a free drag at the rendered page scale and restores flow in a single undo', async () => {
    const user = userEvent.setup()
    const changed = vi.fn()
    render(<Harness onChange={changed} />)
    expect(screen.getByRole('button', { name: 'Serbest taşıma' })).toHaveAttribute('aria-pressed', 'true')
    const { body, block, pixelsPerMm } = freeCanvasGeometry()
    const handle = screen.getByRole('button', { name: 'Açıklama öğesini sürükle' })
    freeDrag(handle, block, 30, 40, pixelsPerMm, () => {
      expect(changed).not.toHaveBeenCalled()
      expect(block.style.transform).toContain('translate')
    })

    expect(changed).toHaveBeenCalledTimes(1)
    const saved = changed.mock.lastCall![0] as DesignerDraft
    const position = saved.schema.sections[0].blocks![0].position!
    expect(position.x).toBeCloseTo(30)
    expect(position.y).toBeCloseTo(60)
    expect(position.width).toBeCloseTo(90)
    expect(position.height).toBeCloseTo(30)
    expect(getComputedStyle(block).position).toBe('absolute')
    expect(getComputedStyle(body).position).toBe('relative')
    expectPhysicalStyle(block, 'left', 30)
    expectPhysicalStyle(block, 'top', 60)
    expectPhysicalStyle(block, 'width', 90)
    expect(block.style.transform).toBe('')

    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(block).not.toHaveAttribute('data-qms-position')
    expect(getComputedStyle(block).position).toBe('relative')
    expect((changed.mock.lastCall![0] as DesignerDraft).schema.sections[0].blocks?.[0].position).toBeUndefined()
    expect(screen.getByRole('button', { name: 'Geri al' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'İleri al' }))
    expectPhysicalStyle(block, 'left', 30)
    expectPhysicalStyle(block, 'top', 60)
  })

  it('moves a positioned block again, resizes it from the corner, and nudges it with arrow keys', () => {
    const changed = vi.fn()
    render(<Harness onChange={changed} />)
    const { block, pixelsPerMm } = freeCanvasGeometry()
    const handle = screen.getByRole('button', { name: 'Açıklama öğesini sürükle' })
    const position = () => (changed.mock.lastCall![0] as DesignerDraft).schema.sections[0].blocks![0].position!
    freeDrag(handle, block, 25, 30, pixelsPerMm)
    freeDrag(handle, block, 10, -5, pixelsPerMm)
    expect(changed).toHaveBeenCalledTimes(2)
    expect(position().x).toBeCloseTo(35)
    expect(position().y).toBeCloseTo(45)
    expectPhysicalStyle(block, 'left', 35)
    expectPhysicalStyle(block, 'top', 45)

    freeDrag(screen.getByRole('button', { name: 'Öğeyi boyutlandır' }), block, 15, 10, pixelsPerMm, undefined, true)
    expect(changed).toHaveBeenCalledTimes(3)
    expect(position().x).toBeCloseTo(35)
    expect(position().y).toBeCloseTo(45)
    expect(position().width).toBeCloseTo(105)
    expect(position().height).toBeCloseTo(40)
    expectPhysicalStyle(block, 'width', 105)
    expectPhysicalStyle(block, 'min-height', 40)

    fireEvent.keyDown(handle, { key: 'ArrowRight' })
    fireEvent.keyDown(handle, { key: 'ArrowDown', shiftKey: true })
    expect(position().x).toBeCloseTo(36)
    expect(position().y).toBeCloseTo(50)
    expect(position().width).toBeCloseTo(105)
    expect(position().height).toBeCloseTo(40)
  })

  it('creates a Word-like table and places a form field into a selected cell', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await addTable(user)
    expect(screen.getByText('3 × 3 tablo')).toBeInTheDocument()
    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(9)

    await user.click(screen.getByRole('textbox', { name: 'Tablo hücresi 2-1' }))
    await user.click(paletteButton('Tarih'))

    expect(screen.getByText('1 bölüm · 2 alan')).toBeInTheDocument()
    expect(screen.getByText('BAĞLI FORM ALANI')).toBeInTheDocument()
  })

  it('formats an inserted text block from the document toolbar', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Metin' }))
    const text = screen.getByRole('textbox', { name: 'Belge metni' })
    await user.clear(text)
    await user.type(text, 'Kontrollü belge metni')
    await user.click(screen.getByRole('button', { name: 'Kalın' }))

    expect(text).toHaveTextContent('Kontrollü belge metni')
    expect(text.querySelector('strong')).toHaveTextContent('Kontrollü belge metni')
  })

  it('merges neighboring table cells, preserves their text, and can split and undo the change', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    await addTable(user, 2, 3)
    await user.click(screen.getByRole('textbox', { name: 'Tablo hücresi 1-1' }))
    await user.click(screen.getByRole('button', { name: 'Sağ hücreyle birleştir' }))

    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(5)
    const merged = screen.getByRole('textbox', { name: 'Tablo hücresi 1-1' })
    expect(merged).toHaveTextContent('Başlık 1')
    expect(merged).toHaveTextContent('Başlık 2')
    expect(getComputedStyle(merged.closest<HTMLElement>('[data-qms-cell-key]')!).gridColumn.replace(/\s/g, '')).toBe('1/span2')
    expect(screen.queryByRole('textbox', { name: 'Tablo hücresi 1-2' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Hücreyi böl' }))
    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(6)
    expect(screen.getByRole('textbox', { name: 'Tablo hücresi 1-1' })).toHaveTextContent('Başlık 1')
    expect(screen.getByRole('textbox', { name: 'Tablo hücresi 1-1' })).toHaveTextContent('Başlık 2')
    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(screen.getAllByRole('textbox', { name: /Tablo hücresi/ })).toHaveLength(5)
    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(screen.getByRole('textbox', { name: 'Tablo hücresi 1-1' })).toHaveTextContent('Başlık 1')
    expect(screen.getByRole('textbox', { name: 'Tablo hücresi 1-2' })).toHaveTextContent('Başlık 2')
  })

  it('formats only the selected words with the studio toolbar and preserves the remaining text', async () => {
    const user = userEvent.setup()
    render(<Harness />)
    await user.click(screen.getByRole('button', { name: 'Metin' }))
    const text = screen.getByRole('textbox', { name: 'Belge metni' })
    const inspector = screen.getByRole('textbox', { name: 'Metin' })
    expect(inspector).toHaveAttribute('readonly')
    await user.clear(text)
    await user.type(text, 'Kalite kontrol formu')
    expect(inspector).toHaveValue('Kalite kontrol formu')
    selectText(text, 0, 6)
    await user.click(screen.getByRole('button', { name: 'Kalın' }))

    await waitFor(() => expect(text.querySelector('strong')).toHaveTextContent('Kalite'))
    expect(text.querySelector('strong')!.textContent).toBe('Kalite')
    expect(text).toHaveTextContent('Kalite kontrol formu')
    await user.click(screen.getByRole('button', { name: 'Geri al' }))
    expect(text.querySelector('strong')).toBeNull()
    expect(text).toHaveTextContent('Kalite kontrol formu')
    await user.click(screen.getByRole('button', { name: 'İleri al' }))
    expect(text.querySelector('strong')!.textContent).toBe('Kalite')
  })

  it('selects a current block and clears history when a new revision removes the previously selected block', async () => {
    const user = userEvent.setup()
    function ResetHarness() {
      const [draft, setDraft] = useState(initialDraft)
      const [revision, setRevision] = useState('draft:1')
      return <><button onClick={() => { setDraft(initialDraft); setRevision('draft:2') }}>Yeni sürümü yükle</button><AdvancedFormDesigner draft={draft} onChange={setDraft} editable metadataEditable revisionKey={revision} /></>
    }
    render(<ResetHarness />)
    await user.click(screen.getByRole('button', { name: 'Metin' }))
    expect(screen.getByRole('textbox', { name: 'Belge metni' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Yeni sürümü yükle' }))
    expect(screen.queryByRole('textbox', { name: 'Belge metni' })).not.toBeInTheDocument()
    expect(screen.getByLabelText('Alan etiketi')).toHaveValue('Açıklama')
    expect(screen.getByRole('button', { name: 'Kalın' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Geri al' })).toBeDisabled()
  })
})
