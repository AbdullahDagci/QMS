import type { ElectronicFormSchema, FormDocumentBlock, FormFieldBlock, FormSection, FormTableBlock, FormTableCell, FormTextStyle } from '../../api/electronicForms'
import { blockKeys, createFieldBlock, materializeSchema, normalizeBlocks } from './formDocumentModel'

export interface DocumentPlacementPayload {
  kind: 'block' | 'placement'
  blockKey: string
  fieldKey?: string
  cellKey?: string
}

interface BlockLocation {
  section: FormSection
  sectionIndex: number
  block: FormDocumentBlock
  blockIndex: number
}

interface FieldPlacement extends BlockLocation {
  fieldKey: string
  style: FormTextStyle
  cell?: FormTableCell
}

/** targetIndex is an insertion boundary in the document before removal. */
export function moveDocumentPlacement(schema: ElectronicFormSchema, payload: DocumentPlacementPayload, sectionIndex: number, targetIndex: number): ElectronicFormSchema {
  if (!Number.isInteger(sectionIndex) || !Number.isInteger(targetIndex)) return schema
  const next = materializeSchema(structuredClone(schema))
  const source = findBlock(next, payload.blockKey)
  const targetSection = next.sections[sectionIndex]
  if (!source || !targetSection) return schema

  let movingBlock: FormDocumentBlock
  let movingFields: string[]
  let removesBlock = true
  if (payload.kind === 'placement') {
    const placement = placementFor(source, payload.fieldKey, payload.cellKey)
    if (!placement) return schema
    movingFields = [placement.fieldKey]
    removesBlock = !placement.cell
    movingBlock = placement.cell
      ? fieldBlockFor(next, placement.fieldKey, placement.style)
      : source.block
    if (!canTransferFields(source.section, targetSection, movingFields)) return schema
    detachPlacement(placement)
  } else {
    movingBlock = source.block
    movingFields = source.block.type === 'field' ? [source.block.fieldKey]
      : source.block.type === 'table' ? source.block.cells.flatMap(cell => cell.fieldKey ? [cell.fieldKey] : []) : []
    if (!canTransferFields(source.section, targetSection, movingFields)) return schema
    source.section.blocks!.splice(source.blockIndex, 1)
  }

  const adjustedIndex = source.sectionIndex === sectionIndex && removesBlock && source.blockIndex < targetIndex ? targetIndex - 1 : targetIndex
  targetSection.blocks!.splice(Math.max(0, Math.min(adjustedIndex, targetSection.blocks!.length)), 0, movingBlock)
  transferFields(source.section, targetSection, movingFields)
  return finish(schema, next)
}

/** Moves a field into a cell; a displaced field remains on the document. */
export function bindDocumentField(schema: ElectronicFormSchema, blockKey: string, cellKey: string, fieldKey: string | null): ElectronicFormSchema {
  const next = materializeSchema(structuredClone(schema))
  const target = findBlock(next, blockKey)
  if (!target || target.block.type !== 'table') return schema
  const cell = target.block.cells.find(item => item.key === cellKey)
  if (!cell || cell.fieldKey === fieldKey) return schema

  if (fieldKey === null) {
    releaseCell(next, target.section, target.block, cell)
    return finish(schema, next)
  }

  const incoming = findFieldPlacement(next, fieldKey)
  if (!incoming || !canTransferFields(incoming.section, target.section, [fieldKey])) return schema
  const incomingStyle = structuredClone(incoming.style)
  detachPlacement(incoming)
  releaseCell(next, target.section, target.block, cell)
  cell.fieldKey = fieldKey
  cell.text = ''
  delete cell.runs
  cell.style = incomingStyle
  transferFields(incoming.section, target.section, [fieldKey])
  return finish(schema, next)
}

function findBlock(schema: ElectronicFormSchema, key: string): BlockLocation | null {
  for (let sectionIndex = 0; sectionIndex < schema.sections.length; sectionIndex++) {
    const section = schema.sections[sectionIndex]
    const blockIndex = section.blocks!.findIndex(block => block.key === key)
    if (blockIndex >= 0) return { section, sectionIndex, block: section.blocks![blockIndex], blockIndex }
  }
  return null
}

