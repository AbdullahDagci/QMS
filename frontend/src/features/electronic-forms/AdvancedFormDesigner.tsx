import { useEffect, useEffectEvent, useRef, useState, type ChangeEvent, type DragEvent, type PointerEvent as ReactPointerEvent, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import {
  AddRounded, ArrowDownwardRounded, ArrowUpwardRounded, CalendarMonthRounded, CheckBoxRounded,
  ChecklistRounded, ContentCopyRounded, DeleteOutlineRounded, DragIndicatorRounded,
  FormatAlignCenterRounded, FormatAlignJustifyRounded, FormatAlignLeftRounded, FormatAlignRightRounded,
  FormatBoldRounded, FormatColorFillRounded, FormatColorTextRounded, FormatItalicRounded,
  FormatUnderlinedRounded, NotesRounded, NumbersRounded, RadioButtonCheckedRounded,
  RedoRounded, ScheduleRounded, SearchRounded, ShortTextRounded, TableChartRounded, TitleRounded,
  TuneRounded, UndoRounded, ViewSidebarRounded, ChevronLeftRounded, ChevronRightRounded, ZoomOutMapRounded, GridViewRounded, ArticleOutlined, ViewQuiltOutlined, InfoOutlined, MergeTypeRounded, CallSplitRounded,
} from '@mui/icons-material'
import {
  Alert, Box, Button, Checkbox, Chip, Divider, FormControlLabel, IconButton, MenuItem, Paper,
  Stack, Tab, Tabs, TextField, ToggleButton, ToggleButtonGroup, Tooltip, Typography, Popover,
} from '@mui/material'
import type {
  ElectronicFormSchema, FormBlockPosition, FormCondition, FormDocumentBlock, FormField, FormFieldType,
  FormSection, FormTableBlock, FormTableCell, FormTextStyle, OutputTemplateConfiguration,
} from '../../api/electronicForms'
import {
  blockKeys, blocksFor, createFieldBlock, createTableBlock, createTextBlock, cssFont,
  defaultDocumentSettings, fontFamilies, materializeSchema, normalizeBlocks, uniqueKey,
} from './formDocumentModel'
import type { Editor } from '@tiptap/react'
import { StudioRichText, applyRichTextStyle } from './StudioRichText'
import { moveDocumentPlacement, bindDocumentField } from './formDocumentActions'
import { mergeTableCell, resizeStudioTable, setTableHeaderRows, splitTableCell, tableColumnWidths, visibleTableCells } from './studioTables'
import { bodyMetrics, clampPosition, movePositionWithKey, positionFromRect, PIXELS_PER_MM, resizePosition, type FormBodyMetrics } from './freeFormPosition'
import { startFreeFormDrag } from './freeFormDrag'
import { resolveFlowDropTarget } from './resolveFlowDropTarget'
import './formStudio.css'

export interface DesignerDraft {
  name: string
  description: string
  category: string
  kind: string
  schema: ElectronicFormSchema
  output: OutputTemplateConfiguration
  changeSummary: string
}

interface AdvancedFormDesignerProps {
  draft: DesignerDraft
  onChange: (value: DesignerDraft) => void
  editable: boolean
  metadataEditable: boolean
  revisionKey: string
}

interface Selection { blockKey: string; cellKey?: string }
interface DragPayload { kind: 'block' | 'placement'; blockKey: string; fieldKey?: string; cellKey?: string }
type PointerDropTarget =
  | { kind: 'free'; position: FormBlockPosition }
  | { kind: 'position'; sectionIndex: number; targetIndex: number; visualId: string }
  | { kind: 'cell'; sectionIndex: number; blockKey: string; cellKey: string; visualId: string }

const palette: Array<{ type: FormFieldType; label: string; description: string; icon: ReactNode }> = [
  { type: 'shortText', label: 'Kısa metin', description: 'Tek satırlı giriş', icon: <ShortTextRounded /> },
  { type: 'longText', label: 'Uzun metin', description: 'Çok satırlı açıklama', icon: <NotesRounded /> },
  { type: 'number', label: 'Sayı', description: 'Alt/üst sınır ve birim', icon: <NumbersRounded /> },
  { type: 'date', label: 'Tarih', description: 'Takvim seçimi', icon: <CalendarMonthRounded /> },
  { type: 'dateTime', label: 'Tarih ve saat', description: 'Zaman damgası', icon: <ScheduleRounded /> },
  { type: 'singleSelect', label: 'Tekli seçim', description: 'Bir seçenek seçilir', icon: <RadioButtonCheckedRounded /> },
  { type: 'multiSelect', label: 'Çoklu seçim', description: 'Birden çok seçenek', icon: <ChecklistRounded /> },
  { type: 'checkbox', label: 'Onay kutusu', description: 'Evet / hayır alanı', icon: <CheckBoxRounded /> },
]

const paletteMime = 'application/x-qms-field-type'
const blockMime = 'application/x-qms-document-block'
const pagePixels = { portrait: { width: 794, height: 1123 }, landscape: { width: 1123, height: 794 } }

export function AdvancedFormDesigner({ draft, onChange, editable, metadataEditable, revisionKey }: AdvancedFormDesignerProps) {
  const firstBlock = blocksFor(draft.schema.sections[0])[0]
  const [selection, setSelection] = useState<Selection | null>(firstBlock ? { blockKey: firstBlock.key } : null)
  const [inspectorTab, setInspectorTab] = useState(0)
  const [zoom, setZoom] = useState(90)
  const [ribbon, setRibbon] = useState(0)
  const [libraryTab, setLibraryTab] = useState(0)
  const [libraryOpen, setLibraryOpen] = useState(true)
  const [inspectorOpen, setInspectorOpen] = useState(true)
  const [tableAnchor, setTableAnchor] = useState<HTMLElement | null>(null)
  const [tableSize, setTableSize] = useState({ rows: 3, columns: 3 })
  const [fitPage, setFitPage] = useState(true)
  const [viewportWidth, setViewportWidth] = useState(800)
  const [pageHeight, setPageHeight] = useState(1123)
  const viewportRef = useRef<HTMLDivElement | null>(null)
  const pageRef = useRef<HTMLDivElement | null>(null)
  const activeRichEditor = useRef<Editor | null>(null)
  const [richSelectionStyle, setRichSelectionStyle] = useState<FormTextStyle | null>(null)
  const [paletteSearch, setPaletteSearch] = useState('')
  const [dragMode, setDragMode] = useState<'free' | 'order'>('free')
  const bodyRef = useRef<HTMLDivElement | null>(null)
  const [dragTarget, setDragTarget] = useState<string | null>(null)
  const [historyAvailability, setHistoryAvailability] = useState({ undo: false, redo: false })
  const undoStack = useRef<DesignerDraft[]>([])
  const redoStack = useRef<DesignerDraft[]>([])

  const resetRevisionSelection = useEffectEvent(() => {
    setSelection(firstBlock ? { blockKey: firstBlock.key } : null)
    activeRichEditor.current = null
    setRichSelectionStyle(null)
  })

  useEffect(() => {
    undoStack.current = []
    redoStack.current = []
    setHistoryAvailability({ undo: false, redo: false })
    resetRevisionSelection()
  }, [revisionKey])

  const commit = (next: DesignerDraft) => {
    if (!editable) return
    const bounds = bodyMetrics(next.schema)
    next = { ...next, schema: { ...next.schema, sections: next.schema.sections.map(section => ({ ...section, ...(section.blocks ? { blocks: section.blocks.map(block => block.position ? { ...block, position: clampPosition(block.position, bounds) } : block) } : {}) })) } }
    if (JSON.stringify(next) === JSON.stringify(draft)) return
    undoStack.current.push(structuredClone(draft))
    if (undoStack.current.length > 80) undoStack.current.shift()
    redoStack.current = []
    setHistoryAvailability({ undo: true, redo: false })
    onChange(next)
  }

  const edit = (change: (next: DesignerDraft) => void) => {
    const next = structuredClone(draft)
    next.schema = materializeSchema(next.schema)
    change(next)
    next.schema.sections = normalizeSections(next.schema.sections)
    commit(next)
  }

  const undo = () => {
    if (!editable) return
    const previous = undoStack.current.pop()
    if (!previous) return
    redoStack.current.push(structuredClone(draft))
    setHistoryAvailability({ undo: undoStack.current.length > 0, redo: true })
    onChange(previous)
  }

  const redo = () => {
    if (!editable) return
    const next = redoStack.current.pop()
    if (!next) return
    undoStack.current.push(structuredClone(draft))
    setHistoryAvailability({ undo: true, redo: redoStack.current.length > 0 })
    onChange(next)
  }

  const selectedLocation = selection ? findBlock(draft.schema, selection.blockKey) : null
  const selectedBlock = selectedLocation?.block ?? null
  const selectedCell = selectedBlock?.type === 'table' && selection?.cellKey
    ? selectedBlock.cells.find(cell => cell.key === selection.cellKey) ?? null : null
  const selectedFieldKey = selectedBlock?.type === 'field' ? selectedBlock.fieldKey : selectedCell?.fieldKey ?? null
  const selectedField = selectedFieldKey ? findField(draft.schema, selectedFieldKey) : null
  const selectedSectionIndex = selectedLocation?.sectionIndex ?? 0
  const baseSelectedStyle = selectedCell?.style ?? (selectedBlock?.type === 'text' || selectedBlock?.type === 'field' ? selectedBlock.style : null)
  const selectedStyle = richSelectionStyle ?? baseSelectedStyle
  const query = paletteSearch.trim().toLocaleLowerCase('tr-TR')
  const visiblePalette = palette.filter(item => !query || `${item.label} ${item.description}`.toLocaleLowerCase('tr-TR').includes(query))
  const document = draft.schema.document ?? defaultDocumentSettings()
  const page = pagePixels[document.orientation]
  const metrics = bodyMetrics(document)
  const effectiveZoom = fitPage ? Math.min(100, Math.max(35, Math.floor((viewportWidth - 80) / page.width * 100))) : zoom
  const selectItem = (value: Selection) => {
    if (selection?.blockKey !== value.blockKey || selection?.cellKey !== value.cellKey) {
      activeRichEditor.current = null
      setRichSelectionStyle(null)
    }
    setSelection(value)
    setInspectorTab(0)
  }
  useEffect(() => {
    if (typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(entries => {
      for (const entry of entries) {
        if (entry.target === viewportRef.current) setViewportWidth(entry.contentRect.width)
        if (entry.target === pageRef.current) setPageHeight(entry.contentRect.height)
      }
    })
    if (viewportRef.current) observer.observe(viewportRef.current)
    if (pageRef.current) observer.observe(pageRef.current)
    return () => observer.disconnect()
  }, [])
  useEffect(() => {
    activeRichEditor.current = null
    setRichSelectionStyle(null)
  }, [selection?.blockKey, selection?.cellKey])

  const addSection = () => {
    const sectionIndex = draft.schema.sections.length
    const field = createField('shortText', draft.schema, 1)
    const fieldBlock = createFieldBlock(field.key, blockKeys(draft.schema))
    const section: FormSection = { key: uniqueKey(`bolum${sectionIndex + 1}`, sectionKeys(draft.schema)), title: `Yeni bölüm ${sectionIndex + 1}`, description: '', order: sectionIndex + 1, columns: 2, fields: [field], blocks: [fieldBlock] }
    edit(next => { next.schema.sections.push(section) })
    setSelection({ blockKey: fieldBlock.key })
    setInspectorTab(0)
  }

  const addDocumentBlock = (kind: 'paragraph' | 'heading' | 'table', rows = 3, columns = 3) => {
    let createdKey = ''
    edit(next => {
      const section = next.schema.sections[selectedSectionIndex] ?? next.schema.sections[0]
      const block = kind === 'table' ? createTableBlock(blockKeys(next.schema), rows, columns) : createTextBlock(blockKeys(next.schema), kind)
      createdKey = block.key
      const selectedIndex = selectedLocation?.sectionIndex === selectedSectionIndex ? section.blocks!.findIndex(item => item.key === selectedLocation.block.key) : -1
      section.blocks!.splice(selectedIndex >= 0 ? selectedIndex + 1 : section.blocks!.length, 0, block)
    })
    if (createdKey) setSelection({ blockKey: createdKey })
    setInspectorTab(0)
  }

  const addField = (sectionIndex: number, type: FormFieldType, targetIndex?: number, targetCell?: { blockKey: string; cellKey: string }, position?: FormBlockPosition) => {
    let nextSelection: Selection | null = null
    edit(next => {
      const section = next.schema.sections[sectionIndex]
      if (!section) return
      const field = createField(type, next.schema, section.fields.length + 1)
      section.fields.push(field)
      if (targetCell) {
        const table = section.blocks!.find(block => block.key === targetCell.blockKey)
        const cell = table?.type === 'table' ? table.cells.find(item => item.key === targetCell.cellKey) : null
        if (cell && table?.type === 'table') {
          releaseCellField(section, table, cell)
          cell.fieldKey = field.key
          cell.text = ''
          delete cell.runs
          nextSelection = { blockKey: table.key, cellKey: cell.key }
          return
        }
      }
      const block = createFieldBlock(field.key, blockKeys(next.schema))
      if (position) block.position = position
      section.blocks!.splice(targetIndex ?? section.blocks!.length, 0, block)
      nextSelection = { blockKey: block.key }
    })
    if (nextSelection) setSelection(nextSelection)
    setInspectorTab(0)
  }

  const updateSection = (sectionKey: string, value: FormSection) => edit(next => {
    const index = next.schema.sections.findIndex(section => section.key === sectionKey)
    if (index >= 0) next.schema.sections[index] = { ...value, blocks: normalizeBlocks(blocksFor(value)) }
  })

  const deleteSection = (sectionKey: string) => {
    if (draft.schema.sections.length === 1) return
    edit(next => { next.schema.sections = next.schema.sections.filter(section => section.key !== sectionKey) })
    const section = draft.schema.sections.find(item => item.key !== sectionKey)
    const block = section ? blocksFor(section)[0] : null
    setSelection(block ? { blockKey: block.key } : null)
  }

  const moveSection = (sectionKey: string, direction: -1 | 1) => edit(next => {
    const index = next.schema.sections.findIndex(section => section.key === sectionKey)
    const target = index + direction
    if (index < 0 || target < 0 || target >= next.schema.sections.length) return
    ;[next.schema.sections[index], next.schema.sections[target]] = [next.schema.sections[target], next.schema.sections[index]]
  })

  const patchBlock = (blockKey: string, updater: (block: FormDocumentBlock, section: FormSection) => FormDocumentBlock) => edit(next => {
    const location = findBlock(next.schema, blockKey)
    if (location) location.section.blocks![location.blockIndex] = updater(location.block, location.section)
  })

  const patchField = (fieldKey: string, nextField: FormField) => edit(next => {
    for (const section of next.schema.sections) {
      const index = section.fields.findIndex(field => field.key === fieldKey)
      if (index >= 0) section.fields[index] = nextField
    }
  })

  const deleteField = (fieldKey: string) => {
    const location = findField(draft.schema, fieldKey)
    if (!location || location.section.fields.length === 1) return
    edit(next => {
      const section = next.schema.sections[location.sectionIndex]
      section.fields = section.fields.filter(field => field.key !== fieldKey)
      section.blocks = section.blocks!.filter(block => block.type !== 'field' || block.fieldKey !== fieldKey).map(block => block.type === 'table' ? { ...block, cells: block.cells.map(cell => cell.fieldKey === fieldKey ? { ...cell, fieldKey: null } : cell) } : block)
    })
    const remaining = draft.schema.sections.flatMap(section => blocksFor(section)).find(block => block.key !== selection?.blockKey)
    setSelection(remaining ? { blockKey: remaining.key } : null)
  }

  const duplicateField = (fieldKey: string) => {
    const location = findField(draft.schema, fieldKey)
    if (!location) return
    let createdKey = ''
    edit(next => {
      const section = next.schema.sections[location.sectionIndex]
      const copy = structuredClone(location.field)
      copy.key = uniqueKey(`${location.field.key}Kopya`, fieldKeys(next.schema))
      copy.label = `${location.field.label} kopyası`
      section.fields.push(copy)
      const block = createFieldBlock(copy.key, blockKeys(next.schema))
      createdKey = block.key
      const sourceIndex = selectedLocation?.sectionIndex === location.sectionIndex ? selectedLocation.blockIndex : section.blocks!.length - 1
      section.blocks!.splice(sourceIndex + 1, 0, block)
    })
    if (createdKey) setSelection({ blockKey: createdKey })
  }

  const deleteBlock = (blockKey: string) => {
    const location = findBlock(draft.schema, blockKey)
    if (!location) return
    if (location.block.type === 'field') return deleteField(location.block.fieldKey)
    edit(next => {
      const current = findBlock(next.schema, blockKey)
      if (!current) return
      if (current.block.type === 'table') {
        const fieldRefs = current.block.cells.map(cell => cell.fieldKey).filter((key): key is string => Boolean(key))
        const replacements = fieldRefs.map(fieldKey => createFieldBlock(fieldKey, blockKeys(next.schema)))
        current.section.blocks!.splice(current.blockIndex, 1, ...replacements)
      } else current.section.blocks!.splice(current.blockIndex, 1)
    })
    const nextBlock = blocksFor(location.section).find(block => block.key !== blockKey)
    setSelection(nextBlock ? { blockKey: nextBlock.key } : null)
  }

  const duplicateBlock = (blockKey: string) => {
    const location = findBlock(draft.schema, blockKey)
    if (!location) return
    if (location.block.type === 'field') return duplicateField(location.block.fieldKey)
    let createdKey = ''
    edit(next => {
      const current = findBlock(next.schema, blockKey)
      if (!current) return
      const copy = structuredClone(current.block)
      copy.key = uniqueKey(`${current.block.key}Kopya`, blockKeys(next.schema))
      createdKey = copy.key
      if (copy.type === 'table') copy.cells = copy.cells.map((cell, index) => ({ ...cell, key: `${copy.key}Hucre${index + 1}`, fieldKey: null }))
      current.section.blocks!.splice(current.blockIndex + 1, 0, copy)
    })
    if (createdKey) setSelection({ blockKey: createdKey })
  }

  const applyStyle = (stylePatch: Partial<FormTextStyle>) => {
    if (!selection || !selectedStyle) return
    if (activeRichEditor.current && !activeRichEditor.current.isDestroyed && applyRichTextStyle(activeRichEditor.current, stylePatch)) return
    patchBlock(selection.blockKey, block => {
      if (block.type === 'text' || block.type === 'field') return { ...block, style: { ...block.style, ...stylePatch }, ...(block.type === 'text' && block.runs ? { runs: block.runs.map(run => ({ ...run, style: { ...run.style, ...stylePatch } })) } : {}) }
      if (block.type === 'table' && selection.cellKey) return { ...block, cells: block.cells.map(cell => cell.key === selection.cellKey ? { ...cell, style: { ...cell.style, ...stylePatch }, ...(cell.runs ? { runs: cell.runs.map(run => ({ ...run, style: { ...run.style, ...stylePatch } })) } : {}) } : cell) }
      return block
    })
  }

  const bindCellField = (blockKey: string, cellKey: string, fieldKey: string | null) => {
    commit({ ...draft, schema: bindDocumentField(draft.schema, blockKey, cellKey, fieldKey) })
    setSelection({ blockKey, cellKey })
  }

  const resizeSelectedTable = (rows: number, columns: number) => {
    if (!selectedBlock || selectedBlock.type !== 'table') return
    patchBlock(selectedBlock.key, (block, section) => {
      if (block.type !== 'table') return block
      const resized = resizeStudioTable(block, rows, columns)
      const retained = new Set(resized.cells.map(cell => cell.key))
      const removedFields = block.cells.filter(cell => !retained.has(cell.key)).map(cell => cell.fieldKey).filter((key): key is string => Boolean(key))
      for (const fieldKey of removedFields) section.blocks!.push(createFieldBlock(fieldKey, section.blocks!.map(item => item.key)))
      return resized
    })
  }

  const moveBlock = (payload: DragPayload, targetSectionIndex: number, targetIndex: number) => {
    const schema = moveDocumentPlacement(draft.schema, payload, targetSectionIndex, targetIndex)
    if (schema === draft.schema) return
    const placed = schema.sections.flatMap(section => blocksFor(section)).find(block => payload.fieldKey ? block.type === 'field' && block.fieldKey === payload.fieldKey : block.key === payload.blockKey)
    if (placed) delete placed.position
    commit({ ...draft, schema })
    if (placed) setSelection({ blockKey: placed.key })
  }

  const moveBlockByStep = (blockKey: string, direction: -1 | 1) => {
    const location = findBlock(draft.schema, blockKey)
    if (!location) return
    const sectionBlocks = blocksFor(location.section)
    const targetIndex = direction === -1 ? location.blockIndex - 1 : location.blockIndex + 2
    if (targetIndex < 0 || (direction === 1 && location.blockIndex === sectionBlocks.length - 1)) return
    const payload: DragPayload = location.block.type === 'field'
      ? { kind: 'placement', blockKey: location.block.key, fieldKey: location.block.fieldKey }
      : { kind: 'block', blockKey: location.block.key }
    moveBlock(payload, location.sectionIndex, targetIndex)
  }

  const handlePointerDrop = (payload: DragPayload, target: PointerDropTarget) => {
    setDragTarget(null)
    if (target.kind === 'free') {
      const source = findBlock(draft.schema, payload.blockKey)
      if (!source) return
      let schema = payload.cellKey && payload.fieldKey
        ? moveDocumentPlacement(draft.schema, payload, source.sectionIndex, blocksFor(source.section).length)
        : materializeSchema(structuredClone(draft.schema))
      if (schema === draft.schema) return
      schema = structuredClone(schema)
      const placed = payload.cellKey && payload.fieldKey
        ? schema.sections.flatMap(section => blocksFor(section)).find(block => block.type === 'field' && block.fieldKey === payload.fieldKey)
        : findBlock(schema, payload.blockKey)?.block
      if (!placed) return
      placed.position = clampPosition(target.position, metrics)
      commit({ ...draft, schema })
      setSelection({ blockKey: placed.key })
      return
    }
    if (target.kind === 'cell') {
      if (payload.kind === 'placement' && payload.fieldKey) bindCellField(target.blockKey, target.cellKey, payload.fieldKey)
      return
    }
    moveBlock(payload, target.sectionIndex, target.targetIndex)
  }

  const handleDrop = (event: DragEvent, sectionIndex: number, targetIndex: number) => {
    event.preventDefault(); event.stopPropagation(); setDragTarget(null)
    const paletteType = event.dataTransfer.getData(paletteMime) as FormFieldType
    if (paletteType) {
      const rect = bodyRef.current?.getBoundingClientRect()
      const scale = rect ? rect.width / (metrics.width * PIXELS_PER_MM) : 0
      const position = dragMode === 'free' && rect && scale > 0
        ? clampPosition({ x: (event.clientX - rect.left) / scale / PIXELS_PER_MM, y: (event.clientY - rect.top) / scale / PIXELS_PER_MM, width: Math.min(90, metrics.width), height: paletteType === 'longText' ? 25 : 18 }, metrics) : undefined
      return addField(sectionIndex, paletteType, targetIndex, undefined, position)
    }
    const raw = event.dataTransfer.getData(blockMime)
    if (!raw) return
    try { moveBlock(JSON.parse(raw) as DragPayload, sectionIndex, targetIndex) } catch { /* Ignore external drops. */ }
  }

  const handleCellDrop = (event: DragEvent, sectionIndex: number, blockKey: string, cellKey: string) => {
    event.preventDefault(); event.stopPropagation()
    const paletteType = event.dataTransfer.getData(paletteMime) as FormFieldType
    if (paletteType) return addField(sectionIndex, paletteType, undefined, { blockKey, cellKey })
    const raw = event.dataTransfer.getData(blockMime)
    if (!raw) return
    try { const payload = JSON.parse(raw) as DragPayload; if (payload.kind === 'placement' && payload.fieldKey) bindCellField(blockKey, cellKey, payload.fieldKey) } catch { /* Ignore external drops. */ }
  }

  const updateDocument = (patch: Partial<NonNullable<ElectronicFormSchema['document']>>) => edit(next => {
    next.schema.document = { ...(next.schema.document ?? defaultDocumentSettings()), ...patch }
    const bounds = bodyMetrics(next.schema)
    for (const section of next.schema.sections) for (const block of section.blocks ?? []) if (block.position) block.position = clampPosition(block.position, bounds)
  })


  const insertFieldGrid = () => edit(next => {
    const section = next.schema.sections[selectedSectionIndex]
    const candidates = section.blocks!.filter(block => block.type === 'field')
    if (!candidates.length) return
    const table = createTableBlock(blockKeys(next.schema), Math.min(20, candidates.length), 2)
    table.headerRows = 0
    table.columnWidths = [35, 65]
    table.borderColor = '#aeb8c5'
    for (let index = 0; index < table.rows; index++) {
      const fieldBlock = candidates[index]
      if (fieldBlock.type !== 'field') continue
      const field = section.fields.find(item => item.key === fieldBlock.fieldKey)!
      Object.assign(table.cells[index * 2], { text: field.label, backgroundColor: '#f3f5f8', style: { ...fieldBlock.style, fontSize: 11 } })
      Object.assign(table.cells[index * 2 + 1], { text: '', fieldKey: field.key, backgroundColor: '#ffffff', style: { ...fieldBlock.style, fontSize: 11 } })
    }
    const consumed = new Set(candidates.slice(0, table.rows).map(block => block.key))
    section.blocks = section.blocks!.filter(block => !consumed.has(block.key))
    section.blocks.unshift(table)
    setSelection({ blockKey: table.key })
    setRibbon(3)
  })

  return <Box className="form-studio" onKeyDown={event => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
      event.preventDefault()
      if (event.shiftKey) redo(); else undo()
    }
  }} sx={{ height: '100%', minHeight: 0, flex: 1, display: 'flex', flexDirection: 'column', bgcolor: '#e9edf2' }}>
    <div className="studio-tabs">
      <div className="studio-wordmark"><ArticleOutlined fontSize="small" /><span>FORM STÜDYOSU</span></div>
      <Tabs value={ribbon === 3 && selectedBlock?.type !== 'table' ? 0 : ribbon} onChange={(_, value: number) => setRibbon(value)} aria-label="Tasarım araçları">
        <Tab label="Giriş" /><Tab label="Ekle" /><Tab label="Sayfa düzeni" />
        {selectedBlock?.type === 'table' && <Tab label="Tablo düzeni" />}
      </Tabs>
      <div className="studio-quick-actions">
        <ToggleButtonGroup className="studio-drag-mode" size="small" exclusive value={dragMode} onChange={(_, value: 'free' | 'order' | null) => value && setDragMode(value)} aria-label="Taşıma biçimi"><ToggleButton value="free" aria-label="Serbest taşıma">Serbest taşı</ToggleButton><ToggleButton value="order" aria-label="Sıralama">Sırala</ToggleButton></ToggleButtonGroup>
        <IconButton aria-label="Geri al" title="Geri al" disabled={!editable || !historyAvailability.undo} onClick={undo}><UndoRounded fontSize="small" /></IconButton>
        <IconButton aria-label="İleri al" title="İleri al" disabled={!editable || !historyAvailability.redo} onClick={redo}><RedoRounded fontSize="small" /></IconButton>
        <span className="studio-divider" />
        <IconButton aria-label="Alan kitaplığını aç/kapat" title="Alan kitaplığı" onClick={() => setLibraryOpen(value => !value)}><ViewSidebarRounded fontSize="small" /></IconButton>
        <IconButton aria-label="Özellikler panelini aç/kapat" title="Özellikler" onClick={() => setInspectorOpen(value => !value)}><TuneRounded fontSize="small" /></IconButton>
      </div>
    </div>
    <div className="studio-ribbon" role="toolbar" aria-label="Belge araç çubuğu">
      {(ribbon === 0 || ribbon === 3 && selectedBlock?.type !== 'table') && <>
        <div className="studio-ribbon-group">
          <div className="studio-ribbon-controls">
            <TextField select size="small" aria-label="Yazı tipi" value={selectedStyle?.fontFamily ?? document.defaultFontFamily} disabled={!editable || !selectedStyle} onChange={event => applyStyle({ fontFamily: event.target.value })} sx={{ width: 155 }}>{fontFamilies.map(font => <MenuItem key={font} value={font} sx={{ fontFamily: cssFont(font) }}>{font}</MenuItem>)}</TextField>
            <TextField select size="small" aria-label="Yazı boyutu" value={selectedStyle?.fontSize ?? document.defaultFontSize} disabled={!editable || !selectedStyle} onChange={event => applyStyle({ fontSize: Number(event.target.value) })} sx={{ width: 70 }}>{[8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 40, 48].map(size => <MenuItem key={size} value={size}>{size}</MenuItem>)}</TextField>
            <ToggleButton size="small" value="bold" selected={Boolean(selectedStyle?.bold)} disabled={!editable || !selectedStyle} onMouseDown={event => event.preventDefault()} onChange={() => applyStyle({ bold: !selectedStyle?.bold })} aria-label="Kalın"><FormatBoldRounded /></ToggleButton>
            <ToggleButton size="small" value="italic" selected={Boolean(selectedStyle?.italic)} disabled={!editable || !selectedStyle} onMouseDown={event => event.preventDefault()} onChange={() => applyStyle({ italic: !selectedStyle?.italic })} aria-label="İtalik"><FormatItalicRounded /></ToggleButton>
            <ToggleButton size="small" value="underline" selected={Boolean(selectedStyle?.underline)} disabled={!editable || !selectedStyle} onMouseDown={event => event.preventDefault()} onChange={() => applyStyle({ underline: !selectedStyle?.underline })} aria-label="Altı çizili"><FormatUnderlinedRounded /></ToggleButton>
            <ColorTool label="Yazı rengi" icon={<FormatColorTextRounded />} value={selectedStyle?.color ?? '#172033'} disabled={!editable || !selectedStyle} onChange={color => applyStyle({ color })} />
            <ColorTool label="Vurgu rengi" icon={<FormatColorFillRounded />} value={selectedStyle?.backgroundColor ?? '#ffffff'} disabled={!editable || !selectedStyle} onChange={backgroundColor => applyStyle({ backgroundColor })} />
          </div>
          <span className="studio-group-caption">Yazı tipi · Metin seçerek ayrı biçimlendirin</span>
        </div>
        <div className="studio-ribbon-group">
          <ToggleButtonGroup size="small" exclusive value={selectedStyle?.alignment ?? 'left'} onChange={(_, value) => value && applyStyle({ alignment: value })} disabled={!editable || !selectedStyle} aria-label="Metin hizası"><ToggleButton value="left" aria-label="Sola hizala"><FormatAlignLeftRounded /></ToggleButton><ToggleButton value="center" aria-label="Ortala"><FormatAlignCenterRounded /></ToggleButton><ToggleButton value="right" aria-label="Sağa hizala"><FormatAlignRightRounded /></ToggleButton><ToggleButton value="justify" aria-label="İki yana yasla"><FormatAlignJustifyRounded /></ToggleButton></ToggleButtonGroup>
          <span className="studio-group-caption">Paragraf</span>
        </div>
        <div className="studio-ribbon-group">
          <div className="studio-ribbon-controls"><ToolbarButton icon={<NotesRounded />} label="Metin" onClick={() => addDocumentBlock('paragraph')} disabled={!editable} /><ToolbarButton icon={<TitleRounded />} label="Başlık" onClick={() => addDocumentBlock('heading')} disabled={!editable} /></div>
          <span className="studio-group-caption">Belgeye yaz</span>
        </div>
      </>}
      {ribbon === 1 && <>
        <div className="studio-ribbon-group"><div className="studio-ribbon-controls"><ToolbarButton icon={<NotesRounded />} label="Metin" onClick={() => addDocumentBlock('paragraph')} disabled={!editable} /><ToolbarButton icon={<TitleRounded />} label="Başlık" onClick={() => addDocumentBlock('heading')} disabled={!editable} /><Button size="small" startIcon={<TableChartRounded />} disabled={!editable} onClick={event => setTableAnchor(event.currentTarget)}>Tablo</Button><ToolbarButton icon={<AddRounded />} label="Bölüm" onClick={addSection} disabled={!editable} /></div><span className="studio-group-caption">Belge öğeleri</span></div>
        <div className="studio-ribbon-group"><div className="studio-ribbon-controls"><Button size="small" startIcon={<ViewQuiltOutlined />} disabled={!editable || !blocksFor(draft.schema.sections[selectedSectionIndex]).some(block => block.type === 'field')} onClick={insertFieldGrid}>Alanları form tablosuna dönüştür</Button></div><span className="studio-group-caption">Hazır düzen · Etiket ve doldurulacak alan</span></div>
      </>}
      {ribbon === 2 && <>
        <div className="studio-ribbon-group"><TextField select size="small" label="Sayfa yönü" value={document.orientation} disabled={!editable} onChange={event => updateDocument({ orientation: event.target.value as 'portrait' | 'landscape' })} sx={{ width: 160 }}><MenuItem value="portrait">A4 · Dikey</MenuItem><MenuItem value="landscape">A4 · Yatay</MenuItem></TextField><span className="studio-group-caption">Sayfa</span></div>
        <div className="studio-ribbon-group"><div className="studio-ribbon-controls">{(['marginTop', 'marginRight', 'marginBottom', 'marginLeft'] as const).map((key, index) => <TextField key={key} size="small" type="number" label={['Üst (mm)', 'Sağ (mm)', 'Alt (mm)', 'Sol (mm)'][index]} value={document[key]} disabled={!editable} onChange={event => updateDocument({ [key]: Math.max(5, Math.min(40, Number(event.target.value))) })} sx={{ width: 88 }} />)}</div><span className="studio-group-caption">Kenar boşlukları</span></div>
        <div className="studio-ribbon-group"><TextField select size="small" label="Bölüm sütunları" value={draft.schema.sections[selectedSectionIndex].columns} disabled={!editable} onChange={event => updateSection(draft.schema.sections[selectedSectionIndex].key, { ...draft.schema.sections[selectedSectionIndex], columns: Number(event.target.value) as 1 | 2 })} sx={{ width: 155 }}><MenuItem value={1}>Tek sütun</MenuItem><MenuItem value={2}>İki sütun</MenuItem></TextField><span className="studio-group-caption">Alan yerleşimi</span></div>
      </>}
      {ribbon === 3 && selectedBlock?.type === 'table' && <>
        <div className="studio-ribbon-group"><div className="studio-ribbon-controls"><TextField size="small" type="number" label="Satır" value={selectedBlock.rows} disabled={!editable} onChange={event => resizeSelectedTable(Number(event.target.value), selectedBlock.columns)} sx={{ width: 75 }} /><TextField size="small" type="number" label="Sütun" value={selectedBlock.columns} disabled={!editable} onChange={event => resizeSelectedTable(selectedBlock.rows, Number(event.target.value))} sx={{ width: 75 }} /><Button size="small" disabled={!editable || selectedBlock.rows >= 20} onClick={() => resizeSelectedTable(selectedBlock.rows + 1, selectedBlock.columns)}>+ Satır</Button><Button size="small" disabled={!editable || selectedBlock.columns >= 10} onClick={() => resizeSelectedTable(selectedBlock.rows, selectedBlock.columns + 1)}>+ Sütun</Button></div><span className="studio-group-caption">Tablo boyutu · Kenarlardan sütun genişliğini değiştirin</span></div>
        <div className="studio-ribbon-group"><div className="studio-ribbon-controls"><Button size="small" startIcon={<MergeTypeRounded />} disabled={!editable || !selectedCell || mergeTableCell(selectedBlock, selectedCell.key, 'right') === selectedBlock} onClick={() => selectedCell && patchBlock(selectedBlock.key, block => block.type === 'table' ? mergeTableCell(block, selectedCell.key, 'right') : block)}>Sağ hücreyle birleştir</Button><Button size="small" disabled={!editable || !selectedCell || mergeTableCell(selectedBlock, selectedCell.key, 'down') === selectedBlock} onClick={() => selectedCell && patchBlock(selectedBlock.key, block => block.type === 'table' ? mergeTableCell(block, selectedCell.key, 'down') : block)}>Alt hücreyle birleştir</Button><Button size="small" startIcon={<CallSplitRounded />} disabled={!editable || !selectedCell || (selectedCell.colSpan ?? 1) === 1 && (selectedCell.rowSpan ?? 1) === 1} onClick={() => selectedCell && patchBlock(selectedBlock.key, block => block.type === 'table' ? splitTableCell(block, selectedCell.key) : block)}>Hücreyi böl</Button></div><span className="studio-group-caption">Hücre işlemleri · Tab ile sonraki hücreye geçin</span></div>
      </>}
    </div>
    <Popover open={Boolean(tableAnchor)} anchorEl={tableAnchor} onClose={() => setTableAnchor(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}>
      <div className="studio-table-picker"><strong>Tablo ekle</strong><span>{tableSize.columns} sütun × {tableSize.rows} satır</span><div className="studio-table-grid">{Array.from({ length: 48 }, (_, index) => { const row = Math.floor(index / 8) + 1; const column = index % 8 + 1; return <button key={index} aria-label={row + ' satır ' + column + ' sütun tablo ekle'} className={row <= tableSize.rows && column <= tableSize.columns ? 'chosen' : ''} onMouseEnter={() => setTableSize({ rows: row, columns: column })} onFocus={() => setTableSize({ rows: row, columns: column })} onClick={() => { addDocumentBlock('table', row, column); setTableAnchor(null); setRibbon(3) }} /> })}</div><small>Hücrelere yazın veya form alanı yerleştirin.</small></div>
    </Popover>

    <div className="studio-workspace" style={{ gridTemplateColumns: (libraryOpen ? '216px ' : '0px ') + 'minmax(0, 1fr) ' + (inspectorOpen ? '272px' : '0px') }}>
      <aside className="studio-library" hidden={!libraryOpen}>
        <div className="studio-panel-tabs"><button className={libraryTab === 0 ? 'active' : ''} onClick={() => setLibraryTab(0)}><GridViewRounded fontSize="small" />Alanlar</button><button className={libraryTab === 1 ? 'active' : ''} onClick={() => setLibraryTab(1)}><InfoOutlined fontSize="small" />Form</button></div>
        <div className="studio-panel-body">
        {libraryTab === 1 ? <Stack spacing={2}><Typography variant="subtitle2">Form bilgileri</Typography><TextField disabled={!metadataEditable} size="small" label="Form adı" value={draft.name} onChange={event => commit({ ...draft, name: event.target.value })} /><TextField disabled={!metadataEditable} size="small" label="Kategori" value={draft.category} onChange={event => commit({ ...draft, category: event.target.value })} /><TextField disabled={!metadataEditable} size="small" multiline minRows={3} label="Açıklama" value={draft.description} onChange={event => commit({ ...draft, description: event.target.value })} /><TextField disabled={!editable} size="small" multiline minRows={3} label="Değişiklik özeti" value={draft.changeSummary} onChange={event => commit({ ...draft, changeSummary: event.target.value })} /></Stack>
          : <><Typography className="studio-panel-eyebrow">DOLDURULABİLİR ALANLAR</Typography><Typography variant="caption" color="text.secondary">Bir alan seçip belgeye ekleyin.</Typography><TextField size="small" fullWidth placeholder="Alan ara" value={paletteSearch} onChange={event => setPaletteSearch(event.target.value)} slotProps={{ input: { startAdornment: <SearchRounded fontSize="small" sx={{ mr: .5, color: 'text.secondary' }} /> } }} sx={{ my: 1.5 }} />
          <div className="studio-palette">{visiblePalette.map(item => <button key={item.type} className="studio-palette-item" disabled={!editable} draggable={editable} onDragStart={event => { event.dataTransfer.effectAllowed = 'copy'; event.dataTransfer.setData(paletteMime, item.type) }} onClick={() => addField(selectedSectionIndex, item.type, undefined, selection?.cellKey && selectedBlock?.type === 'table' ? { blockKey: selectedBlock.key, cellKey: selection.cellKey } : undefined)}>{item.icon}<span><span className="studio-palette-label">{item.label}</span><small>{item.description}</small></span><AddRounded className="studio-palette-plus" fontSize="small" /></button>)}</div>
          <div className="studio-selection-hint">{selection?.cellKey ? 'Yeni alan seçili hücrenin içine eklenir.' : dragMode === 'free' ? 'Tutamacı sürükleyin: öğe sayfada serbestçe taşınır. Köşesinden boyutlandırın.' : 'Tutamacı sürükleyin: öğelerin sırasını değiştirin.'}</div>
          <Typography className="studio-panel-eyebrow" sx={{ mt: 3 }}>BELGE YAPISI</Typography>
          {draft.schema.sections.map((section, index) => <button className="studio-outline-item" key={section.key} onClick={() => { const block = blocksFor(section)[0]; if (block) { selectItem({ blockKey: block.key }); viewportRef.current?.querySelector<HTMLElement>('[data-qms-section-key="' + section.key + '"]')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' }) } }}><span>{String(index + 1).padStart(2, '0')}</span>{section.title}</button>)}</>}
        </div>
      </aside>
      <Box ref={viewportRef} className="studio-viewport">
        <div className="studio-page-label"><span>A4 · {document.orientation === 'portrait' ? '210 × 297' : '297 × 210'} mm</span><button onClick={() => { setFitPage(true) }}><ZoomOutMapRounded sx={{ fontSize: 14 }} />Genişliğe sığdır</button></div>
        <Box sx={{ width: page.width * effectiveZoom / 100, mx: 'auto', pb: 5 }}>
          <HorizontalRuler width={page.width} zoom={effectiveZoom} />
          <Box sx={{ position: 'relative', width: page.width * effectiveZoom / 100, height: Math.max(page.height, pageHeight) * effectiveZoom / 100 }}>
            <Paper ref={pageRef} className="studio-page" elevation={0} sx={{ position: 'absolute', left: 0, top: 0, width: page.width, minHeight: page.height, transform: 'scale(' + effectiveZoom / 100 + ')', transformOrigin: 'top left', bgcolor: '#fff', borderRadius: 0 }}>
              <Box sx={{ minHeight: page.height, boxSizing: 'border-box', pl: document.marginLeft + 'mm', pr: document.marginRight + 'mm', pt: document.marginTop + 'mm', pb: document.marginBottom + 'mm', fontFamily: cssFont(document.defaultFontFamily), fontSize: document.defaultFontSize + 'pt' }}>
                <div className="studio-document-heading" style={{ borderBottomColor: draft.output.primaryColor, height: metrics.headerReserve + 'mm', boxSizing: 'border-box', marginBottom: 0 }}><span style={{ color: draft.output.primaryColor }}>KONTROLLÜ ELEKTRONİK FORM</span><h1>{draft.name || 'Adsız form'}</h1>{draft.description && <p>{draft.description}</p>}</div>
                <Box ref={bodyRef} className="studio-layout-body" data-qms-layout-body data-drag-mode={dragMode} data-body-width={metrics.width} data-body-height={metrics.height} onDragOver={event => { if (editable && dragMode === 'free') event.preventDefault() }} onDrop={event => { if (dragMode === 'free') handleDrop(event, selectedSectionIndex, blocksFor(draft.schema.sections[selectedSectionIndex]).length) }} sx={{ position: 'relative', width: metrics.width + 'mm', minHeight: metrics.height + 'mm' }}><Stack spacing={3}>{draft.schema.sections.map((section, sectionIndex) => <CanvasSection key={section.key} section={section} sectionIndex={sectionIndex} sectionCount={draft.schema.sections.length} selection={selection} editable={editable} dragTarget={dragTarget} onDragTarget={setDragTarget} onDrop={handleDrop} onCellDrop={handleCellDrop} onPointerDrop={handlePointerDrop} onSelect={selectItem} onActivateRich={editor => { activeRichEditor.current = editor }} onRichStyle={setRichSelectionStyle} onBlockChange={(key, updater) => patchBlock(key, updater)} onMoveBlock={moveBlockByStep} onDuplicateBlock={duplicateBlock} onDeleteBlock={deleteBlock} onSectionChange={value => updateSection(section.key, value)} onDeleteSection={() => deleteSection(section.key)} onMoveSection={direction => moveSection(section.key, direction)} />)}</Stack></Box>
                <div className="studio-document-footer" style={{ position: 'absolute', bottom: '6mm', left: document.marginLeft + 'mm', right: document.marginRight + 'mm', marginTop: 0 }}>{draft.output.footerText || 'Kontrollü elektronik kayıt'}</div>
              </Box>
            </Paper>
          </Box>
        </Box>
      </Box>
      <aside className="studio-inspector" hidden={!inspectorOpen}>
        <Tabs value={inspectorTab} onChange={(_, value: number) => setInspectorTab(value)} variant="fullWidth"><Tab label="Öğe özellikleri" /><Tab label="Sayfa / PDF" /></Tabs>
        {inspectorTab === 0 && selectedBlock && !selectedCell && <PositionProperties block={selectedBlock} metrics={metrics} editable={editable} onChange={position => patchBlock(selectedBlock.key, block => { const next = { ...block }; if (position) next.position = position; else delete next.position; return next })} />}
        {inspectorTab === 1 ? <OutputProperties draft={draft} editable={editable} onChange={commit} /> : selectedBlock?.type === 'text' ? <TextBlockProperties block={selectedBlock} editable={editable} onDuplicate={() => duplicateBlock(selectedBlock.key)} onDelete={() => deleteBlock(selectedBlock.key)} /> : selectedBlock?.type === 'table' && selectedCell ? <><button className="studio-context-link" onClick={() => setRibbon(3)}><TableChartRounded fontSize="small" />Tablo düzeni araçlarını aç</button><CellProperties cell={selectedCell} fields={selectedLocation?.section.fields ?? []} boundField={selectedField?.field ?? null} editable={editable} onCellChange={value => patchBlock(selectedBlock.key, block => block.type === 'table' ? { ...block, cells: block.cells.map(cell => cell.key === value.key ? { ...value, runs: value.text === cell.text ? value.runs : undefined } : cell) } : block)} onBind={fieldKey => bindCellField(selectedBlock.key, selectedCell.key, fieldKey)} onFieldChange={value => selectedField && patchField(selectedField.field.key, value)} /></> : selectedBlock?.type === 'table' ? <><button className="studio-context-link" onClick={() => setRibbon(3)}><TableChartRounded fontSize="small" />Tablo düzeni araçlarını aç</button><TableProperties block={selectedBlock} editable={editable} onResize={resizeSelectedTable} onChange={value => patchBlock(selectedBlock.key, () => value)} onDuplicate={() => duplicateBlock(selectedBlock.key)} onDelete={() => deleteBlock(selectedBlock.key)} /></> : selectedField ? <FieldProperties field={selectedField.field} allFields={draft.schema.sections.flatMap(section => section.fields)} editable={editable} canDelete={selectedField.section.fields.length > 1} onChange={value => patchField(selectedField.field.key, value)} onDuplicate={() => duplicateField(selectedField.field.key)} onDelete={() => deleteField(selectedField.field.key)} /> : <div className="studio-empty-inspector"><TuneRounded /><strong>Bir öğe seçin</strong><span>Özelliklerini düzenlemek için belge üzerindeki metne, tabloya veya alana tıklayın.</span></div>}
      </aside>
    </div>
    <footer className="studio-statusbar"><span>{draft.schema.sections.length} bölüm · {fieldKeys(draft.schema).length} alan</span><span>{selectedCell ? 'Hücre ' + (selectedCell.row + 1) + ':' + (selectedCell.column + 1) : selectedBlock?.type === 'text' ? 'Metin düzenleme' : 'Belge düzenleme'}</span><div className="studio-status-zoom"><IconButton size="small" aria-label="Uzaklaştır" onClick={() => { setZoom(Math.max(35, effectiveZoom - 10)); setFitPage(false) }}><ChevronLeftRounded fontSize="small" /></IconButton><span>%{effectiveZoom}</span><IconButton size="small" aria-label="Yakınlaştır" onClick={() => { setZoom(Math.min(150, effectiveZoom + 10)); setFitPage(false) }}><ChevronRightRounded fontSize="small" /></IconButton><button onClick={() => setFitPage(true)}>Sığdır</button></div></footer>
  </Box>
}

function PositionProperties({ block, metrics, editable, onChange }: { block: FormDocumentBlock; metrics: FormBodyMetrics; editable: boolean; onChange: (position: FormBlockPosition | null) => void }) {
  const position = block.position
  const convert = () => {
    const element = [...globalThis.document.querySelectorAll<HTMLElement>('[data-qms-block-key]')].find(item => item.dataset.qmsBlockKey === block.key)
    const body = element?.closest<HTMLElement>('[data-qms-layout-body]')
    if (!element || !body) return
    const rect = body.getBoundingClientRect()
    const scale = rect.width / (metrics.width * PIXELS_PER_MM)
    const next = positionFromRect(element.getBoundingClientRect(), rect, scale, metrics)
    onChange(clampPosition({ ...next, width: Math.min(next.width, block.type === 'table' ? metrics.width : 90) }, metrics))
  }
  return <Box className="studio-position-panel" sx={{ p: 2, borderBottom: '1px solid #e2e8f0' }}>
    <Typography variant="overline">SAYFADAKİ KONUM · mm</Typography>
    {position ? <>
      <Box key={`${block.key}:${JSON.stringify(position)}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1, mt: 1 }}>
        {(['x', 'y', 'width', 'height'] as const).map(key => <TextField key={key} size="small" type="number" label={{ x: 'X konumu', y: 'Y konumu', width: 'Genişlik (mm)', height: 'Yükseklik (mm)' }[key]} defaultValue={Number(position[key].toFixed(1))} disabled={!editable} slotProps={{ htmlInput: { step: 1, min: key === 'width' ? 15 : key === 'height' ? 5 : 0 } }} onKeyDown={event => { if (event.key === 'Enter') (event.target as HTMLInputElement).blur() }} onBlur={event => {
          const value = Number(event.target.value)
          if (!event.target.value.trim() || !Number.isFinite(value) || Math.abs(value - position[key]) < .05) return
          const next = key === 'width' || key === 'height' ? resizePosition(position, key === 'width' ? value : position.width, key === 'height' ? value : position.height, metrics) : clampPosition({ ...position, [key]: value }, metrics)
          onChange(next)
        }} />)}
      </Box>
      <Typography variant="caption" sx={{ display: 'block', mt: 1, color: 'text.secondary' }}>Tutamaktan taşıyın, sağ alt köşeden boyutlandırın. Tutamak seçiliyken ok tuşlarıyla hassas taşıyın.</Typography>
      <Button size="small" disabled={!editable} onClick={() => onChange(null)} sx={{ mt: 1 }}>Akış düzenine geri al</Button>
    </> : <><Typography variant="caption" sx={{ display: 'block', my: 1 }}>Tutamacı sayfanın boş bir noktasına sürükleyin.</Typography><Button size="small" disabled={!editable} onClick={convert}>Serbest kutuya dönüştür</Button></>}
  </Box>
}

function FreeResizeHandle({ payload, onDrop }: { payload: DragPayload; onDrop: (payload: DragPayload, target: PointerDropTarget) => void }) {
  const cleanup = useRef<(() => void) | null>(null)
  useEffect(() => () => cleanup.current?.(), [])
  return <button className="studio-free-resize" aria-label="Öğeyi boyutlandır" title="Köşeyi sürükleyerek boyutlandırın" onClick={event => event.stopPropagation()} onPointerDown={event => {
    if (event.button !== 0) return
    event.preventDefault(); event.stopPropagation()
    cleanup.current?.()
    cleanup.current = startFreeFormDrag({ handle: event.currentTarget, pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY, resize: true, onActive: () => {}, onCommit: position => onDrop(payload, { kind: 'free', position }) })
  }} />
}

function ToolbarButton({ icon, label, onClick, disabled }: { icon: ReactNode; label: string; onClick: () => void; disabled: boolean }) { return <Button size="small" startIcon={icon} disabled={disabled} onClick={onClick}>{label}</Button> }
function ColorTool({ label, icon, value, disabled, onChange }: { label: string; icon: ReactNode; value: string; disabled: boolean; onChange: (value: string) => void }) { return <Tooltip title={label}><Box component="label" sx={{ position: 'relative', display: 'inline-flex', cursor: disabled ? 'default' : 'pointer', opacity: disabled ? .38 : 1 }}><IconButton component="span" size="small" disabled={disabled} aria-label={label}>{icon}</IconButton><Box sx={{ position: 'absolute', left: 7, right: 7, bottom: 2, height: 3, bgcolor: value }} /><Box component="input" type="color" value={value} disabled={disabled} onChange={(event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value)} sx={{ position: 'absolute', width: 1, height: 1, opacity: 0 }} /></Box></Tooltip> }
function HorizontalRuler({ width, zoom }: { width: number; zoom: number }) { const tick = 20 * zoom / 100; return <Box sx={{ width: width * zoom / 100, height: 20, mb: 1, bgcolor: '#fff', border: 1, borderColor: 'divider', backgroundImage: `repeating-linear-gradient(90deg, transparent 0, transparent ${tick - 2}px, #94a3b8 ${tick - 1}px, transparent ${tick}px)`, position: 'relative' }}><Typography variant="caption" sx={{ position: 'absolute', left: 5, top: -1, color: 'text.secondary' }}>0</Typography></Box> }

interface CanvasSectionProps {
  section: FormSection
  sectionIndex: number
  sectionCount: number
  selection: Selection | null
  editable: boolean
  dragTarget: string | null
  onDragTarget: (value: string | null) => void
  onDrop: (event: DragEvent, sectionIndex: number, targetIndex: number) => void
  onCellDrop: (event: DragEvent, sectionIndex: number, blockKey: string, cellKey: string) => void
  onPointerDrop: (payload: DragPayload, target: PointerDropTarget) => void
  onSelect: (value: Selection) => void
  onActivateRich: (editor: Editor) => void
  onRichStyle: (style: FormTextStyle) => void
  onBlockChange: (key: string, updater: (block: FormDocumentBlock, section: FormSection) => FormDocumentBlock) => void
  onMoveBlock: (blockKey: string, direction: -1 | 1) => void
  onDuplicateBlock: (blockKey: string) => void
  onDeleteBlock: (blockKey: string) => void
  onSectionChange: (value: FormSection) => void
  onDeleteSection: () => void
  onMoveSection: (direction: -1 | 1) => void
}

function CanvasSection({ section, sectionIndex, sectionCount, selection, editable, dragTarget, onDragTarget, onDrop, onCellDrop, onPointerDrop, onSelect, onActivateRich, onRichStyle, onBlockChange, onMoveBlock, onDuplicateBlock, onDeleteBlock, onSectionChange, onDeleteSection, onMoveSection }: CanvasSectionProps) {
  const blocks = blocksFor(section)
  return <Box data-qms-section-index={sectionIndex} data-qms-section-key={section.key} data-qms-block-count={blocks.length} sx={{ position: 'static', border: '1px solid transparent', borderRadius: 1.5, transition: 'border-color 120ms', '&:hover': { borderColor: '#d9e1ea' } }}><Stack direction="row" sx={{ alignItems: 'center', mb: 1.25, gap: 1 }}><Box sx={{ flex: 1 }}>{editable ? <TextField fullWidth variant="standard" value={section.title} onChange={event => onSectionChange({ ...section, title: event.target.value })} slotProps={{ input: { sx: { fontWeight: 850, fontSize: 17 } } }} /> : <Typography sx={{ fontWeight: 850, fontSize: 17 }}>{section.title}</Typography>}{editable ? <TextField fullWidth variant="standard" placeholder="Bölüm açıklaması" value={section.description ?? ''} onChange={event => onSectionChange({ ...section, description: event.target.value })} slotProps={{ input: { sx: { fontSize: 12, color: 'text.secondary' } } }} /> : section.description && <Typography variant="body2" color="text.secondary">{section.description}</Typography>}</Box>{editable && <Stack direction="row" spacing={.1}><Tooltip title="Bölümü yukarı taşı"><span><IconButton size="small" disabled={sectionIndex === 0} onClick={() => onMoveSection(-1)}><ArrowUpwardRounded fontSize="small" /></IconButton></span></Tooltip><Tooltip title="Bölümü aşağı taşı"><span><IconButton size="small" disabled={sectionIndex === sectionCount - 1} onClick={() => onMoveSection(1)}><ArrowDownwardRounded fontSize="small" /></IconButton></span></Tooltip><Tooltip title="Bölümü sil"><span><IconButton size="small" color="error" disabled={sectionCount === 1} onClick={onDeleteSection}><DeleteOutlineRounded fontSize="small" /></IconButton></span></Tooltip></Stack>}</Stack><Box sx={{ display: 'grid', gridTemplateColumns: section.columns === 2 ? 'repeat(2, minmax(0, 1fr))' : '1fr', gap: 1.1 }}>{blocks.map((block, blockIndex) => { const target = `${section.key}:${blockIndex}`; return <Box key={block.key} sx={{ display: 'contents' }}><DropMarker active={dragTarget === target} sectionIndex={sectionIndex} targetIndex={blockIndex} visualId={target} onDragEnter={() => onDragTarget(target)} onDrop={event => onDrop(event, sectionIndex, blockIndex)} /><DocumentBlockCanvas block={block} blockIndex={blockIndex} blockCount={blocks.length} section={section} sectionIndex={sectionIndex} selected={selection?.blockKey === block.key} selectedCellKey={selection?.blockKey === block.key ? selection.cellKey : undefined} editable={editable} dragTarget={dragTarget} onDragTarget={onDragTarget} onPointerDrop={onPointerDrop} onSelect={cellKey => onSelect({ blockKey: block.key, ...(cellKey ? { cellKey } : {}) })} onChange={updater => onBlockChange(block.key, updater)} onCellDrop={onCellDrop} onActivateRich={onActivateRich} onRichStyle={onRichStyle} onMove={direction => onMoveBlock(block.key, direction)} onDuplicate={() => onDuplicateBlock(block.key)} onDelete={() => onDeleteBlock(block.key)} /></Box> })}<Box sx={{ gridColumn: '1 / -1' }}><DropZone active={dragTarget === `${section.key}:end`} editable={editable} sectionIndex={sectionIndex} targetIndex={blocks.length} visualId={`${section.key}:end`} onDragEnter={() => onDragTarget(`${section.key}:end`)} onDragLeave={() => onDragTarget(null)} onDrop={event => onDrop(event, sectionIndex, blocks.length)} /></Box></Box></Box>
}

interface DocumentBlockCanvasProps {
  block: FormDocumentBlock
  blockIndex: number
  blockCount: number
  section: FormSection
  sectionIndex: number
  selected: boolean
  selectedCellKey?: string
  editable: boolean
  dragTarget: string | null
  onDragTarget: (value: string | null) => void
  onPointerDrop: (payload: DragPayload, target: PointerDropTarget) => void
  onSelect: (cellKey?: string) => void
  onChange: (updater: (block: FormDocumentBlock, section: FormSection) => FormDocumentBlock) => void
  onCellDrop: (event: DragEvent, sectionIndex: number, blockKey: string, cellKey: string) => void
  onActivateRich: (editor: Editor) => void
  onRichStyle: (style: FormTextStyle) => void
  onMove: (direction: -1 | 1) => void
  onDuplicate: () => void
  onDelete: () => void
}

function DocumentBlockCanvas({ block, blockIndex, blockCount, section, sectionIndex, selected, selectedCellKey, editable, dragTarget, onDragTarget, onPointerDrop, onSelect, onChange, onCellDrop, onActivateRich, onRichStyle, onMove, onDuplicate, onDelete }: DocumentBlockCanvasProps) {
  const [previewWidths, setPreviewWidths] = useState<number[] | null>(null)
  const field = block.type === 'field' ? section.fields.find(item => item.key === block.fieldKey) : null
  const label = block.type === 'text' ? (block.style.fontSize >= 20 && block.style.bold ? 'Başlık' : 'Metin') : block.type === 'table' ? 'Tablo' : field?.label || 'Form alanı'
  const payload: DragPayload = block.type === 'field'
    ? { kind: 'placement', blockKey: block.key, fieldKey: block.fieldKey }
    : { kind: 'block', blockKey: block.key }
  const gridColumn = block.type === 'field' && field?.width === 6 ? undefined : '1 / -1'
  const position = block.position
  const chrome = editable && <BlockChrome label={label} payload={payload} selected={selected} canMoveUp={blockIndex > 0} canMoveDown={blockIndex < blockCount - 1} deleteDisabled={block.type === 'field' && section.fields.length === 1} onSelect={() => onSelect()} onDragTarget={onDragTarget} onPointerDrop={onPointerDrop} onMove={onMove} onDuplicate={onDuplicate} onDelete={onDelete} />
  const data = { 'data-qms-block-index': blockIndex, 'data-qms-block-key': block.key, 'data-qms-block-type': block.type, 'data-qms-section-index': sectionIndex, 'data-qms-position': position ? JSON.stringify(position) : undefined }
  const wrap = (content: ReactNode) => <Box sx={{ gridColumn, ...(position ? { minHeight: position.height + 'mm' } : { display: 'contents' }) }}>
    <Box {...data} className="studio-block" onClick={() => onSelect()} sx={{
      gridColumn, position: position ? 'absolute' : 'relative', borderRadius: '2px',
      ...(position ? { left: position.x + 'mm', top: position.y + 'mm', width: position.width + 'mm', minHeight: position.height + 'mm', zIndex: selected ? 5 : 2 } : {}),
      outline: selected ? '1px solid #3974dc' : '1px solid transparent', outlineOffset: 3,
      '&:hover .qms-block-tools': { opacity: 1, transform: 'translateY(0)' },
      bgcolor: block.type === 'text' ? block.style.backgroundColor : undefined,
    }}>{chrome}{content}{editable && selected && <FreeResizeHandle payload={payload} onDrop={onPointerDrop} />}</Box>
  </Box>
  if (block.type === 'text') return wrap(<StudioRichText label="Belge metni" text={block.text} runs={block.runs} style={block.style} editable={editable} onChange={value => onChange(current => current.type === 'text' ? { ...current, ...value } : current)} onActivate={editor => { onSelect(); onActivateRich(editor) }} onSelectionStyle={onRichStyle} />)
  if (block.type === 'table') {
    const widths = previewWidths ?? tableColumnWidths(block)
    const total = widths.reduce((sum, width) => sum + width, 0)
    return wrap(<div className="studio-table" style={{ gridTemplateColumns: widths.map(width => 'minmax(0, ' + width + 'fr)').join(' ') }}>
      {visibleTableCells(block).map(cell => <TableCellCanvas key={cell.key} cell={cell} block={block} section={section} sectionIndex={sectionIndex} selected={selectedCellKey === cell.key} editable={editable} dragTarget={dragTarget} onDragTarget={onDragTarget} onPointerDrop={onPointerDrop} onSelect={() => onSelect(cell.key)} onActivateRich={onActivateRich} onRichStyle={onRichStyle} onChange={value => onChange(current => current.type === 'table' ? { ...current, cells: current.cells.map(item => item.key === value.key ? value : item) } : current)} onDrop={event => onCellDrop(event, sectionIndex, block.key, cell.key)} />)}
      {editable && widths.slice(0, -1).map((_, index) => <ColumnResizer key={index} column={index} left={widths.slice(0, index + 1).reduce((sum, width) => sum + width, 0) / total * 100} widths={tableColumnWidths(block)} onPreview={setPreviewWidths} onCommit={columnWidths => { setPreviewWidths(null); onChange(current => current.type === 'table' ? { ...current, columnWidths } : current) }} />)}
    </div>)
  }
  return field ? wrap(<CanvasField field={field} style={block.style} selected={false} editable={editable} />) : null
}


function TableCellCanvas({ cell, block, section, sectionIndex, selected, editable, dragTarget, onDragTarget, onPointerDrop, onSelect, onActivateRich, onRichStyle, onChange, onDrop }: { cell: FormTableCell; block: FormTableBlock; section: FormSection; sectionIndex: number; selected: boolean; editable: boolean; dragTarget: string | null; onDragTarget: (value: string | null) => void; onPointerDrop: (payload: DragPayload, target: PointerDropTarget) => void; onSelect: () => void; onActivateRich: (editor: Editor) => void; onRichStyle: (style: FormTextStyle) => void; onChange: (value: FormTableCell) => void; onDrop: (event: DragEvent) => void }) {
  const cellRef = useRef<HTMLDivElement | null>(null)
  const field = cell.fieldKey ? section.fields.find(item => item.key === cell.fieldKey) : null
  const targetId = 'cell:' + sectionIndex + ':' + block.key + ':' + cell.key
  const handle = field && editable ? <PointerDragHandle label={field.label + ' alanını sürükle'} payload={{ kind: 'placement', blockKey: block.key, cellKey: cell.key, fieldKey: field.key }} onSelect={onSelect} onTargetChange={onDragTarget} onDrop={onPointerDrop} compact /> : null
  const tabToNextCell = (backwards: boolean) => {
    const table = cellRef.current?.closest('.studio-table')
    if (!table) return
    const entries = [...table.querySelectorAll<HTMLElement>('[data-qms-cell-key]')]
    const index = entries.findIndex(element => element === cellRef.current)
    const target = entries[index + (backwards ? -1 : 1)]
    target?.click()
    target?.querySelector<HTMLElement>('[contenteditable="true"],button')?.focus()
  }
  return <Box ref={cellRef} data-qms-cell-key={cell.key} data-qms-table-key={block.key} data-qms-section-index={sectionIndex} data-qms-cell-target={targetId} className="studio-table-cell" onClick={event => { event.stopPropagation(); onSelect() }} onDragOver={event => { if (editable) event.preventDefault() }} onDrop={onDrop} sx={{ gridColumn: (cell.column + 1) + ' / span ' + (cell.colSpan ?? 1), gridRow: (cell.row + 1) + ' / span ' + (cell.rowSpan ?? 1), minWidth: 0, minHeight: 48, p: field ? .8 : 1, borderStyle: 'solid', borderColor: block.borderColor, borderWidth: block.borderWidth + 'pt', outline: dragTarget === targetId || selected ? '2px solid #3974dc' : 'none', outlineOffset: -2, zIndex: selected ? 1 : 0, bgcolor: cell.backgroundColor }}>
    {field ? <CanvasField field={field} style={cell.style} selected={selected} editable={editable} compact action={handle} /> : <StudioRichText label={'Tablo hücresi ' + (cell.row + 1) + '-' + (cell.column + 1)} text={cell.text} runs={cell.runs} style={cell.style} editable={editable} onChange={value => onChange({ ...cell, ...value })} onActivate={editor => { onSelect(); onActivateRich(editor) }} onSelectionStyle={onRichStyle} onTab={tabToNextCell} />}
  </Box>
}

function ColumnResizer({ column, left, widths, onPreview, onCommit }: { column: number; left: number; widths: number[]; onPreview: (widths: number[] | null) => void; onCommit: (widths: number[]) => void }) {
  const cleanupRef = useRef<(() => void) | null>(null)
  useEffect(() => () => cleanupRef.current?.(), [])
  return <div className="studio-column-resizer" role="separator" aria-label={'Sütun ' + (column + 1) + ' genişliğini değiştir'} aria-orientation="vertical" tabIndex={0} style={{ left: left + '%' }} onKeyDown={event => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return
    event.preventDefault()
    const next = [...widths]
    const delta = event.key === 'ArrowLeft' ? -2 : 2
    if (next[column] + delta < 5 || next[column + 1] - delta < 5) return
    next[column] += delta; next[column + 1] -= delta
    onCommit(next)
  }} onPointerDown={event => {
    if (event.button !== 0) return
    event.preventDefault(); event.stopPropagation()
    const table = event.currentTarget.closest('.studio-table')
    if (!table) return
    const startX = event.clientX
    const width = table.getBoundingClientRect().width
    const total = widths.reduce((sum, value) => sum + value, 0)
    let next = [...widths]
    const move = (nativeEvent: PointerEvent) => {
      const delta = Math.max(5 - widths[column], Math.min(widths[column + 1] - 5, (nativeEvent.clientX - startX) / width * total))
      next = [...widths]
      next[column] += delta; next[column + 1] -= delta
      onPreview(next)
    }
    const finish = () => { cleanup(); onCommit(next) }
    const cancel = () => { cleanup(); onPreview(null) }
    const cleanup = () => {
      window.removeEventListener('pointermove', move)
      window.removeEventListener('pointerup', finish)
      window.removeEventListener('pointercancel', cancel)
      cleanupRef.current = null
    }
    cleanupRef.current?.()
    cleanupRef.current = cleanup
    window.addEventListener('pointermove', move)
    window.addEventListener('pointerup', finish)
    window.addEventListener('pointercancel', cancel)
  }} />
}

function DropMarker({ active, sectionIndex, targetIndex, visualId }: { active: boolean; sectionIndex: number; targetIndex: number; visualId: string; onDragEnter: () => void; onDrop: (event: DragEvent) => void }) {
  const [bounds, setBounds] = useState<{ left: number; top: number; width: number } | null>(null)
  useEffect(() => {
    if (!active) return
    const measure = () => {
      const section = [...globalThis.document.querySelectorAll<HTMLElement>('[data-qms-section-key]')].find(item => Number(item.dataset.qmsSectionIndex) === sectionIndex)
      const target = section && [...section.querySelectorAll<HTMLElement>('[data-qms-block-index]')].find(item => Number(item.dataset.qmsBlockIndex) === targetIndex)
      if (!section || !target) return
      const rect = target.getBoundingClientRect()
      setBounds({ left: rect.left, top: rect.top - 5, width: rect.width })
    }
    measure()
    globalThis.document.addEventListener('scroll', measure, true)
    return () => globalThis.document.removeEventListener('scroll', measure, true)
  }, [active, sectionIndex, targetIndex])
  return active && bounds ? createPortal(<Box data-qms-visual-id={visualId} sx={{ position: 'fixed', ...bounds, height: 3, zIndex: 1600, pointerEvents: 'none', bgcolor: '#3974dc', boxShadow: '0 0 0 2px #ffffff', borderRadius: '2px' }} />, globalThis.document.body) : null
}

function DropZone({ active, editable, sectionIndex, targetIndex, visualId, onDragEnter, onDragLeave, onDrop }: { active: boolean; editable: boolean; sectionIndex: number; targetIndex: number; visualId: string; onDragEnter: () => void; onDragLeave: () => void; onDrop: (event: DragEvent) => void }) { return <Box data-qms-position-index={targetIndex} data-qms-section-index={sectionIndex} data-qms-visual-id={visualId} data-qms-drop-end onDragOver={event => { if (!editable) return; event.preventDefault(); onDragEnter() }} onDragLeave={onDragLeave} onDrop={onDrop} sx={{ py: 1, border: '1px dashed', borderColor: active ? 'primary.main' : '#cbd5e1', bgcolor: active ? 'rgba(0, 158, 247, .08)' : '#f8fafc', borderRadius: 1.25, textAlign: 'center', transition: '120ms' }}><Typography variant="caption" color={active ? 'primary.main' : 'text.secondary'} sx={{ fontWeight: active ? 800 : 500 }}>{active ? 'Buraya bırakın' : 'Öğeleri tutamağından sürükleyerek yerleştirin'}</Typography></Box> }
function CanvasField({ field, style, selected, editable, compact = false, action }: { field: FormField; style: FormTextStyle; selected: boolean; editable: boolean; compact?: boolean; action?: ReactNode }) {
  return <Box data-qms-field-preview data-qms-editable={editable} sx={{ p: compact ? .25 : .75, cursor: 'default', outline: selected && !compact ? '1px solid #3974dc' : '1px solid transparent', outlineOffset: 3, borderRadius: '2px', bgcolor: style.backgroundColor, position: 'relative' }}>
    {action && <Box sx={{ position: 'absolute', right: 4, top: 3, zIndex: 2 }}>{action}</Box>}
    <Stack direction="row" spacing={.5} sx={{ alignItems: 'center', pr: action ? 3 : 0, mb: .75 }}>
      <Typography sx={{ fontFamily: cssFont(style.fontFamily), fontSize: style.fontSize + 'pt', fontWeight: style.bold ? 700 : 400, fontStyle: style.italic ? 'italic' : 'normal', textDecoration: style.underline ? 'underline' : 'none', textAlign: style.alignment, color: style.color }}>{field.label || 'Adsız alan'}</Typography>
      {field.required && <Typography color="error.main">*</Typography>}
      {!compact && selected && <Chip size="small" variant="outlined" label={typeLabel(field.type)} sx={{ ml: 'auto', height: 18, fontSize: 9, border: 0, bgcolor: '#edf3fb', color: '#8191a7' }} />}
    </Stack>
    <FieldMock field={field} compact={compact} />
  </Box>
}

function BlockChrome({ label, payload, selected, canMoveUp, canMoveDown, deleteDisabled, onSelect, onDragTarget, onPointerDrop, onMove, onDuplicate, onDelete }: { label: string; payload: DragPayload; selected: boolean; canMoveUp: boolean; canMoveDown: boolean; deleteDisabled: boolean; onSelect: () => void; onDragTarget: (value: string | null) => void; onPointerDrop: (payload: DragPayload, target: PointerDropTarget) => void; onMove: (direction: -1 | 1) => void; onDuplicate: () => void; onDelete: () => void }) {
  return <><Box className="studio-block-grip"><PointerDragHandle label={`${label} öğesini sürükle`} payload={payload} onSelect={onSelect} onTargetChange={onDragTarget} onDrop={onPointerDrop} /></Box><Paper className="qms-block-tools" elevation={4} onClick={event => event.stopPropagation()} sx={{ position: 'absolute', right: -3, top: -39, zIndex: 8, height: 32, display: 'flex', alignItems: 'center', px: .25, border: 1, borderColor: 'rgba(0, 158, 247, .35)', borderRadius: 1.5, opacity: selected ? 1 : 0, pointerEvents: selected ? 'auto' : 'none', transform: selected ? 'translateY(0)' : 'translateY(3px)', transition: 'opacity 120ms, transform 120ms', bgcolor: 'rgba(255,255,255,.98)' }}>

    <Divider orientation="vertical" flexItem sx={{ my: .5, mx: .25 }} />
    <Tooltip title="Yukarı taşı"><span><IconButton size="small" aria-label={`${label} öğesini yukarı taşı`} disabled={!canMoveUp} onClick={() => onMove(-1)}><ArrowUpwardRounded sx={{ fontSize: 17 }} /></IconButton></span></Tooltip>
    <Tooltip title="Aşağı taşı"><span><IconButton size="small" aria-label={`${label} öğesini aşağı taşı`} disabled={!canMoveDown} onClick={() => onMove(1)}><ArrowDownwardRounded sx={{ fontSize: 17 }} /></IconButton></span></Tooltip>
    <Tooltip title="Kopyala"><IconButton size="small" aria-label={`${label} öğesini kopyala`} onClick={onDuplicate}><ContentCopyRounded sx={{ fontSize: 16 }} /></IconButton></Tooltip>
    <Tooltip title="Sil"><span><IconButton size="small" color="error" aria-label={`${label} öğesini sil`} disabled={deleteDisabled} onClick={onDelete}><DeleteOutlineRounded sx={{ fontSize: 17 }} /></IconButton></span></Tooltip>
  </Paper></>
}

interface PointerGesture {
  pointerId: number
  startX: number
  startY: number
  active: boolean
  target: PointerDropTarget | null
  previousCursor: string
  previousUserSelect: string
  captureElement: HTMLButtonElement
  cleanup: () => void
}

function PointerDragHandle({ label, payload, onSelect, onTargetChange, onDrop, compact = false }: { label: string; payload: DragPayload; onSelect: () => void; onTargetChange: (value: string | null) => void; onDrop: (payload: DragPayload, target: PointerDropTarget) => void; compact?: boolean }) {
  const freeCleanup = useRef<(() => void) | null>(null)
  const gesture = useRef<PointerGesture | null>(null)
  const [dragging, setDragging] = useState(false)

  const updateGesture = (pointerId: number, clientX: number, clientY: number) => {
    const current = gesture.current
    if (!current || current.pointerId !== pointerId) return
    if (!current.active && Math.hypot(clientX - current.startX, clientY - current.startY) < 5) return
    if (!current.active) {
      current.active = true
      setDragging(true)
      if (globalThis.document?.body) {
        globalThis.document.body.style.cursor = 'grabbing'
        globalThis.document.body.style.userSelect = 'none'
      }
    }
    current.target = resolvePointerDropTarget(clientX, clientY, payload)
    onTargetChange(current.target && 'visualId' in current.target ? current.target.visualId : null)
  }

  const finishGesture = (pointerId: number, clientX: number, clientY: number, shouldDrop: boolean) => {
    const current = gesture.current
    if (!current || current.pointerId !== pointerId) return
    if (current.active && shouldDrop) {
      const target = resolvePointerDropTarget(clientX, clientY, payload)
      if (target) onDrop(payload, target)
    }
    current.cleanup()
    try { current.captureElement.releasePointerCapture?.(pointerId) } catch { /* Global listeners remain the reliable fallback. */ }
    if (globalThis.document?.body) {
      globalThis.document.body.style.cursor = current.previousCursor
      globalThis.document.body.style.userSelect = current.previousUserSelect
    }
    gesture.current = null
    setDragging(false)
    onTargetChange(null)
  }

  const begin = (event: ReactPointerEvent<HTMLButtonElement>) => {
    if (event.button !== 0) return
    event.preventDefault()
    event.stopPropagation()
    onSelect()
    const captureElement = event.currentTarget
    if (captureElement.closest<HTMLElement>('[data-qms-layout-body]')?.dataset.dragMode === 'free') {
      freeCleanup.current?.()
      freeCleanup.current = startFreeFormDrag({
        handle: captureElement, pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY, fromCell: Boolean(payload.cellKey), onActive: setDragging,
        onCommit: (position, cell) => {
          if (payload.kind === 'placement' && cell && cell.dataset.qmsCellKey !== payload.cellKey) {
            onDrop(payload, { kind: 'cell', sectionIndex: Number(cell.dataset.qmsSectionIndex), blockKey: cell.dataset.qmsTableKey!, cellKey: cell.dataset.qmsCellKey!, visualId: cell.dataset.qmsCellTarget ?? '' })
          } else onDrop(payload, { kind: 'free', position })
        },
      })
      if (freeCleanup.current) return
    }
    const nativeMove = (nativeEvent: PointerEvent) => {
      updateGesture(nativeEvent.pointerId, nativeEvent.clientX, nativeEvent.clientY)
      if (gesture.current?.active) nativeEvent.preventDefault()
    }
    const nativeUp = (nativeEvent: PointerEvent) => finishGesture(nativeEvent.pointerId, nativeEvent.clientX, nativeEvent.clientY, true)
    const nativeCancel = (nativeEvent: PointerEvent) => finishGesture(nativeEvent.pointerId, nativeEvent.clientX, nativeEvent.clientY, false)
    const cleanup = () => {
      globalThis.document?.removeEventListener('pointermove', nativeMove)
      globalThis.document?.removeEventListener('pointerup', nativeUp)
      globalThis.document?.removeEventListener('pointercancel', nativeCancel)
    }
    gesture.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      active: false,
      target: null,
      previousCursor: globalThis.document?.body.style.cursor ?? '',
      previousUserSelect: globalThis.document?.body.style.userSelect ?? '',
      captureElement,
      cleanup,
    }
    globalThis.document?.addEventListener('pointermove', nativeMove, { passive: false })
    globalThis.document?.addEventListener('pointerup', nativeUp)
    globalThis.document?.addEventListener('pointercancel', nativeCancel)
    try { captureElement.setPointerCapture?.(event.pointerId) } catch { /* Document-level listeners keep dragging active. */ }
  }

  useEffect(() => () => {
    freeCleanup.current?.()
    const current = gesture.current
    if (!current) return
    current.cleanup()
    if (globalThis.document?.body) {
      globalThis.document.body.style.cursor = current.previousCursor
      globalThis.document.body.style.userSelect = current.previousUserSelect
    }
  }, [])

  return <Tooltip title="Tutun ve sürükleyin"><IconButton size="small" aria-label={label} onPointerDown={begin} onKeyDown={event => {
    if (!event.key.startsWith('Arrow')) return
    const source = event.currentTarget.closest<HTMLElement>('[data-qms-block-key]')
    const body = event.currentTarget.closest<HTMLElement>('[data-qms-layout-body]')
    if (!source?.dataset.qmsPosition || !body || payload.cellKey) return
    event.preventDefault(); event.stopPropagation()
    onDrop(payload, { kind: 'free', position: movePositionWithKey(JSON.parse(source.dataset.qmsPosition) as FormBlockPosition, event.key, { width: Number(body.dataset.bodyWidth), height: Number(body.dataset.bodyHeight) }, event.shiftKey ? 5 : 1) })
  }} sx={{ width: compact ? 25 : 28, height: compact ? 25 : 28, touchAction: 'none', cursor: dragging ? 'grabbing' : 'grab', color: dragging ? 'primary.main' : 'text.secondary', bgcolor: dragging ? 'rgba(0, 158, 247, .1)' : 'transparent' }}><DragIndicatorRounded sx={{ fontSize: compact ? 17 : 19 }} /></IconButton></Tooltip>
}

function resolvePointerDropTarget(clientX: number, clientY: number, payload: DragPayload): PointerDropTarget | null {
  return resolveFlowDropTarget(clientX, clientY, payload)
}

function FieldMock({ field, compact }: { field: FormField; compact: boolean }) {
  if (field.type === 'checkbox') return <Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: 'text.secondary' }}><Checkbox size="small" disabled /><Typography variant="body2">Onay</Typography></Stack>
  const placeholder = field.type === 'date' ? 'gg.aa.yyyy'
    : field.type === 'dateTime' ? 'gg.aa.yyyy ss:dd'
    : field.type === 'number' ? `0${field.unit ? ` ${field.unit}` : ''}`
    : field.type === 'singleSelect' || field.type === 'multiSelect' ? field.options[0]?.label ?? 'Seçenek belirleyin'
    : field.placeholder || (field.type === 'longText' ? 'Uzun metin girilecek alan' : 'Metin girilecek alan')
  return <Box sx={{ minHeight: field.type === 'longText' ? (compact ? 40 : 54) : (compact ? 26 : 32), px: .75, py: .5, borderBottom: '1px solid #b6c2d1', bgcolor: '#f8fafc', display: 'flex', alignItems: field.type === 'longText' ? 'flex-start' : 'center', color: '#97a3b3', fontSize: '9pt' }}>
    {placeholder}{(field.type === 'singleSelect' || field.type === 'multiSelect') && <Typography sx={{ ml: 'auto' }}>⌄</Typography>}
  </Box>
}

function TextBlockProperties({ block, editable, onDuplicate, onDelete }: { block: Extract<FormDocumentBlock, { type: 'text' }>; editable: boolean; onDuplicate: () => void; onDelete: () => void }) { return <Stack spacing={2} sx={{ p: 2 }}><Box><Typography variant="overline" sx={{ fontWeight: 850 }}>Metin bloğu</Typography><Typography variant="h6" sx={{ fontWeight: 850 }}>Belge metni</Typography></Box><Stack direction="row" spacing={1}><Button fullWidth variant="outlined" startIcon={<ContentCopyRounded />} disabled={!editable} onClick={onDuplicate}>Kopyala</Button><Button fullWidth variant="outlined" color="error" startIcon={<DeleteOutlineRounded />} disabled={!editable} onClick={onDelete}>Sil</Button></Stack><TextField multiline minRows={6} label="Metin" value={block.text} slotProps={{ input: { readOnly: true } }} helperText="Metni doğrudan sayfa üzerinde düzenleyin." /><Alert severity="info">Yazı tipi, boyut, renk ve hizalamayı üst araç çubuğundan değiştirebilirsiniz.</Alert></Stack> }
export function TableProperties({ block, editable, onResize, onChange, onDuplicate, onDelete }: { block: FormTableBlock; editable: boolean; onResize: (rows: number, columns: number) => void; onChange: (value: FormTableBlock) => void; onDuplicate: () => void; onDelete: () => void }) {
  const [headerError, setHeaderError] = useState<{ key: string; message: string } | null>(null)
  const changeHeaderRows = (count: number) => {
    if (!Number.isInteger(count) || count < 0 || count > block.rows) {
      setHeaderError({ key: block.key, message: `0 ile ${block.rows} arasında bir tam sayı girin.` })
      return
    }
    const next = setTableHeaderRows(block, count)
    if (next === block && count !== block.headerRows) {
      setHeaderError({ key: block.key, message: 'Önce başlık sınırındaki birleştirilmiş hücreyi bölün.' })
      return
    }
    setHeaderError(null)
    onChange(next)
  }
  const error = headerError?.key === block.key ? headerError.message : ''
  return <Stack spacing={2} sx={{ p: 2 }}>
    <Box><Typography variant="overline" sx={{ fontWeight: 850 }}>Tablo</Typography><Typography variant="h6" sx={{ fontWeight: 850 }}>{block.rows} × {block.columns} tablo</Typography><Typography variant="body2" color="text.secondary">Düzenlemek veya alan yerleştirmek için bir hücre seçin.</Typography></Box>
    <Stack direction="row" spacing={1}><Button fullWidth variant="outlined" startIcon={<ContentCopyRounded />} disabled={!editable} onClick={onDuplicate}>Kopyala</Button><Button fullWidth variant="outlined" color="error" startIcon={<DeleteOutlineRounded />} disabled={!editable} onClick={onDelete}>Sil</Button></Stack>
    <Stack direction="row" spacing={1}><TextField type="number" size="small" label="Satır" value={block.rows} disabled={!editable} onChange={event => onResize(Number(event.target.value), block.columns)} slotProps={{ htmlInput: { min: 1, max: 20 } }} /><TextField type="number" size="small" label="Sütun" value={block.columns} disabled={!editable} onChange={event => onResize(block.rows, Number(event.target.value))} slotProps={{ htmlInput: { min: 1, max: 10 } }} /></Stack>
    <TextField type="number" size="small" label="Başlık satırı" value={block.headerRows} disabled={!editable} onChange={event => changeHeaderRows(Number(event.target.value))} error={Boolean(error)} helperText={error || 'Üstteki başlık satırları her PDF sayfasında tekrar eder.'} slotProps={{ htmlInput: { min: 0, max: block.rows } }} />
    <TextField type="number" size="small" label="Kenarlık kalınlığı" value={block.borderWidth} disabled={!editable} onChange={event => onChange({ ...block, borderWidth: Math.max(0, Math.min(6, Number(event.target.value))) })} slotProps={{ htmlInput: { min: 0, max: 6, step: .5 } }} />
    <TextField type="color" size="small" label="Kenarlık rengi" value={block.borderColor} disabled={!editable} onChange={event => onChange({ ...block, borderColor: event.target.value })} />
  </Stack>
}
function CellProperties({ cell, fields, boundField, editable, onCellChange, onBind, onFieldChange }: { cell: FormTableCell; fields: FormField[]; boundField: FormField | null; editable: boolean; onCellChange: (value: FormTableCell) => void; onBind: (fieldKey: string | null) => void; onFieldChange: (value: FormField) => void }) { return <Stack spacing={2} sx={{ p: 2 }}><Box><Typography variant="overline" sx={{ fontWeight: 850 }}>Tablo hücresi</Typography><Typography variant="h6" sx={{ fontWeight: 850 }}>Satır {cell.row + 1} · Sütun {cell.column + 1}</Typography></Box><TextField select size="small" disabled={!editable} label="Hücre içeriği" value={cell.fieldKey ?? ''} onChange={event => onBind(event.target.value || null)}><MenuItem value="">Statik metin</MenuItem>{fields.map(field => <MenuItem key={field.key} value={field.key}>Form alanı · {field.label}</MenuItem>)}</TextField>{!cell.fieldKey && <TextField multiline minRows={4} label="Hücre metni" value={cell.text} slotProps={{ input: { readOnly: true } }} helperText="Metni doğrudan sayfa üzerinde düzenleyin." />}<TextField type="color" size="small" disabled={!editable} label="Hücre arka planı" value={cell.backgroundColor} onChange={event => onCellChange({ ...cell, backgroundColor: event.target.value })} /><Alert severity="info">Hücre yazısını üst araç çubuğundan biçimlendirin. Sol paletten bir alanı tıklarsanız doğrudan bu hücreye eklenir.</Alert>{boundField && <><Divider>BAĞLI FORM ALANI</Divider><FieldSettings field={boundField} allFields={fields} editable={editable} onChange={onFieldChange} /></>}</Stack> }
function FieldProperties({ field, allFields, editable, canDelete, onChange, onDuplicate, onDelete }: { field: FormField; allFields: FormField[]; editable: boolean; canDelete: boolean; onChange: (value: FormField) => void; onDuplicate: () => void; onDelete: () => void }) { return <Stack spacing={2} sx={{ p: 2 }}><Box><Typography variant="overline" sx={{ fontWeight: 850 }}>Form alanı</Typography><Typography variant="h6" sx={{ fontWeight: 850 }}>{field.label || 'Adsız alan'}</Typography><Typography variant="caption" color="text.secondary">Sistem anahtarı: {field.key}</Typography></Box><Stack direction="row" spacing={1}><Button fullWidth variant="outlined" startIcon={<ContentCopyRounded />} disabled={!editable} onClick={onDuplicate}>Kopyala</Button><Button fullWidth variant="outlined" color="error" startIcon={<DeleteOutlineRounded />} disabled={!editable || !canDelete} onClick={onDelete}>Sil</Button></Stack><Divider /><FieldSettings field={field} allFields={allFields} editable={editable} onChange={onChange} /></Stack> }

function FieldSettings({ field, allFields, editable, onChange }: { field: FormField; allFields: FormField[]; editable: boolean; onChange: (value: FormField) => void }) {
  const conditionSources = allFields.filter(item => item.key !== field.key && ['shortText', 'singleSelect', 'checkbox'].includes(item.type))
  const visibilitySource = conditionSources.find(item => item.key === field.visibilityCondition?.fieldKey)
  const requiredSource = conditionSources.find(item => item.key === field.requiredCondition?.fieldKey)
  const setType = (type: FormFieldType) => onChange({ ...field, type, options: type === 'singleSelect' || type === 'multiSelect' ? field.options.length ? field.options : [{ value: 'secenek1', label: 'Seçenek 1' }, { value: 'secenek2', label: 'Seçenek 2' }] : [], maxLength: type === 'shortText' ? 500 : type === 'longText' ? 10000 : null })
  return <Stack spacing={2}><TextField disabled={!editable} fullWidth size="small" label="Alan etiketi" value={field.label} onChange={event => onChange({ ...field, label: event.target.value })} /><TextField disabled={!editable} select fullWidth size="small" label="Alan türü" value={field.type} onChange={event => setType(event.target.value as FormFieldType)}>{palette.map(item => <MenuItem key={item.type} value={item.type}>{item.label}</MenuItem>)}</TextField><TextField disabled={!editable} select fullWidth size="small" label="Alan genişliği" value={field.width} onChange={event => onChange({ ...field, width: Number(event.target.value) as 6 | 12 })}><MenuItem value={6}>Yarım genişlik</MenuItem><MenuItem value={12}>Tam genişlik</MenuItem></TextField><FormControlLabel disabled={!editable} control={<Checkbox checked={field.required} onChange={event => onChange({ ...field, required: event.target.checked, requiredCondition: event.target.checked ? null : field.requiredCondition })} />} label="Her zaman zorunlu" /><TextField disabled={!editable} fullWidth size="small" label="Yardım metni" value={field.helpText ?? ''} onChange={event => onChange({ ...field, helpText: event.target.value })} /><TextField disabled={!editable} fullWidth size="small" label="Yer tutucu" value={field.placeholder ?? ''} onChange={event => onChange({ ...field, placeholder: event.target.value })} />{(field.type === 'shortText' || field.type === 'longText') && <TextField disabled={!editable} fullWidth size="small" type="number" label="Azami karakter" value={field.maxLength ?? ''} onChange={event => onChange({ ...field, maxLength: event.target.value ? Number(event.target.value) : null })} />}{field.type === 'number' && <><Stack direction="row" spacing={1}><TextField disabled={!editable} fullWidth size="small" type="number" label="Minimum" value={field.min ?? ''} onChange={event => onChange({ ...field, min: event.target.value ? Number(event.target.value) : null })} /><TextField disabled={!editable} fullWidth size="small" type="number" label="Maksimum" value={field.max ?? ''} onChange={event => onChange({ ...field, max: event.target.value ? Number(event.target.value) : null })} /></Stack><TextField disabled={!editable} fullWidth size="small" label="Birim" value={field.unit ?? ''} onChange={event => onChange({ ...field, unit: event.target.value })} /></>}{(field.type === 'singleSelect' || field.type === 'multiSelect') && <TextField disabled={!editable} fullWidth size="small" multiline minRows={4} label="Seçenekler" helperText="Her satıra bir seçenek yazın." value={field.options.map(item => item.label).join('\n')} onChange={event => onChange({ ...field, options: optionsFromLines(event.target.value) })} />}<Divider><Typography variant="caption">KOŞULLAR</Typography></Divider><TextField disabled={!editable} select fullWidth size="small" label="Yalnız şu alan…" value={field.visibilityCondition?.fieldKey ?? ''} onChange={event => onChange({ ...field, visibilityCondition: event.target.value ? { fieldKey: event.target.value, operator: 'equals', value: sourceDefault(conditionSources.find(item => item.key === event.target.value)) } : null })}><MenuItem value="">Her zaman görünür</MenuItem>{conditionSources.map(item => <MenuItem key={item.key} value={item.key}>{item.label}</MenuItem>)}</TextField>{field.visibilityCondition && <ConditionEditor condition={field.visibilityCondition} source={visibilitySource} disabled={!editable} onChange={value => onChange({ ...field, visibilityCondition: value })} />}<TextField disabled={!editable || field.required} select fullWidth size="small" label="Şu koşulda zorunlu…" value={field.requiredCondition?.fieldKey ?? ''} onChange={event => onChange({ ...field, requiredCondition: event.target.value ? { fieldKey: event.target.value, operator: 'equals', value: sourceDefault(conditionSources.find(item => item.key === event.target.value)) } : null })}><MenuItem value="">Koşula bağlı değil</MenuItem>{conditionSources.map(item => <MenuItem key={item.key} value={item.key}>{item.label}</MenuItem>)}</TextField>{field.requiredCondition && <ConditionEditor condition={field.requiredCondition} source={requiredSource} disabled={!editable || field.required} onChange={value => onChange({ ...field, requiredCondition: value })} />}</Stack>
}

function ConditionEditor({ condition, source, disabled, onChange }: { condition: FormCondition; source?: FormField; disabled: boolean; onChange: (value: FormCondition) => void }) { return <Stack direction="row" spacing={1}><TextField disabled={disabled} select fullWidth size="small" label="Koşul" value={condition.operator} onChange={event => onChange({ ...condition, operator: event.target.value as FormCondition['operator'] })}><MenuItem value="equals">Eşitse</MenuItem><MenuItem value="notEquals">Eşit değilse</MenuItem><MenuItem value="hasValue">Doluysa</MenuItem></TextField>{condition.operator !== 'hasValue' && (source?.type === 'singleSelect' ? <TextField disabled={disabled} select fullWidth size="small" label="Değer" value={String(condition.value ?? '')} onChange={event => onChange({ ...condition, value: event.target.value })}>{source.options.map(item => <MenuItem key={item.value} value={item.value}>{item.label}</MenuItem>)}</TextField> : source?.type === 'checkbox' ? <TextField disabled={disabled} select fullWidth size="small" label="Değer" value={String(condition.value ?? false)} onChange={event => onChange({ ...condition, value: event.target.value === 'true' })}><MenuItem value="true">Evet</MenuItem><MenuItem value="false">Hayır</MenuItem></TextField> : <TextField disabled={disabled} fullWidth size="small" label="Değer" value={String(condition.value ?? '')} onChange={event => onChange({ ...condition, value: event.target.value })} />)}</Stack> }

function OutputProperties({ draft, editable, onChange }: { draft: DesignerDraft; editable: boolean; onChange: (value: DesignerDraft) => void }) { const document = draft.schema.document ?? defaultDocumentSettings(); const updateDocument = (patch: Partial<typeof document>) => onChange({ ...draft, schema: { ...materializeSchema(draft.schema), document: { ...document, ...patch } } }); return <Stack spacing={2} sx={{ p: 2 }}><Box><Typography variant="overline" sx={{ fontWeight: 850 }}>Sayfa ve çıktı</Typography><Typography variant="h6" sx={{ fontWeight: 850 }}>A4 belge ayarları</Typography></Box><TextField select disabled={!editable} size="small" label="Sayfa yönü" value={document.orientation} onChange={event => updateDocument({ orientation: event.target.value as 'portrait' | 'landscape' })}><MenuItem value="portrait">Dikey</MenuItem><MenuItem value="landscape">Yatay</MenuItem></TextField><Stack direction="row" spacing={1}><TextField disabled={!editable} type="number" size="small" label="Üst mm" value={document.marginTop} onChange={event => updateDocument({ marginTop: Number(event.target.value) })} /><TextField disabled={!editable} type="number" size="small" label="Alt mm" value={document.marginBottom} onChange={event => updateDocument({ marginBottom: Number(event.target.value) })} /></Stack><Stack direction="row" spacing={1}><TextField disabled={!editable} type="number" size="small" label="Sol mm" value={document.marginLeft} onChange={event => updateDocument({ marginLeft: Number(event.target.value) })} /><TextField disabled={!editable} type="number" size="small" label="Sağ mm" value={document.marginRight} onChange={event => updateDocument({ marginRight: Number(event.target.value) })} /></Stack><Divider>PDF</Divider><TextField disabled={!editable} size="small" label="PDF başlığı" value={draft.output.title} onChange={event => onChange({ ...draft, output: { ...draft.output, title: event.target.value } })} /><TextField disabled={!editable} size="small" label="Altbilgi" value={draft.output.footerText} onChange={event => onChange({ ...draft, output: { ...draft.output, footerText: event.target.value } })} /><TextField disabled={!editable} size="small" label="Kurumsal renk" type="color" value={draft.output.primaryColor} onChange={event => onChange({ ...draft, output: { ...draft.output, primaryColor: event.target.value } })} /><FormControlLabel disabled={!editable} control={<Checkbox checked={draft.output.includeEmptyFields} onChange={event => onChange({ ...draft, output: { ...draft.output, includeEmptyFields: event.target.checked } })} />} label="Boş alanları göster" /><FormControlLabel disabled={!editable} control={<Checkbox checked={draft.output.includeSignatures} onChange={event => onChange({ ...draft, output: { ...draft.output, includeSignatures: event.target.checked } })} />} label="E-imzaları ekle" /><FormControlLabel disabled={!editable} control={<Checkbox checked={draft.output.includeAuditTrail} onChange={event => onChange({ ...draft, output: { ...draft.output, includeAuditTrail: event.target.checked } })} />} label="Denetim izini ekle" /></Stack> }

function createField(type: FormFieldType, schema: ElectronicFormSchema, order: number): FormField { const baseLabel = palette.find(item => item.type === type)?.label ?? 'Yeni alan'; const key = uniqueKey(slug(baseLabel), fieldKeys(schema)); const options = type === 'singleSelect' || type === 'multiSelect' ? [{ value: 'secenek1', label: 'Seçenek 1' }, { value: 'secenek2', label: 'Seçenek 2' }] : []; return { key, label: baseLabel, type, required: false, helpText: '', placeholder: '', width: 12, order, maxLength: type === 'shortText' ? 500 : type === 'longText' ? 10000 : null, min: null, max: null, unit: '', options, visibilityCondition: null, requiredCondition: null } }
function findBlock(schema: ElectronicFormSchema, key: string) { for (let sectionIndex = 0; sectionIndex < schema.sections.length; sectionIndex++) { const section = schema.sections[sectionIndex]; const blocks = blocksFor(section); const blockIndex = blocks.findIndex(block => block.key === key); if (blockIndex >= 0) return { section, sectionIndex, block: blocks[blockIndex], blockIndex } } return null }
function findField(schema: ElectronicFormSchema, key: string) { for (let sectionIndex = 0; sectionIndex < schema.sections.length; sectionIndex++) { const section = schema.sections[sectionIndex]; const fieldIndex = section.fields.findIndex(field => field.key === key); if (fieldIndex >= 0) return { section, sectionIndex, field: section.fields[fieldIndex], fieldIndex } } return null }
function releaseCellField(section: FormSection, table: FormTableBlock, cell: FormTableCell) { if (!cell.fieldKey) return; const tableIndex = section.blocks!.findIndex(block => block.key === table.key); section.blocks!.splice(tableIndex + 1, 0, createFieldBlock(cell.fieldKey, section.blocks!.map(block => block.key))); cell.fieldKey = null }

function normalizeSections(sections: FormSection[]) { return sections.map((section, index) => ({ ...section, order: index + 1, fields: section.fields.map((field, fieldIndex) => ({ ...field, order: fieldIndex + 1 })), blocks: normalizeBlocks(section.blocks?.length ? section.blocks : blocksFor(section)) })) }
function fieldKeys(schema: ElectronicFormSchema) { return schema.sections.flatMap(section => section.fields.map(field => field.key)) }
function sectionKeys(schema: ElectronicFormSchema) { return schema.sections.map(section => section.key) }
function slug(value: string) { const clean = value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-zA-Z0-9]+/g, ' ').trim().split(/\s+/); if (!clean.length) return 'alan'; return clean.map((part, index) => index === 0 ? part.toLocaleLowerCase('tr-TR') : part[0].toLocaleUpperCase('tr-TR') + part.slice(1)).join('').replace(/^[^a-zA-Z]+/, '') || 'alan' }
function sourceDefault(field?: FormField): unknown { if (field?.type === 'checkbox') return true; return field?.options[0]?.value ?? '' }
function optionsFromLines(value: string) { const used: string[] = []; return value.split('\n').map(item => item.trim()).filter(Boolean).map((label, index) => { const optionValue = uniqueKey(slug(label) || `secenek${index + 1}`, used); used.push(optionValue); return { value: optionValue, label } }) }
function typeLabel(type: FormFieldType) { return palette.find(item => item.type === type)?.label ?? type }
