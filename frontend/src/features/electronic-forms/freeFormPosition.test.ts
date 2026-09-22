import { describe, expect, it } from 'vitest'
import { bodyMetrics, clampPosition, movePosition, movePositionWithKey, PIXELS_PER_MM, positionFromRect, resizePosition, screenDeltaToMillimeters } from './freeFormPosition'
import { defaultDocumentSettings } from './formDocumentModel'

describe('free form millimeter layout', () => {
  it('uses the printable area below a fixed 34 mm header for both page orientations', () => {
    expect(bodyMetrics()).toEqual({ width: 174, height: 227, left: 18, top: 52, pageWidth: 210, pageHeight: 297, headerReserve: 34 })
    const document = { ...defaultDocumentSettings(), orientation: 'landscape' as const, marginLeft: 10, marginRight: 20, marginTop: 12, marginBottom: 16 }
    expect(bodyMetrics({ engineVersion: 1, sections: [], document })).toEqual({ width: 267, height: 148, left: 10, top: 46, pageWidth: 297, pageHeight: 210, headerReserve: 34 })
  })

  it('converts equal physical drags consistently at different zoom levels', () => {
    for (const scale of [.35, .75, 1, 1.5]) {
      const result = screenDeltaToMillimeters(20 * PIXELS_PER_MM * scale, -8 * PIXELS_PER_MM * scale, scale)
      expect(result.x).toBeCloseTo(20)
      expect(result.y).toBeCloseTo(-8)
    }
  })

  it('materializes DOM rectangles relative to the body instead of the screen or page origin', () => {
    const scale = .75
    const unit = PIXELS_PER_MM * scale
    const position = positionFromRect({ left: 300 + 25 * unit, top: 450 + 40 * unit, width: 70 * unit, height: 22 * unit }, { left: 300, top: 450 }, scale, bodyMetrics())
    expect(position.x).toBeCloseTo(25)
    expect(position.y).toBeCloseTo(40)
    expect(position.width).toBeCloseTo(70)
    expect(position.height).toBeCloseTo(22)
  })

  it('clamps negative coordinates, minimum dimensions, and oversized blocks to the page', () => {
    const metrics = bodyMetrics()
    expect(clampPosition({ x: -50, y: -1, width: 1, height: 0 }, metrics)).toEqual({ x: 0, y: 0, width: 15, height: 5 })
    expect(clampPosition({ x: 160, y: 220, width: 40, height: 30 }, metrics)).toEqual({ x: 134, y: 197, width: 40, height: 30 })
    expect(clampPosition({ x: 10, y: 15, width: 900, height: 900 }, metrics)).toEqual({ x: 0, y: 0, width: 174, height: 227 })
  })

  it('moves freely in both axes and retains dimensions at printable boundaries', () => {
    const position = { x: 20, y: 30, width: 40, height: 15 }
    expect(movePosition(position, { x: 12, y: -8 }, bodyMetrics())).toEqual({ x: 32, y: 22, width: 40, height: 15 })
    expect(movePosition(position, { x: 999, y: 999 }, bodyMetrics())).toEqual({ x: 134, y: 212, width: 40, height: 15 })
    expect(position).toEqual({ x: 20, y: 30, width: 40, height: 15 })
  })

  it('resizes against the lower/right boundary without moving the top-left anchor', () => {
    const position = { x: 120, y: 190, width: 30, height: 20 }
    expect(resizePosition(position, 100, 100, bodyMetrics())).toEqual({ x: 120, y: 190, width: 54, height: 37 })
    expect(resizePosition(position, -1, -1, bodyMetrics())).toEqual({ x: 120, y: 190, width: 15, height: 5 })
  })

  it('supports keyboard nudging with configurable steps and ignores non-direction keys', () => {
    const position = { x: 20, y: 30, width: 40, height: 15 }
    expect(movePositionWithKey(position, 'ArrowRight', bodyMetrics())).toEqual({ ...position, x: 21 })
    expect(movePositionWithKey(position, 'ArrowUp', bodyMetrics(), 10)).toEqual({ ...position, y: 20 })
    expect(movePositionWithKey(position, 'Escape', bodyMetrics())).toBe(position)
  })

  it('never propagates non-finite geometry or zero-scale input', () => {
    expect(clampPosition({ x: NaN, y: Infinity, width: NaN, height: -Infinity }, bodyMetrics())).toEqual({ x: 0, y: 0, width: 15, height: 5 })
    expect(screenDeltaToMillimeters(Infinity, NaN, 0)).toEqual({ x: 0, y: 0 })
    expect(screenDeltaToMillimeters(PIXELS_PER_MM, PIXELS_PER_MM, 0)).toEqual({ x: 1, y: 1 })
    const malformed = bodyMetrics({ ...defaultDocumentSettings(), marginLeft: Infinity, marginRight: 900, marginTop: 900, marginBottom: NaN })
    expect(malformed.width).toBeGreaterThanOrEqual(15)
    expect(malformed.height).toBeGreaterThanOrEqual(5)
    expect(Object.values(malformed).every(Number.isFinite)).toBe(true)
  })
})