function placementFor(location: BlockLocation, fieldKey?: string, cellKey?: string): FieldPlacement | null {
  if (!fieldKey || !location.section.fields.some(field => field.key === fieldKey)) return null
  if (location.block.type === 'field' && location.block.fieldKey === fieldKey && !cellKey) {
    return { ...location, fieldKey, style: location.block.style }
  }
  if (location.block.type === 'table' && cellKey) {
    const cell = location.block.cells.find(item => item.key === cellKey && item.fieldKey === fieldKey)
    if (cell) return { ...location, fieldKey, style: cell.style, cell }
  }
  return null
}

function findFieldPlacement(schema: ElectronicFormSchema, fieldKey: string): FieldPlacement | null {
  for (let sectionIndex = 0; sectionIndex < schema.sections.length; sectionIndex++) {
    const section = schema.sections[sectionIndex]
    for (let blockIndex = 0; blockIndex < section.blocks!.length; blockIndex++) {
      const block = section.blocks![blockIndex]
      const cellKey = block.type === 'table' ? block.cells.find(cell => cell.fieldKey === fieldKey)?.key : undefined
      const placement = placementFor({ section, sectionIndex, block, blockIndex }, fieldKey, cellKey)
      if (placement) return placement
    }
  }
  return null
}

function detachPlacement(placement: FieldPlacement) {
  if (placement.cell) placement.cell.fieldKey = null
  else placement.section.blocks!.splice(placement.blockIndex, 1)
}

function fieldBlockFor(schema: ElectronicFormSchema, fieldKey: string, style: FormTextStyle): FormFieldBlock {
  return { ...createFieldBlock(fieldKey, blockKeys(schema)), style: structuredClone(style) }
}

function releaseCell(schema: ElectronicFormSchema, section: FormSection, table: FormTableBlock, cell: FormTableCell) {
  if (!cell.fieldKey) return
  const replacement = fieldBlockFor(schema, cell.fieldKey, cell.style)
  const tableIndex = section.blocks!.findIndex(block => block.key === table.key)
  section.blocks!.splice(tableIndex + 1, 0, replacement)
  cell.fieldKey = null
}

function canTransferFields(source: FormSection, target: FormSection, fieldKeys: string[]) {
  const keys = new Set(fieldKeys)
  if (keys.size !== fieldKeys.length || fieldKeys.some(key => !source.fields.some(field => field.key === key))) return false
  if (source === target) return true
  return source.fields.length - keys.size >= 1 && target.fields.length + keys.size <= 50
    && !target.fields.some(field => keys.has(field.key))
}

function transferFields(source: FormSection, target: FormSection, fieldKeys: string[]) {
  if (source === target) return
  const keys = new Set(fieldKeys)
  target.fields.push(...source.fields.filter(field => keys.has(field.key)))
  source.fields = source.fields.filter(field => !keys.has(field.key))
}

function finish(original: ElectronicFormSchema, next: ElectronicFormSchema): ElectronicFormSchema {
  const allFields = new Set<string>()
  for (const section of next.sections) {
    if (section.fields.length < 1 || section.fields.length > 50 || section.blocks!.length > 200) return original
    const available = new Set(section.fields.map(field => field.key))
    const placed = new Set<string>()
    for (const field of section.fields) {
      if (allFields.has(field.key)) return original
      allFields.add(field.key)
    }
    for (const block of section.blocks!) {
      const keys = block.type === 'field' ? [block.fieldKey]
        : block.type === 'table' ? block.cells.flatMap(cell => cell.fieldKey ? [cell.fieldKey] : []) : []
      for (const key of keys) {
        if (!available.has(key) || placed.has(key)) return original
        placed.add(key)
      }
    }
    if (available.size !== placed.size) return original
  }
  next.sections = next.sections.map((section, index) => ({
    ...section,
    order: index + 1,
    fields: section.fields.map((field, fieldIndex) => ({ ...field, order: fieldIndex + 1 })),
    blocks: normalizeBlocks(section.blocks!),
  }))
  return next
}
