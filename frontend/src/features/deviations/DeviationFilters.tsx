import { useState } from 'react'
import { TextField } from '@mui/material'
import { SearchableMultiSelect, SearchableSelect, type SelectOption } from '../../components/SearchableSelect'
import { AdvancedFilterPanel } from '../../components/AdvancedFilterPanel'
import { emptyDeviationFilters, type DeviationFilterState } from './deviationFilterModel'

const classificationOptions: Array<SelectOption<string>> = [
  { value: 'Minor', label: 'Minör' },
  { value: 'Major', label: 'Majör' },
  { value: 'Critical', label: 'Kritik' },
]

const statusOptions: Array<SelectOption<string>> = [
  { value: 'Draft', label: 'Taslak' },
  { value: 'Submitted', label: 'Gönderildi' },
  { value: 'PreliminaryReview', label: 'Ön inceleme' },
  { value: 'Investigation', label: 'Araştırma' },
  { value: 'ImpactAssessment', label: 'Etki değerlendirmesi' },
  { value: 'QualityAssessment', label: 'KG değerlendirmesi' },
  { value: 'ActionImplementation', label: 'Aksiyon uygulama' },
  { value: 'EffectivenessReview', label: 'Etkinlik' },
  { value: 'ClosureApproval', label: 'Kapanış onayı' },
  { value: 'Closed', label: 'Kapalı' },
]

const capaOptions: Array<SelectOption<'all' | 'true' | 'false'>> = [
  { value: 'all', label: 'Tümü' },
  { value: 'true', label: 'DÖF gerekli' },
  { value: 'false', label: 'DÖF gerekli değil' },
]

export function DeviationFilters({ value, onApply }: {
  value: DeviationFilterState
  onApply: (value: DeviationFilterState) => void
}) {
  const [draft, setDraft] = useState(value)

  const update = <K extends keyof DeviationFilterState>(field: K, next: DeviationFilterState[K]) =>
    setDraft((current) => ({ ...current, [field]: next }))

  return (
    <AdvancedFilterPanel
      activeCount={countDeviationFilters(draft)}
      onClear={() => { setDraft(emptyDeviationFilters); onApply(emptyDeviationFilters) }}
      onApply={() => onApply(draft)}
    >
        <TextField size="small" label="Kayıt no içerir" value={draft.recordNumber} onChange={(event) => update('recordNumber', event.target.value)} />
        <TextField size="small" label="Başlık içerir" value={draft.title} onChange={(event) => update('title', event.target.value)} />
        <TextField size="small" label="Bölüm içerir" value={draft.department} onChange={(event) => update('department', event.target.value)} />
        <SearchableMultiSelect label="Sınıflandırma" values={draft.classifications} options={classificationOptions} onChange={(next) => update('classifications', next)} />
        <SearchableMultiSelect label="Durum" values={draft.statuses} options={statusOptions} onChange={(next) => update('statuses', next)} />
        <SearchableSelect label="DÖF gerekliliği" value={draft.capaRequired} options={capaOptions} onChange={(next) => update('capaRequired', next ?? 'all')} />
        <TextField size="small" label="En düşük risk" type="number" value={draft.riskMin} onChange={(event) => update('riskMin', event.target.value)} />
        <TextField size="small" label="En yüksek risk" type="number" value={draft.riskMax} onChange={(event) => update('riskMax', event.target.value)} />
        <TextField size="small" label="Hedef tarih başlangıç" type="date" value={draft.targetFrom} onChange={(event) => update('targetFrom', event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        <TextField size="small" label="Hedef tarih bitiş" type="date" value={draft.targetTo} onChange={(event) => update('targetTo', event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
    </AdvancedFilterPanel>
  )
}

function countDeviationFilters(value: DeviationFilterState) {
  return [
    value.recordNumber, value.title, value.department, value.riskMin, value.riskMax,
    value.classifications.length ? 'classification' : '', value.statuses.length ? 'status' : '',
    value.capaRequired === 'all' ? '' : value.capaRequired, value.targetFrom, value.targetTo,
  ].filter(Boolean).length
}
