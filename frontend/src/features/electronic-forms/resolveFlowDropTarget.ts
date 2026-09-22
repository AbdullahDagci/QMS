export interface FlowDragPayload {
  kind: 'block' | 'placement'
  blockKey: string
  fieldKey?: string
  cellKey?: string
}

export type FlowDropTarget =
  | { kind: 'position'; sectionIndex: number; targetIndex: number; visualId: string }
  | { kind: 'cell'; sectionIndex: number; blockKey: string; cellKey: string; visualId: string }

interface MeasuredBlock { index: number; rect: DOMRect }
interface MeasuredRow { top: number; bottom: number; left: number; right: number; blocks: MeasuredBlock[] }

/** Resolves insertion boundaries across the page, including the external drag gutter. */
export function resolveFlowDropTarget(clientX: number, clientY: number, payload: FlowDragPayload, surface?: HTMLElement): FlowDropTarget | null {
  if (!Number.isFinite(clientX) || !Number.isFinite(clientY)) return null
  const hit = document.elementFromPoint?.(clientX, clientY) as HTMLElement | null
  const source = [...document.querySelectorAll<HTMLElement>('[data-qms-block-key]')].find(element => element.dataset.qmsBlockKey === payload.blockKey)
  const scope = surface?.closest<HTMLElement>('.studio-page') ?? surface
    ?? source?.closest<HTMLElement>('.studio-page')
    ?? source?.closest<HTMLElement>('[data-qms-layout-body]')
    ?? hit?.closest<HTMLElement>('.studio-page')
    ?? hit?.closest<HTMLElement>('[data-qms-layout-body]')
    ?? source?.closest<HTMLElement>('[data-qms-section-key]')
    ?? hit?.closest<HTMLElement>('[data-qms-section-key]')
  if (!scope) return null
  const viewport = scope.closest<HTMLElement>('.studio-viewport')
  if (!insideIfMeasurable(scope.getBoundingClientRect(), clientX, clientY)
    || viewport && !insideIfMeasurable(viewport.getBoundingClientRect(), clientX, clientY)) return null

  const explicit = hit?.closest<HTMLElement>('[data-qms-position-index]')
  if (explicit && scope.contains(explicit)) {
    const sectionIndex = Number(explicit.dataset.qmsSectionIndex)
    const targetIndex = Number(explicit.dataset.qmsPositionIndex)
    if (Number.isInteger(sectionIndex) && Number.isInteger(targetIndex)) return { kind: 'position', sectionIndex, targetIndex, visualId: explicit.dataset.qmsVisualId ?? '' }
  }

  if (payload.kind === 'placement') {
    const cell = hit?.closest<HTMLElement>('[data-qms-cell-key]')
    if (cell && scope.contains(cell)) {
      const sectionIndex = Number(cell.dataset.qmsSectionIndex)
      const blockKey = cell.dataset.qmsTableKey
      const cellKey = cell.dataset.qmsCellKey
      if (Number.isInteger(sectionIndex) && blockKey && cellKey) return { kind: 'cell', sectionIndex, blockKey, cellKey, visualId: cell.dataset.qmsCellTarget ?? '' }
    }
  }

  const sections = scope.matches('[data-qms-section-key]') ? [scope] : [...scope.querySelectorAll<HTMLElement>('[data-qms-section-key]')]
  const hitSection = hit?.closest<HTMLElement>('[data-qms-section-key]')
  const section = hitSection && sections.includes(hitSection) ? hitSection : nearestSection(sections, clientY)
  if (!section) return null
  const sectionIndex = Number(section.dataset.qmsSectionIndex)
  const count = Number(section.dataset.qmsBlockCount)
  if (!Number.isInteger(sectionIndex) || !Number.isInteger(count)) return null
  const sectionKey = section.dataset.qmsSectionKey ?? String(sectionIndex)
  const targetAt = (index: number): FlowDropTarget => {
    const targetIndex = Math.max(0, Math.min(count, index))
    return { kind: 'position', sectionIndex, targetIndex, visualId: targetIndex === count ? `${sectionKey}:end` : `${sectionKey}:${targetIndex}` }
  }
  const blocks = [...section.querySelectorAll<HTMLElement>('[data-qms-block-index]')]
    .filter(element => element.closest('[data-qms-section-key]') === section)
    .map(element => ({ index: Number(element.dataset.qmsBlockIndex), rect: element.getBoundingClientRect() }))
    .filter(block => Number.isInteger(block.index) && block.rect.width > 0 && block.rect.height > 0)

  // A non-layout environment can still resolve an explicitly hit block, e.g. keyboard/test adapters.
  if (!blocks.length) {
    const direct = hit?.closest<HTMLElement>('[data-qms-block-index]')
    const index = direct && section.contains(direct) ? Number(direct.dataset.qmsBlockIndex) : 0
    return targetAt(Number.isInteger(index) ? index : 0)
  }

  const rows = measureRows(blocks)
  for (const row of rows) {
    const first = row.blocks[0]
    const last = row.blocks.at(-1)!
    if (clientY < row.top) return targetAt(first.index)
    if (clientY > row.bottom) continue
    if (row.blocks.length === 1) return targetAt(first.index + (clientY > (row.top + row.bottom) / 2 ? 1 : 0))
    if (clientX < row.left || clientX > row.right) {
      return targetAt(clientY > (row.top + row.bottom) / 2 ? last.index + 1 : first.index)
    }
    for (const block of row.blocks) {
      if (clientX < block.rect.left) return targetAt(block.index)
      if (clientX <= block.rect.right) return targetAt(block.index + (clientX > (block.rect.left + block.rect.right) / 2 ? 1 : 0))
    }
    return targetAt(last.index + 1)
  }
  return targetAt(count)
}

function insideIfMeasurable(rect: DOMRect, x: number, y: number) {
  return rect.width <= 0 || rect.height <= 0 || x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom
}

function nearestSection(sections: HTMLElement[], y: number) {
  return sections.reduce<{ element: HTMLElement | null; distance: number }>((nearest, element) => {
    const rect = element.getBoundingClientRect()
    const distance = y < rect.top ? rect.top - y : y > rect.bottom ? y - rect.bottom : 0
    return distance < nearest.distance ? { element, distance } : nearest
  }, { element: null, distance: Infinity }).element
}

function measureRows(blocks: MeasuredBlock[]): MeasuredRow[] {
  const rows: MeasuredRow[] = []
  for (const block of [...blocks].sort((a, b) => a.rect.top - b.rect.top || a.rect.left - b.rect.left)) {
    const previous = rows.at(-1)
    if (previous && Math.abs(previous.top - block.rect.top) <= 3) {
      previous.blocks.push(block)
      previous.bottom = Math.max(previous.bottom, block.rect.bottom)
      previous.left = Math.min(previous.left, block.rect.left)
      previous.right = Math.max(previous.right, block.rect.right)
    } else rows.push({ top: block.rect.top, bottom: block.rect.bottom, left: block.rect.left, right: block.rect.right, blocks: [block] })
  }
  for (const row of rows) row.blocks.sort((a, b) => a.rect.left - b.rect.left)
  return rows
}
