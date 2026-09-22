import type { ElectronicFormSchema, FormBlockPosition, FormDocumentSettings } from '../../api/electronicForms'
import { defaultDocumentSettings } from './formDocumentModel'

export const PIXELS_PER_MM = 96 / 25.4
export const HEADER_RESERVE_MM = 34
export const MIN_BLOCK_WIDTH_MM = 15
export const MIN_BLOCK_HEIGHT_MM = 5

export interface FormBodyMetrics {
  width: number
  height: number
  left: number
  top: number
  pageWidth: number
  pageHeight: number
  headerReserve: number
}

export interface ScreenRect { left: number; top: number; width: number; height: number }
export interface MillimeterDelta { x: number; y: number }
type BodyBounds = Pick<FormBodyMetrics, 'width' | 'height'>

/** The free layout origin is the printable left edge, after the 34 mm header. */
export function bodyMetrics(input?: ElectronicFormSchema | FormDocumentSettings): FormBodyMetrics {
  const defaults = defaultDocumentSettings()
  const document = input && 'sections' in input ? input.document ?? defaults : input ?? defaults
  const pageWidth = document.orientation === 'landscape' ? 297 : 210
  const pageHeight = document.orientation === 'landscape' ? 210 : 297
  const left = clamp(finite(document.marginLeft, defaults.marginLeft), 0, pageWidth - MIN_BLOCK_WIDTH_MM)
  const right = clamp(finite(document.marginRight, defaults.marginRight), 0, pageWidth - left - MIN_BLOCK_WIDTH_MM)
  const marginTop = clamp(finite(document.marginTop, defaults.marginTop), 0, pageHeight - HEADER_RESERVE_MM - MIN_BLOCK_HEIGHT_MM)
  const bottom = clamp(finite(document.marginBottom, defaults.marginBottom), 0, pageHeight - marginTop - HEADER_RESERVE_MM - MIN_BLOCK_HEIGHT_MM)
  return { width: pageWidth - left - right, height: pageHeight - marginTop - bottom - HEADER_RESERVE_MM, left, top: marginTop + HEADER_RESERVE_MM, pageWidth, pageHeight, headerReserve: HEADER_RESERVE_MM }
}

/** Keeps dimensions while bringing a moved block back inside the printable body. */
export function clampPosition(position: FormBlockPosition, metrics: BodyBounds): FormBlockPosition {
  const limits = bounds(metrics)
  const width = clamp(finite(position.width, MIN_BLOCK_WIDTH_MM), Math.min(MIN_BLOCK_WIDTH_MM, limits.width), limits.width)
  const height = clamp(finite(position.height, MIN_BLOCK_HEIGHT_MM), Math.min(MIN_BLOCK_HEIGHT_MM, limits.height), limits.height)
  return {
    x: clamp(finite(position.x, 0), 0, limits.width - width),
    y: clamp(finite(position.y, 0), 0, limits.height - height),
    width,
    height,
  }
}

/** scale is the rendered scale factor: 0.75 means 75% zoom. */
export function screenDeltaToMillimeters(dx: number, dy: number, scale = 1): MillimeterDelta {
  const pixelsPerMm = PIXELS_PER_MM * validScale(scale)
  return { x: finite(dx, 0) / pixelsPerMm, y: finite(dy, 0) / pixelsPerMm }
}

export function positionFromRect(rect: ScreenRect, bodyRect: Pick<ScreenRect, 'left' | 'top'>, scale: number, metrics: BodyBounds): FormBlockPosition {
  const origin = screenDeltaToMillimeters(rect.left - bodyRect.left, rect.top - bodyRect.top, scale)
  const size = screenDeltaToMillimeters(rect.width, rect.height, scale)
  return clampPosition({ x: origin.x, y: origin.y, width: size.x, height: size.y }, metrics)
}

export function movePosition(position: FormBlockPosition, delta: MillimeterDelta, metrics: BodyBounds): FormBlockPosition {
  const current = clampPosition(position, metrics)
  return clampPosition({ ...current, x: current.x + finite(delta.x, 0), y: current.y + finite(delta.y, 0) }, metrics)
}

/** Bottom/right resize keeps the top-left anchor fixed at page boundaries. */
export function resizePosition(position: FormBlockPosition, width: number, height: number, metrics: BodyBounds): FormBlockPosition {
  const current = clampPosition(position, metrics)
  const limits = bounds(metrics)
  return {
    ...current,
    width: clamp(finite(width, current.width), Math.min(MIN_BLOCK_WIDTH_MM, limits.width), limits.width - current.x),
    height: clamp(finite(height, current.height), Math.min(MIN_BLOCK_HEIGHT_MM, limits.height), limits.height - current.y),
  }
}

export function movePositionWithKey(position: FormBlockPosition, key: string, metrics: BodyBounds, step = 1): FormBlockPosition {
  const distance = Number.isFinite(step) && step > 0 ? step : 1
  const delta = key === 'ArrowLeft' ? { x: -distance, y: 0 } : key === 'ArrowRight' ? { x: distance, y: 0 }
    : key === 'ArrowUp' ? { x: 0, y: -distance } : key === 'ArrowDown' ? { x: 0, y: distance } : null
  return delta ? movePosition(position, delta, metrics) : position
}

function validScale(scale: number) { return Number.isFinite(scale) && scale > 0 ? scale : 1 }
function finite(value: number, fallback: number) { return Number.isFinite(value) ? value : fallback }
function clamp(value: number, min: number, max: number) { return Math.max(min, Math.min(max, value)) }
function bounds(metrics: BodyBounds) {
  return { width: Math.max(MIN_BLOCK_WIDTH_MM, finite(metrics.width, MIN_BLOCK_WIDTH_MM)), height: Math.max(MIN_BLOCK_HEIGHT_MM, finite(metrics.height, MIN_BLOCK_HEIGHT_MM)) }
}
