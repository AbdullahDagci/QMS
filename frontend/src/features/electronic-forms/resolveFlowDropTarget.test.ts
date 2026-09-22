import { afterEach, describe, expect, it, vi } from 'vitest'
import { resolveFlowDropTarget } from './resolveFlowDropTarget'

const originalElementFromPoint = Object.getOwnPropertyDescriptor(document, 'elementFromPoint')
afterEach(() => {
  document.body.replaceChildren()
  vi.restoreAllMocks()
  if (originalElementFromPoint) Object.defineProperty(document, 'elementFromPoint', originalElementFromPoint)
  else Reflect.deleteProperty(document, 'elementFromPoint')
})

function layout(halfWidth = false, scale = 1) {
  const viewport = document.createElement('div')
  viewport.className = 'studio-viewport'
  const page = document.createElement('div')
  page.className = 'studio-page'
  const body = document.createElement('div')
  body.dataset.qmsLayoutBody = 'true'
  const section = document.createElement('section')
  section.dataset.qmsSectionKey = 'first'
  section.dataset.qmsSectionIndex = '0'
  section.dataset.qmsBlockCount = '3'
  const rectangles = halfWidth ? [[200, 200, 280, 70], [520, 200, 280, 70], [200, 300, 600, 70]]
    : [[200, 200, 600, 60], [200, 280, 600, 90], [200, 390, 600, 80]]
  const bounds = (element: HTMLElement, values: number[]) => vi.spyOn(element, 'getBoundingClientRect').mockReturnValue(new DOMRect(...values.map(value => value * scale) as [number, number, number, number]))
  bounds(viewport, [0, 0, 1000, 1000])
  bounds(page, [100, 50, 800, 1100])
  bounds(body, [200, 140, 600, 950])
  bounds(section, [200, 160, 600, 370])
  const blocks = rectangles.map((rect, index) => {
    const block = document.createElement('div')
    block.dataset.qmsBlockKey = `block${index}`
    block.dataset.qmsBlockIndex = String(index)
    bounds(block, rect)
    section.append(block)
    return block
  })
  body.append(section)
  page.append(body)
  viewport.append(page)
  document.body.append(viewport)
  const hit = vi.fn(() => page as Element | null)
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: hit })
  const resolve = (x: number, y: number, kind: 'block' | 'placement' = 'block') => resolveFlowDropTarget(x * scale, y * scale, { kind, blockKey: 'block2', fieldKey: kind === 'placement' ? 'field' : undefined })
  return { page, section, blocks, hit, resolve, bounds }
}

describe('flow drag geometry', () => {
  it.each([.5, 1, 1.5])('resolves a vertical drag in the left grip gutter at %s scale', scale => {
    const test = layout(false, scale)
    expect(test.resolve(180, 300)).toEqual({ kind: 'position', sectionIndex: 0, targetIndex: 1, visualId: 'first:1' })
    expect(test.resolve(180, 360)).toEqual({ kind: 'position', sectionIndex: 0, targetIndex: 2, visualId: 'first:2' })
  })

  it('maps the middle gap to its insertion boundary instead of the top of the section', () => {
    const test = layout()
    test.hit.mockReturnValue(test.section)
    expect(test.resolve(500, 275)).toMatchObject({ kind: 'position', targetIndex: 1 })
    expect(test.resolve(500, 380)).toMatchObject({ kind: 'position', targetIndex: 2 })
  })

  it('maps blank page areas above and below content to start and end', () => {
    const test = layout()
    expect(test.resolve(400, 90)).toMatchObject({ kind: 'position', targetIndex: 0 })
    expect(test.resolve(400, 900)).toEqual({ kind: 'position', sectionIndex: 0, targetIndex: 3, visualId: 'first:end' })
  })

  it('uses a half-width row as a unit in the gutter and permits insertion between its columns', () => {
    const test = layout(true)
    expect(test.resolve(180, 210)).toMatchObject({ targetIndex: 0 })
    expect(test.resolve(180, 260)).toMatchObject({ targetIndex: 2 })
    expect(test.resolve(500, 230)).toMatchObject({ targetIndex: 1 })
    expect(test.resolve(750, 230)).toMatchObject({ targetIndex: 2 })
    expect(test.resolve(500, 290)).toMatchObject({ targetIndex: 2 })
  })

  it('rejects the external page background and off-viewport coordinates', () => {
    const test = layout()
    expect(test.resolve(50, 250)).toBeNull()
    expect(test.resolve(950, 250)).toBeNull()
    expect(test.resolve(500, 1050)).toBeNull()
  })

  it('prefers a valid hit cell for fields but not for dragged whole blocks', () => {
    const test = layout()
    const cell = document.createElement('div')
    Object.assign(cell.dataset, { qmsCellKey: 'cell', qmsTableKey: 'table', qmsSectionIndex: '0', qmsCellTarget: 'first:table:cell' })
    test.blocks[1].append(cell)
    test.hit.mockReturnValue(cell)
    expect(test.resolve(500, 300, 'placement')).toEqual({ kind: 'cell', sectionIndex: 0, blockKey: 'table', cellKey: 'cell', visualId: 'first:table:cell' })
    expect(test.resolve(500, 300, 'block')).toMatchObject({ kind: 'position', targetIndex: 1 })
  })

  it('selects the nearest section from page whitespace', () => {
    const test = layout()
    const second = document.createElement('section')
    Object.assign(second.dataset, { qmsSectionKey: 'second', qmsSectionIndex: '1', qmsBlockCount: '1' })
    test.bounds(second, [200, 600, 600, 150])
    const block = document.createElement('div')
    block.dataset.qmsBlockIndex = '0'
    test.bounds(block, [200, 650, 600, 60])
    second.append(block)
    test.page.append(second)

    expect(test.resolve(180, 590)).toEqual({ kind: 'position', sectionIndex: 1, targetIndex: 0, visualId: 'second:0' })
    expect(test.resolve(180, 780)).toEqual({ kind: 'position', sectionIndex: 1, targetIndex: 1, visualId: 'second:end' })
  })
})
