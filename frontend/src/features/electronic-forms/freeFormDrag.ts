import type { FormBlockPosition } from '../../api/electronicForms'
import { clampPosition, MIN_BLOCK_HEIGHT_MM, MIN_BLOCK_WIDTH_MM, PIXELS_PER_MM, positionFromRect, resizePosition, screenDeltaToMillimeters } from './freeFormPosition'

interface FreeDragOptions {
  handle: HTMLElement
  pointerId: number
  clientX: number
  clientY: number
  resize?: boolean
  fromCell?: boolean
  onCommit: (position: FormBlockPosition, cell: HTMLElement | null) => void
  onActive: (active: boolean) => void
}

/** Uses the actual rendered page scale; one gesture creates exactly one history entry. */
export function startFreeFormDrag(options: FreeDragOptions): (() => void) | null {
  const body = options.handle.closest<HTMLElement>('[data-qms-layout-body]')
  const source = options.handle.closest<HTMLElement>(options.fromCell ? '[data-qms-field-preview]' : '[data-qms-block-key]')
  if (!body || !source) return null
  const metrics = { width: Number(body.dataset.bodyWidth), height: Number(body.dataset.bodyHeight) }
  if (!Number.isFinite(metrics.width) || !Number.isFinite(metrics.height) || metrics.width < MIN_BLOCK_WIDTH_MM || metrics.height < MIN_BLOCK_HEIGHT_MM) return null
  const bodyRect = body.getBoundingClientRect()
  const scale = bodyRect.width / (metrics.width * PIXELS_PER_MM)
  if (!(scale > 0) || !Number.isFinite(scale)) return null
  const originalRect = source.getBoundingClientRect()
  if (![originalRect.left, originalRect.top, originalRect.width, originalRect.height].every(Number.isFinite) || originalRect.width <= 0 || originalRect.height <= 0) return null
  const rawOrigin = screenDeltaToMillimeters(originalRect.left - bodyRect.left, originalRect.top - bodyRect.top, scale)
  const initial = positionFromRect(originalRect, bodyRect, scale, metrics)
  let starting = options.resize ? { ...initial } : { ...initial, ...rawOrigin }
  let next = { ...initial }
  const original = { transform: source.style.transform, width: source.style.width, minHeight: source.style.minHeight, zIndex: source.style.zIndex, pointerEvents: source.style.pointerEvents }
  const cursor = document.body.style.cursor
  const userSelect = document.body.style.userSelect
  let active = false
  let done = false
  let lastX = options.clientX
  let lastY = options.clientY
  let scrollFrame = 0
  const viewport = body.closest<HTMLElement>('.studio-viewport')
  const feedback = document.createElement('div')
  feedback.className = 'studio-drag-feedback'
  feedback.setAttribute('role', 'status')
  const inside = (x: number, y: number) => {
    const rect = body.getBoundingClientRect()
    if (viewport) {
      const visible = viewport.getBoundingClientRect()
      if (x < visible.left || x > visible.right || y < visible.top || y > visible.bottom) return false
    }
    return x >= rect.left - 36 * scale && x <= rect.right && y >= rect.top && y <= rect.top + metrics.height * PIXELS_PER_MM * scale
  }
  const draw = () => {
    const rect = body.getBoundingClientRect()
    const scrollDelta = screenDeltaToMillimeters(bodyRect.left - rect.left, bodyRect.top - rect.top, scale)
    const delta = screenDeltaToMillimeters(lastX - options.clientX, lastY - options.clientY, scale)
    next = options.resize
      ? resizePosition(starting, starting.width + delta.x + scrollDelta.x, starting.height + delta.y + scrollDelta.y, metrics)
      : clampPosition({ ...starting, x: starting.x + delta.x + scrollDelta.x, y: starting.y + delta.y + scrollDelta.y }, metrics)
    source.style.width = `${next.width * PIXELS_PER_MM}px`
    source.style.minHeight = `${next.height * PIXELS_PER_MM}px`
    source.style.transform = `translate(${(next.x - rawOrigin.x) * PIXELS_PER_MM}px, ${(next.y - rawOrigin.y) * PIXELS_PER_MM}px)${original.transform && original.transform !== 'none' ? ` ${original.transform}` : ''}`
    feedback.textContent = inside(lastX, lastY)
      ? `X ${next.x.toFixed(1)} · Y ${next.y.toFixed(1)} mm  |  ${next.width.toFixed(1)} × ${next.height.toFixed(1)} mm`
      : 'Sayfa dışında — bırakıldığında taşıma iptal edilir'
    feedback.dataset.valid = String(inside(lastX, lastY))
  }
  const autoScroll = () => {
    if (done || !active) return
    if (viewport) {
      const rect = viewport.getBoundingClientRect()
      const speedY = lastY > rect.bottom - 42 ? Math.min(12, (lastY - rect.bottom + 42) / 3)
        : lastY < rect.top + 42 ? -Math.min(12, (rect.top + 42 - lastY) / 3) : 0
      const speedX = lastX > rect.right - 42 ? Math.min(12, (lastX - rect.right + 42) / 3)
        : lastX < rect.left + 42 ? -Math.min(12, (rect.left + 42 - lastX) / 3) : 0
      if (lastX >= rect.left && lastX <= rect.right && lastY >= rect.top && lastY <= rect.bottom && (speedX || speedY)) {
        viewport.scrollTop = Math.max(0, viewport.scrollTop + speedY)
        viewport.scrollLeft = Math.max(0, viewport.scrollLeft + speedX)
        draw()
      }
    }
    scrollFrame = requestAnimationFrame(autoScroll)
  }
  const cleanup = () => {
    if (done) return
    done = true
    cancelAnimationFrame(scrollFrame)
    document.removeEventListener('pointermove', move)
    document.removeEventListener('pointerup', up)
    document.removeEventListener('pointercancel', cancel)
    document.removeEventListener('keydown', key)
    window.removeEventListener('blur', cancel)
    try { options.handle.releasePointerCapture(options.pointerId) } catch { /* Document listeners cover detached handles. */ }
    Object.assign(source.style, original)
    document.body.style.cursor = cursor
    document.body.style.userSelect = userSelect
    feedback.remove()
    options.onActive(false)
  }
  const move = (event: PointerEvent) => {
    if (done || event.pointerId !== options.pointerId) return
    lastX = event.clientX; lastY = event.clientY
    if (!active && Math.hypot(lastX - options.clientX, lastY - options.clientY) < 4) return
    event.preventDefault()
    if (!active) {
      active = true
      // A full-width flow field becomes a movable text box on its first free drag.
      if (!options.resize && !source.dataset.qmsPosition && source.dataset.qmsBlockType !== 'table') {
        starting.width = Math.min(starting.width, source.dataset.qmsBlockType === 'text' ? 120 : 90)
        source.style.width = `${starting.width * PIXELS_PER_MM}px`
        starting.height = Math.max(starting.height, source.getBoundingClientRect().height / scale / PIXELS_PER_MM)
        starting = { ...clampPosition(starting, metrics), x: rawOrigin.x, y: rawOrigin.y }
      }
      source.style.zIndex = '1000'
      source.style.pointerEvents = 'none'
      document.body.style.cursor = options.resize ? 'nwse-resize' : 'grabbing'
      document.body.style.userSelect = 'none'
      document.body.append(feedback)
      options.onActive(true)
      scrollFrame = requestAnimationFrame(autoScroll)
    }
    draw()
  }
  const up = (event: PointerEvent) => {
    if (done || event.pointerId !== options.pointerId) return
    lastX = event.clientX; lastY = event.clientY
    if (active) draw()
    const commit = active && inside(lastX, lastY)
    const hit = document.elementFromPoint?.(lastX, lastY)?.closest<HTMLElement>('[data-qms-cell-key]') ?? null
    const cell = hit && !source.contains(hit) && body.contains(hit) ? hit : null
    cleanup()
    if (commit) options.onCommit(next, options.resize ? null : cell)
  }
  const cancel = (event: Event) => {
    if ('pointerId' in event && event.pointerId !== options.pointerId) return
    cleanup()
  }
  const key = (event: KeyboardEvent) => { if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); cleanup() } }
  document.addEventListener('pointermove', move, { passive: false })
  document.addEventListener('pointerup', up)
  document.addEventListener('pointercancel', cancel)
  document.addEventListener('keydown', key)
  window.addEventListener('blur', cancel)
  try { options.handle.setPointerCapture(options.pointerId) } catch { /* Document-level listeners remain active. */ }
  return cleanup
}
