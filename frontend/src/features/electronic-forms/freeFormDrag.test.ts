import { fireEvent } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FormBlockPosition } from '../../api/electronicForms'
import { startFreeFormDrag } from './freeFormDrag'
import { PIXELS_PER_MM } from './freeFormPosition'

let frames: Map<number, FrameRequestCallback>
let nextFrame: number
let cleanups: Array<() => void>
const originalElementFromPoint = Object.getOwnPropertyDescriptor(document, 'elementFromPoint')

beforeEach(() => {
  frames = new Map()
  nextFrame = 0
  cleanups = []
  vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => { frames.set(++nextFrame, callback); return nextFrame })
  vi.stubGlobal('cancelAnimationFrame', (id: number) => frames.delete(id))
  Object.defineProperty(document, 'elementFromPoint', { configurable: true, value: vi.fn(() => null) })
})

afterEach(() => {
  cleanups.forEach(cleanup => cleanup())
  if (originalElementFromPoint) Object.defineProperty(document, 'elementFromPoint', originalElementFromPoint)
  else Reflect.deleteProperty(document, 'elementFromPoint')
  document.body.replaceChildren()
  document.body.style.cssText = ''
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

function fixture({ scale = .75, position = { x: 20, y: 30, width: 40, height: 20 }, resize = false, fromCell = false }: { scale?: number; position?: FormBlockPosition; resize?: boolean; fromCell?: boolean } = {}) {
  const viewport = document.createElement('div')
  viewport.className = 'studio-viewport'
  const body = document.createElement('div')
  body.dataset.qmsLayoutBody = 'true'
  body.dataset.mode = 'free'
  body.dataset.bodyWidth = '174'
  body.dataset.bodyHeight = '227'
  const source = document.createElement('div')
  source.dataset.qmsBlockKey = 'block'
  source.dataset.qmsBlockType = 'field'
  source.dataset.qmsPosition = 'true'
  if (fromCell) source.dataset.qmsFieldPreview = 'true'
  const handle = document.createElement('button')
  source.append(handle)
  body.append(source)
  viewport.append(body)
  document.body.append(viewport)
  const unit = PIXELS_PER_MM * scale
  vi.spyOn(viewport, 'getBoundingClientRect').mockImplementation(() => new DOMRect(0, 0, 1400, 1400))
  vi.spyOn(body, 'getBoundingClientRect').mockImplementation(() => new DOMRect(200 - viewport.scrollLeft, 200 - viewport.scrollTop, 174 * unit, 227 * unit))
  vi.spyOn(source, 'getBoundingClientRect').mockImplementation(() => {
    const rect = body.getBoundingClientRect()
    const width = source.style.width ? Number.parseFloat(source.style.width) * scale : position.width * unit
    return new DOMRect(rect.left + position.x * unit, rect.top + position.y * unit, width, position.height * unit)
  })
  const startRect = source.getBoundingClientRect()
  const startX = resize ? startRect.right : startRect.left + 2
  const startY = resize ? startRect.bottom : startRect.top + 2
  const onCommit = vi.fn()
  const onActive = vi.fn()
  const begin = () => {
    const cleanup = startFreeFormDrag({ handle, pointerId: 7, clientX: startX, clientY: startY, resize, fromCell, onCommit, onActive })
    if (cleanup) cleanups.push(cleanup)
    return cleanup
  }
  const move = (dx: number, dy: number, pointerId = 7) => fireEvent.pointerMove(document, { pointerId, clientX: startX + dx * unit, clientY: startY + dy * unit })
  const up = (dx: number, dy: number, pointerId = 7) => fireEvent.pointerUp(document, { pointerId, clientX: startX + dx * unit, clientY: startY + dy * unit })
  return { viewport, body, source, handle, begin, move, up, onCommit, onActive, unit, startX, startY }
}

describe('free form pointer drag', () => {
  it.each([.35, .75, 1, 1.5])('moves in both axes correctly at %s page scale and commits once', scale => {
    const drag = fixture({ scale })
    const cleanup = drag.begin()
    expect(cleanup).not.toBeNull()
    drag.move(22, 14)
    expect(drag.onCommit).not.toHaveBeenCalled()
    drag.up(22, 14)
    drag.up(22, 14)
    cleanup!()

    expect(drag.onCommit).toHaveBeenCalledTimes(1)
    const [position, cell] = drag.onCommit.mock.calls[0]
    expect(position.x).toBeCloseTo(42)
    expect(position.y).toBeCloseTo(44)
    expect(position.width).toBeCloseTo(40)
    expect(position.height).toBeCloseTo(20)
    expect(cell).toBeNull()
    expect(drag.onActive.mock.calls).toEqual([[true], [false]])
    expect(document.querySelector('.studio-drag-feedback')).toBeNull()
  })

  it('does not commit a click or a drag released outside the visible body', () => {
    const click = fixture()
    click.begin()
    click.up(0, 0)
    expect(click.onCommit).not.toHaveBeenCalled()
    const outside = fixture()
    outside.begin()
    outside.move(10, 10)
    fireEvent.pointerUp(document, { pointerId: 7, clientX: 1600, clientY: 1600 })
    expect(outside.onCommit).not.toHaveBeenCalled()
    expect(outside.source.style.transform).toBe('')
  })

  it('cancels a release over off-canvas UI even when the body extends under that screen coordinate', () => {
    const drag = fixture()
    vi.spyOn(drag.viewport, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 1400, 400))
    drag.begin()
    drag.move(10, 50)
    drag.up(10, 50)
    expect(drag.onCommit).not.toHaveBeenCalled()
  })

  it.each(['escape', 'blur', 'pointercancel', 'cleanup'])('cancels with %s and restores inline styles and document interaction', action => {
    const drag = fixture()
    drag.source.style.cssText = 'transform: translate(3px, 2px); width: 151px; min-height: 76px; z-index: 2; pointer-events: auto;'
    const original = drag.source.style.cssText
    document.body.style.cursor = 'crosshair'
    document.body.style.userSelect = 'text'
    const cleanup = drag.begin()!
    drag.move(10, 10)
    if (action === 'escape') fireEvent.keyDown(document, { key: 'Escape' })
    else if (action === 'blur') fireEvent(window, new Event('blur'))
    else if (action === 'pointercancel') fireEvent.pointerCancel(document, { pointerId: 7 })
    else cleanup()
    drag.up(10, 10)

    expect(drag.onCommit).not.toHaveBeenCalled()
    expect(drag.source.style.cssText).toBe(original)
    expect(document.body.style.cursor).toBe('crosshair')
    expect(document.body.style.userSelect).toBe('text')
    expect(frames.size).toBe(0)
  })

  it('ignores events from a second pointer including cancellation', () => {
    const drag = fixture()
    drag.begin()
    drag.move(10, 10)
    fireEvent.pointerCancel(document, { pointerId: 8 })
    drag.up(30, 30, 8)
    drag.up(10, 10)
    expect(drag.onCommit).toHaveBeenCalledTimes(1)
    expect(drag.onCommit.mock.calls[0][0].x).toBeCloseTo(30)
  })

  it('keeps the top-left anchor fixed while resizing against the page edge', () => {
    const drag = fixture({ resize: true, position: { x: 120, y: 190, width: 30, height: 20 } })
    drag.begin()
    drag.move(100, 100)
    expect(drag.source.style.transform).toMatch(/^translate\(0px, 0px\)/)
    expect(Number.parseFloat(drag.source.style.width) / PIXELS_PER_MM).toBeCloseTo(54)
    const edge = drag.body.getBoundingClientRect()
    fireEvent.pointerUp(document, { pointerId: 7, clientX: edge.right, clientY: edge.bottom })

    expect(drag.onCommit).toHaveBeenCalledTimes(1)
    expect(drag.onCommit.mock.calls[0][0].x).toBeCloseTo(120)
    expect(drag.onCommit.mock.calls[0][0].y).toBeCloseTo(190)
    expect(drag.onCommit.mock.calls[0][0].width).toBeCloseTo(54)
    expect(drag.onCommit.mock.calls[0][0].height).toBeCloseTo(37)
  })

  it('enforces minimum size without moving the resize anchor', () => {
    const drag = fixture({ resize: true, position: { x: 80, y: 80, width: 50, height: 30 } })
    drag.begin()
    drag.move(-60, -40)
    drag.up(-60, -40)
    expect(drag.onCommit.mock.calls[0][0].x).toBeCloseTo(80)
    expect(drag.onCommit.mock.calls[0][0].y).toBeCloseTo(80)
    expect(drag.onCommit.mock.calls[0][0]).toMatchObject({ width: 15, height: 5 })
  })

  it('uses the actual initial origin instead of a pre-clamped origin when lifting a flow block', () => {
    const drag = fixture({ position: { x: -5, y: 30, width: 50, height: 20 } })
    delete drag.source.dataset.qmsPosition
    drag.begin()
    drag.move(20, 10)
    expect(Number.parseFloat(drag.source.style.transform.slice('translate('.length)) / PIXELS_PER_MM).toBeCloseTo(20)
    drag.up(20, 10)
    expect(drag.onCommit.mock.calls[0][0].x).toBeCloseTo(15)
    expect(drag.onCommit.mock.calls[0][0].y).toBeCloseTo(40)
  })

  it('accounts for horizontal and vertical viewport scroll during a gesture', () => {
    const drag = fixture()
    drag.begin()
    drag.move(10, 10)
    drag.viewport.scrollLeft = 8 * drag.unit
    drag.viewport.scrollTop = 12 * drag.unit
    drag.up(10, 10)

    expect(drag.onCommit.mock.calls[0][0].x).toBeCloseTo(38)
    expect(drag.onCommit.mock.calls[0][0].y).toBeCloseTo(52)
  })

  it('keeps edge scrolling active for a stationary pointer and stops it on cancel', () => {
    const drag = fixture()
    vi.spyOn(drag.viewport, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 550, 550))
    drag.begin()
    fireEvent.pointerMove(document, { pointerId: 7, clientX: 540, clientY: 540 })
    const [id, frame] = frames.entries().next().value!
    frames.delete(id)
    frame(0)
    expect(drag.viewport.scrollTop).toBeGreaterThan(0)
    expect(drag.viewport.scrollLeft).toBeGreaterThan(0)
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(frames.size).toBe(0)
    expect(drag.onCommit).not.toHaveBeenCalled()
  })

  it('reports a destination cell only when it belongs to the active body and not the dragged block', () => {
    const drag = fixture({ fromCell: true })
    const cell = document.createElement('div')
    cell.dataset.qmsCellKey = 'target'
    drag.body.append(cell)
    vi.mocked(document.elementFromPoint).mockReturnValue(cell)
    drag.begin()
    drag.move(15, 15)
    drag.up(15, 15)
    expect(drag.onCommit.mock.calls[0][1]).toBe(cell)
    const self = fixture()
    self.source.append(cell)
    self.begin()
    self.move(15, 15)
    self.up(15, 15)
    expect(self.onCommit.mock.calls[0][1]).toBeNull()
  })

  it('rejects missing and invalid layout geometry without installing a gesture', () => {
    const drag = fixture()
    drag.body.dataset.bodyHeight = 'NaN'
    expect(drag.begin()).toBeNull()
    drag.handle.remove()
    expect(drag.begin()).toBeNull()
    expect(drag.onActive).not.toHaveBeenCalled()
    expect(frames.size).toBe(0)
  })
})
