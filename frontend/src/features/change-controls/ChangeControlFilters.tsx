import { useState } from 'react'
import { TextField } from '@mui/material'
import { AdvancedFilterPanel } from '../../components/AdvancedFilterPanel'
import { SearchableSelect, type SelectOption } from '../../components/SearchableSelect'
import { emptyChangeControlFilters, toChangeControlColumnFilters, type ChangeControlFilterState } from './changeControlFilterModel'

export function ChangeControlFilters({ value, statusOptions, typeOptions, riskOptions, regulatoryOptions, onApply }: { value: ChangeControlFilterState; statusOptions: Array<SelectOption<string>>; typeOptions: Array<SelectOption<string>>; riskOptions: Array<SelectOption<string>>; regulatoryOptions: Array<SelectOption<string>>; onApply: (value: ChangeControlFilterState) => void }) {
  const [draft, setDraft] = useState(value)
  const update = <K extends keyof ChangeControlFilterState>(field: K, next: ChangeControlFilterState[K]) => setDraft(current => ({ ...current, [field]: next }))
  return <AdvancedFilterPanel activeCount={toChangeControlColumnFilters(draft).length} onClear={() => { setDraft(emptyChangeControlFilters); onApply(emptyChangeControlFilters) }} onApply={() => onApply(draft)}>
    <TextField size="small" label="Değişiklik numarası içerir" value={draft.recordNumber} onChange={e => update('recordNumber', e.target.value)} />
    <TextField size="small" label="Kaynak DÖF içerir" value={draft.sourceRecordNumber} onChange={e => update('sourceRecordNumber', e.target.value)} />
    <TextField size="small" label="Başlık içerir" value={draft.title} onChange={e => update('title', e.target.value)} />
    <TextField size="small" label="Sorumlu içerir" value={draft.owner} onChange={e => update('owner', e.target.value)} />
    <SearchableSelect label="Değişiklik türü" value={draft.changeType} options={[{ value: '', label: 'Tüm türler' }, ...typeOptions]} onChange={next => update('changeType', next ?? '')} />
    <SearchableSelect label="Durum" value={draft.status} options={[{ value: '', label: 'Tüm durumlar' }, ...statusOptions]} onChange={next => update('status', next ?? '')} />
    <SearchableSelect label="Risk" value={draft.riskLevel} options={[{ value: '', label: 'Tüm riskler' }, ...riskOptions]} onChange={next => update('riskLevel', next ?? '')} />
    <SearchableSelect label="Ruhsat etkisi" value={draft.regulatoryImpact} options={[{ value: '', label: 'Tüm etkiler' }, ...regulatoryOptions]} onChange={next => update('regulatoryImpact', next ?? '')} />
    <TextField size="small" label="Hedef tarih başlangıç" type="date" value={draft.targetFrom} onChange={e => update('targetFrom', e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
    <TextField size="small" label="Hedef tarih bitiş" type="date" value={draft.targetTo} onChange={e => update('targetTo', e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
  </AdvancedFilterPanel>
}
