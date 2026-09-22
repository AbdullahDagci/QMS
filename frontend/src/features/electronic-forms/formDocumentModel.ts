import type {
  ElectronicFormSchema,
  FormDocumentBlock,
  FormDocumentSettings,
  FormFieldBlock,
  FormSection,
  FormTableBlock,
  FormTableCell,
  FormTextStyle,
} from '../../api/electronicForms'

export const fontFamilies = ['Arial', 'Calibri', 'Georgia', 'Times New Roman', 'Verdana'] as const

export const defaultTextStyle = (overrides: Partial<FormTextStyle> = {}): FormTextStyle => ({
  fontFamily: 'Arial',
  fontSize: 11,
  bold: false,
  italic: false,
  underline: false,
  alignment: 'left',
  color: '#172033',
  backgroundColor: '#ffffff',
  ...overrides,
})

export const defaultDocumentSettings = (): FormDocumentSettings => ({
  pageSize: 'A4',
  orientation: 'portrait',
  marginTop: 18,
  marginRight: 18,
  marginBottom: 18,
  marginLeft: 18,
  defaultFontFamily: 'Arial',
  defaultFontSize: 11,
})

export function blocksFor(section: FormSection): FormDocumentBlock[] {
  if (section.blocks?.length) return [...section.blocks].sort((left, right) => left.order - right.order)
  return [...section.fields].sort((left, right) => left.order - right.order).map((field, index): FormFieldBlock => ({
    key: uniqueKey(`blok${capitalize(field.key)}`, []),
    type: 'field',
    order: index + 1,
    fieldKey: field.key,
    style: defaultTextStyle({ fontSize: 10, bold: true }),
  }))
}

export function materializeSchema(schema: ElectronicFormSchema): ElectronicFormSchema {
  return {
    ...schema,
    document: schema.document ?? defaultDocumentSettings(),
    sections: schema.sections.map(section => ({ ...section, blocks: normalizeBlocks(blocksFor(section)) })),
  }
}

export function normalizeBlocks(blocks: FormDocumentBlock[]): FormDocumentBlock[] {
  return blocks.map((block, index) => ({ ...block, order: index + 1 }))
}

export function createTextBlock(existingKeys: string[], variant: 'paragraph' | 'heading' = 'paragraph'): FormDocumentBlock {
  return {
    key: uniqueKey('metinBlogu', existingKeys),
    type: 'text',
    order: existingKeys.length + 1,
    text: variant === 'heading' ? 'Yeni başlık' : 'Metninizi buraya yazın.',
    style: defaultTextStyle(variant === 'heading' ? { fontSize: 18, bold: true, color: '#0f766e' } : {}),
  }
}

export function createFieldBlock(fieldKey: string, existingKeys: string[]): FormFieldBlock {
  return {
    key: uniqueKey(`blok${capitalize(fieldKey)}`, existingKeys),
    type: 'field',
    order: existingKeys.length + 1,
    fieldKey,
    style: defaultTextStyle({ fontSize: 10, bold: true }),
  }
}

export function createTableBlock(existingKeys: string[], rows = 3, columns = 3): FormTableBlock {
  const key = uniqueKey('tablo', existingKeys)
  const cells: FormTableCell[] = []
  for (let row = 0; row < rows; row++) {
    for (let column = 0; column < columns; column++) {
      cells.push({
        key: `${key}Hucre${row + 1}_${column + 1}`,
        row,
        column,
        text: row === 0 ? `Başlık ${column + 1}` : '',
        fieldKey: null,
        style: defaultTextStyle({ fontSize: 10, bold: row === 0, alignment: row === 0 ? 'center' : 'left' }),
        backgroundColor: row === 0 ? '#eaf4f2' : '#ffffff',
      })
    }
  }
  return { key, type: 'table', order: existingKeys.length + 1, rows, columns, headerRows: 1, borderColor: '#94a3b8', borderWidth: 1, cells }
}

export function resizeTable(block: FormTableBlock, rows: number, columns: number): FormTableBlock {
  const nextRows = Math.max(1, Math.min(20, rows))
  const nextColumns = Math.max(1, Math.min(10, columns))
  const cells: FormTableCell[] = []
  for (let row = 0; row < nextRows; row++) {
    for (let column = 0; column < nextColumns; column++) {
      const current = block.cells.find(cell => cell.row === row && cell.column === column)
      cells.push(current ?? {
        key: uniqueKey(`${block.key}Hucre${row + 1}_${column + 1}`, cells.map(cell => cell.key)),
        row,
        column,
        text: row < block.headerRows ? `Başlık ${column + 1}` : '',
        fieldKey: null,
        style: defaultTextStyle({ fontSize: 10, bold: row < block.headerRows, alignment: row < block.headerRows ? 'center' : 'left' }),
        backgroundColor: row < block.headerRows ? '#eaf4f2' : '#ffffff',
      })
    }
  }
  return { ...block, rows: nextRows, columns: nextColumns, headerRows: Math.min(block.headerRows, nextRows), cells }
}

export function blockKeys(schema: ElectronicFormSchema): string[] {
  return schema.sections.flatMap(section => blocksFor(section).map(block => block.key))
}

export function cssFont(fontFamily: string) {
  return fontFamily.includes(' ') ? `"${fontFamily}", sans-serif` : `${fontFamily}, sans-serif`
}

export function uniqueKey(base: string, existing: string[]) {
  let key = base || 'oge'
  let suffix = 2
  while (existing.includes(key)) key = `${base}${suffix++}`
  return key
}

function capitalize(value: string) { return value ? value[0].toLocaleUpperCase('tr-TR') + value.slice(1) : 'Alan' }
