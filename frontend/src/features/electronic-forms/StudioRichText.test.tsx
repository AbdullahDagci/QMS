import { useState } from 'react'
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Editor } from '@tiptap/react'
import type { FormTextRun } from '../../api/electronicForms'
import { defaultTextStyle } from './formDocumentModel'
import { applyRichTextStyle, RichTextDisplay, StudioRichText } from './StudioRichText'

afterEach(cleanup)

const style = defaultTextStyle()

describe('StudioRichText', () => {
  it('formats only selected text and keeps the selection when the controlled value is echoed back', async () => {
    let editor: Editor | undefined
    let latest = { text: 'Kalite kontrol formu', runs: [] as FormTextRun[] }
    function Harness() {
      const [value, setValue] = useState(latest)
      return <StudioRichText {...value} style={style} label="Metin" editable onActivate={value => { editor = value }} onChange={value => { latest = value; setValue(value) }} />
    }
    render(<Harness />)
    fireEvent.focus(screen.getByRole('textbox', { name: 'Metin' }))
    await waitFor(() => expect(editor).toBeDefined())

    act(() => {
      editor!.commands.setTextSelection({ from: 1, to: 7 })
      expect(applyRichTextStyle(editor!, { bold: true, fontFamily: 'Georgia', fontSize: 16, color: '#cc0000' })).toBe(true)
    })

    expect(latest.text).toBe('Kalite kontrol formu')
    expect(latest.runs.map(run => [run.text, run.style.bold])).toEqual([['Kalite', true], [' kontrol formu', false]])
    expect(latest.runs[0].style).toMatchObject({ fontFamily: 'Georgia', fontSize: 16, color: '#cc0000' })
    expect(editor!.state.selection.from).toBe(1)
    expect(editor!.state.selection.to).toBe(7)

    act(() => {
      editor!.commands.setTextSelection(7)
      expect(applyRichTextStyle(editor!, { italic: true })).toBe(false)
      editor!.commands.insertContent('!')
    })
    expect(latest.text).toBe('Kalite! kontrol formu')
    expect(editor!.state.selection.from).toBe(8)
  })

  it('restores an externally undone value without emitting another change or remounting the focused editor', async () => {
    let editor: Editor | undefined
    const changed = vi.fn()
    const props = { style, label: 'Paragraf', editable: true, onChange: changed, onActivate: (value: Editor) => { editor = value } }
    const { rerender } = render(<StudioRichText {...props} text="İlk metin" />)
    fireEvent.focus(screen.getByRole('textbox', { name: 'Paragraf' }))
    await waitFor(() => expect(editor).toBeDefined())
    const originalEditor = editor

    act(() => {
      editor!.commands.setTextSelection({ from: 1, to: 4 })
      applyRichTextStyle(editor!, { underline: true })
    })
    const formatted = changed.mock.calls[0][0] as { text: string; runs: FormTextRun[] }
    rerender(<StudioRichText {...props} {...formatted} />)
    expect(screen.getByRole('textbox').querySelector('u')).not.toBeNull()
    const numberOfChanges = changed.mock.calls.length

    rerender(<StudioRichText {...props} text="İlk metin" />)
    expect(screen.getByRole('textbox').querySelector('u')).toBeNull()
    expect(screen.getByRole('textbox')).toHaveTextContent('İlk metin')
    expect(changed).toHaveBeenCalledTimes(numberOfChanges)
    expect(editor).toBe(originalEditor)
    expect(editor!.state.selection.from).toBe(1)
    expect(editor!.state.selection.to).toBe(4)
  })

  it('round-trips paragraph breaks and a literal HTML string without interpreting it as markup', async () => {
    let editor: Editor | undefined
    const changed = vi.fn()
    const text = 'Üst satır\n\n<img src=x onerror=alert(1)>\n'
    render(<StudioRichText text={text} style={style} label="Güvenli metin" editable onChange={changed} onActivate={value => { editor = value }} />)
    const textbox = screen.getByRole('textbox')
    expect(textbox.querySelector('img')).toBeNull()
    expect(textbox.textContent).toContain('<img src=x onerror=alert(1)>')
    fireEvent.focus(textbox)
    await waitFor(() => expect(editor).toBeDefined())
    act(() => {
      editor!.commands.selectAll()
      applyRichTextStyle(editor!, { italic: true })
    })
    const result = changed.mock.calls[0][0] as { text: string; runs: FormTextRun[] }
    expect(result.text).toBe(text)
    expect(result.runs).toHaveLength(1)
    expect(result.runs[0].style.italic).toBe(true)
  })

  it('renders mixed read-only formats in points and forwards Tab to cell navigation', async () => {
    const tab = vi.fn()
    const { rerender } = render(<RichTextDisplay text="İmza" style={style} runs={[{ text: 'İm', style: { ...style, fontSize: 18, bold: true } }, { text: 'za', style }]} />)
    expect(screen.getByText('İm').style.fontSize).toBe('18pt')
    expect(screen.getByText('İm').style.fontWeight).toBe('700')
    expect(screen.getByText('za').style.fontSize).toBe('11pt')
    expect(screen.getByText('za').style.fontWeight).toBe('400')
    rerender(<StudioRichText text="İmza" style={style} label="Hücre" editable onChange={() => {}} onTab={tab} />)
    fireEvent.keyDown(screen.getByRole('textbox'), { key: 'Tab', shiftKey: true })
    expect(tab).toHaveBeenCalledWith(true)
  })

  it('starts an empty heading with its base format and sanitizes pasted fonts and colors', async () => {
    let editor: Editor | undefined
    const changed = vi.fn()
    render(<StudioRichText text="" style={{ ...style, fontSize: 24, bold: true }} label="Yeni başlık" editable onChange={changed} onActivate={value => { editor = value }} />)
    fireEvent.focus(screen.getByRole('textbox'))
    await waitFor(() => expect(editor).toBeDefined())
    act(() => { editor!.commands.insertContent('Başlık') })
    expect(changed.mock.lastCall![0].runs[0].style).toMatchObject({ bold: true, fontSize: 24 })
    act(() => {
      editor!.commands.selectAll()
      editor!.commands.insertContent('<p><span style="font-family: Unsupported; color: rgb(255, 0, 0); font-size: 200px">Kayıt</span></p>')
    })
    expect(changed.mock.lastCall![0].runs[0].style).toMatchObject({ fontFamily: 'Arial', color: '#ff0000', fontSize: 72 })
  })

  it('lets table backgrounds show through default text while preserving explicit highlights in editor and preview', () => {
    const runs = [{ text: 'Başlık ', style }, { text: 'önemli', style: { ...style, backgroundColor: '#fff200' } }]
    const { rerender } = render(<div style={{ backgroundColor: '#eaf4f2' }}><StudioRichText text="Başlık önemli" runs={runs} style={style} label="Tablo başlığı" editable onChange={() => {}} /></div>)
    const spans = screen.getByRole('textbox').querySelectorAll('span')
    expect(spans[0].style.backgroundColor).toBe('')
    expect(spans[1].style.backgroundColor).toBe('rgb(255, 242, 0)')

    rerender(<div style={{ backgroundColor: '#eaf4f2' }}><RichTextDisplay text="Başlık önemli" runs={runs} style={style} /></div>)
    expect(screen.getByText('Başlık').style.backgroundColor).toBe('')
    expect(screen.getByText('Başlık').parentElement!.style.backgroundColor).toBe('')
    expect(screen.getByText('önemli').style.backgroundColor).toBe('rgb(255, 242, 0)')
  })
})
