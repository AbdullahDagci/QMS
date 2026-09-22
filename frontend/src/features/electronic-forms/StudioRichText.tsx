import { useLayoutEffect, useRef, type CSSProperties } from 'react'
import { Box } from '@mui/material'
import { EditorContent, useEditor, type Editor, type JSONContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { TextStyle } from '@tiptap/extension-text-style'
import type { FormTextRun, FormTextStyle } from '../../api/electronicForms'
import { cssFont, defaultTextStyle, fontFamilies } from './formDocumentModel'

interface RichTextValue { text: string; runs: FormTextRun[] }

interface StudioRichTextProps {
  text: string
  runs?: FormTextRun[]
  style: FormTextStyle
  editable: boolean
  label: string
  onChange: (value: RichTextValue) => void
  onActivate?: (editor: Editor) => void
  onSelectionStyle?: (style: FormTextStyle) => void
  onTab?: (backwards: boolean) => void
}

function safeFont(value: unknown, fallback = 'Arial') {
  const name = typeof value === 'string' ? value.split(',')[0].trim().replace(/^['"]|['"]$/g, '') : ''
  return fontFamilies.find(font => font.toLowerCase() === name.toLowerCase()) ?? fallback
}

function safeColor(value: unknown, fallback: string): string {
  if (typeof value !== 'string') return fallback
  if (/^#[0-9a-f]{6}$/i.test(value)) return value.toLowerCase()
  if (/^#[0-9a-f]{3}$/i.test(value)) return `#${value.slice(1).split('').map(char => char + char).join('')}`.toLowerCase()
  const rgb = value.match(/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*1)?\s*\)$/)
  return rgb ? `#${rgb.slice(1, 4).map(channel => Math.min(255, Number(channel)).toString(16).padStart(2, '0')).join('')}` : fallback
}

function safeSize(value: unknown, fallback = 11): number {
  if (typeof value === 'string' && !/^\d+(?:\.\d+)?(?:pt|px)?$/.test(value)) return fallback
  const size = typeof value === 'number' ? value : Number.parseFloat(String(value)) * (String(value).endsWith('px') ? 0.75 : 1)
  return Number.isFinite(size) ? Math.max(6, Math.min(72, Math.round(size * 100) / 100)) : fallback
}

function highlightColor(value: unknown): string | undefined {
  const color = safeColor(value, '#ffffff')
  return color === '#ffffff' ? undefined : color
}

function safeStyle(style: Partial<FormTextStyle>, base = defaultTextStyle()): FormTextStyle {
  return {
    fontFamily: safeFont(style.fontFamily, base.fontFamily),
    fontSize: safeSize(style.fontSize, base.fontSize),
    bold: style.bold ?? base.bold,
    italic: style.italic ?? base.italic,
    underline: style.underline ?? base.underline,
    alignment: base.alignment,
    color: safeColor(style.color, base.color),
    backgroundColor: safeColor(style.backgroundColor, base.backgroundColor),
  }
}

const StudioTextStyle = TextStyle.extend({
  addAttributes() {
    return {
      fontFamily: {
        default: null,
        parseHTML: element => element.style.fontFamily ? safeFont(element.style.fontFamily) : null,
        renderHTML: attributes => attributes.fontFamily ? { style: `font-family: ${safeFont(attributes.fontFamily)}` } : {},
      },
      fontSize: {
        default: null,
        parseHTML: element => element.style.fontSize ? safeSize(element.style.fontSize) : null,
        renderHTML: attributes => attributes.fontSize ? { style: `font-size: ${safeSize(attributes.fontSize)}pt` } : {},
      },
      color: {
        default: null,
        parseHTML: element => element.style.color ? safeColor(element.style.color, '#172033') : null,
        renderHTML: attributes => attributes.color ? { style: `color: ${safeColor(attributes.color, '#172033')}` } : {},
      },
      backgroundColor: {
        default: null,
        parseHTML: element => element.style.backgroundColor ? safeColor(element.style.backgroundColor, '#ffffff') : null,
        renderHTML: attributes => highlightColor(attributes.backgroundColor) ? { style: `background-color: ${highlightColor(attributes.backgroundColor)}` } : {},
      },
    }
  },
})

const extensions = [
  StarterKit.configure({
    blockquote: false, bulletList: false, code: false, codeBlock: false, heading: false,
    horizontalRule: false, link: false, listItem: false, listKeymap: false, orderedList: false,
    strike: false, dropcursor: false, gapcursor: false, trailingNode: false, undoRedo: false,
  }),
  StudioTextStyle,
]

function appendRun(runs: FormTextRun[], text: string, style: FormTextStyle) {
  if (!text) return
  const previous = runs.at(-1)
  if (previous && JSON.stringify(previous.style) === JSON.stringify(style)) previous.text += text
  else runs.push({ text, style })
}

function normalizedValue(text: string, runs: FormTextRun[] | undefined, style: FormTextStyle): RichTextValue {
  const result: FormTextRun[] = []
  const source = runs?.length && runs.map(run => run.text).join('') === text ? runs : [{ text, style }]
  for (const run of source) appendRun(result, run.text, safeStyle(run.style, style))
  return { text, runs: result }
}

function marksFor(style: FormTextStyle): NonNullable<JSONContent['marks']> {
  const marks: NonNullable<JSONContent['marks']> = [{ type: 'textStyle', attrs: {
    fontFamily: style.fontFamily, fontSize: style.fontSize, color: style.color, backgroundColor: style.backgroundColor,
  } }]
  if (style.bold) marks.push({ type: 'bold' })
  if (style.italic) marks.push({ type: 'italic' })
  if (style.underline) marks.push({ type: 'underline' })
  return marks
}

function documentFor(value: RichTextValue): JSONContent {
  const content: JSONContent[] = []
  for (const run of value.runs) {
    run.text.split('\n').forEach((line, index) => {
      if (index) content.push({ type: 'hardBreak', marks: marksFor(run.style) })
      if (line) content.push({ type: 'text', text: line, marks: marksFor(run.style) })
    })
  }
  return { type: 'doc', content: [{ type: 'paragraph', content }] }
}

function styleFromMarks(marks: NonNullable<JSONContent['marks']>, base: FormTextStyle) {
  const attributes = marks.find(mark => mark.type === 'textStyle')?.attrs ?? {}
  return safeStyle({ ...attributes, bold: marks.some(mark => mark.type === 'bold'), italic: marks.some(mark => mark.type === 'italic'), underline: marks.some(mark => mark.type === 'underline') }, base)
}

function valueFromEditor(editor: Editor, base: FormTextStyle): RichTextValue {
  const runs: FormTextRun[] = []
  const paragraphs = (editor.getJSON() as JSONContent).content ?? []
  paragraphs.forEach((paragraph, index) => {
    if (index) appendRun(runs, '\n', runs.at(-1)?.style ?? safeStyle(base, base))
    for (const node of paragraph.content ?? []) {
      if (node.type === 'text' || node.type === 'hardBreak') appendRun(runs, node.type === 'hardBreak' ? '\n' : node.text ?? '', styleFromMarks(node.marks ?? [], base))
    }
  })
  return { text: runs.map(run => run.text).join(''), runs }
}

function selectionStyle(editor: Editor, base: FormTextStyle): FormTextStyle {
  const marks = editor.state.storedMarks ?? editor.state.selection.$from.marks()
  return styleFromMarks(marks.map(mark => ({ type: mark.type.name, attrs: mark.attrs })), base)
}

/** Returns false when the caller should apply the style to the entire block instead. */
export function applyRichTextStyle(editor: Editor, patch: Partial<FormTextStyle>): boolean {
  if (editor.isDestroyed || !editor.isEditable || editor.state.selection.empty) return false
  const attributes: Record<string, string | number> = {}
  if (patch.fontFamily !== undefined) attributes.fontFamily = safeFont(patch.fontFamily)
  if (patch.fontSize !== undefined) attributes.fontSize = safeSize(patch.fontSize)
  if (patch.color !== undefined) attributes.color = safeColor(patch.color, '#172033')
  if (patch.backgroundColor !== undefined) attributes.backgroundColor = safeColor(patch.backgroundColor, '#ffffff')
  if (!Object.keys(attributes).length && patch.bold === undefined && patch.italic === undefined && patch.underline === undefined) return false
  let chain = editor.chain().focus(undefined, { scrollIntoView: false })
  if (Object.keys(attributes).length) chain = chain.setMark('textStyle', attributes)
  if (patch.bold !== undefined) chain = patch.bold ? chain.setBold() : chain.unsetBold()
  if (patch.italic !== undefined) chain = patch.italic ? chain.setItalic() : chain.unsetItalic()
  if (patch.underline !== undefined) chain = patch.underline ? chain.setUnderline() : chain.unsetUnderline()
  return chain.run()
}

export function StudioRichText({ text, runs, style, editable, label, onChange, onActivate, onSelectionStyle, onTab }: StudioRichTextProps) {
  const value = normalizedValue(text, runs, style)
  const signature = JSON.stringify(value)
  const lastIncomingSignature = useRef(signature)
  const activate = (editor: Editor) => {
    if (!editor.isFocused) return
    onActivate?.(editor)
    onSelectionStyle?.(selectionStyle(editor, style))
  }
  const editor = useEditor({
    extensions,
    editable,
    content: documentFor(value),
    shouldRerenderOnTransaction: false,
    editorProps: {
      attributes: { role: 'textbox', 'aria-label': label, 'aria-multiline': 'true', spellcheck: 'true' },
      handleKeyDown: (_view, event) => {
        if (event.key === 'Tab' && onTab) { event.preventDefault(); onTab(event.shiftKey); return true }
        return false
      },
    },
    onFocus: ({ editor: focusedEditor }) => {
      if (focusedEditor.isEmpty && !focusedEditor.state.storedMarks) {
        focusedEditor.view.dispatch(focusedEditor.state.tr.setStoredMarks(marksFor(safeStyle(style, style)).map(mark => focusedEditor.schema.mark(mark.type, mark.attrs))))
      }
      activate(focusedEditor)
    },
    onSelectionUpdate: ({ editor: activeEditor }) => activate(activeEditor),
    onUpdate: ({ editor: activeEditor }) => {
      onChange(valueFromEditor(activeEditor, style))
      activate(activeEditor)
    },
  })

  useLayoutEffect(() => {
    if (!editor || editor.isDestroyed) return
    editor.setEditable(editable, false)
    editor.setOptions({ editorProps: { ...editor.options.editorProps, attributes: { role: 'textbox', 'aria-label': label, 'aria-multiline': 'true', spellcheck: 'true' } } })
    // Selection updates can rerender the parent before ProseMirror has consumed a DOM edit.
    // Only a new incoming value may replace the document; an unchanged snapshot must not move the caret.
    if (lastIncomingSignature.current === signature) return
    lastIncomingSignature.current = signature
    if (JSON.stringify(valueFromEditor(editor, style)) === signature) return
    const { from, to } = editor.state.selection
    editor.commands.setContent(documentFor(JSON.parse(signature) as RichTextValue), { emitUpdate: false })
    const end = editor.state.doc.content.size - 1
    editor.commands.setTextSelection({ from: Math.max(1, Math.min(from, end)), to: Math.max(1, Math.min(to, end)) })
  }, [editor, editable, label, signature, style])

  return <Box sx={{
    width: '100%', minWidth: 0,
    '& .tiptap': {
      outline: 'none', minHeight: '1.5em', overflowWrap: 'anywhere', whiteSpace: 'pre-wrap',
      fontFamily: cssFont(style.fontFamily), fontSize: `${style.fontSize}pt`, lineHeight: 1.5,
      fontWeight: 400, fontStyle: 'normal', textDecoration: 'none', color: style.color, textAlign: style.alignment,
      '& p': { margin: 0, minHeight: '1.5em' }, '& strong': { fontWeight: 700 },
    },
  }}><EditorContent editor={editor} /></Box>
}

export function RichTextDisplay({ text, runs, style }: Pick<StudioRichTextProps, 'text' | 'runs' | 'style'>) {
  const value = normalizedValue(text, runs, style)
  return <div style={{ ...displayStyle(style), backgroundColor: undefined, whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', minHeight: '1.5em', lineHeight: 1.5 }}>
    {value.runs.map((run, index) => <span key={index} style={displayStyle(run.style)}>{run.text}</span>)}
  </div>
}

function displayStyle(style: FormTextStyle): CSSProperties {
  return {
    fontFamily: cssFont(style.fontFamily), fontSize: `${style.fontSize}pt`, fontWeight: style.bold ? 700 : 400,
    fontStyle: style.italic ? 'italic' : 'normal', textDecoration: style.underline ? 'underline' : 'none',
    textAlign: style.alignment, color: style.color, backgroundColor: highlightColor(style.backgroundColor),
  }
}
