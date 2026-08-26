import { Autocomplete, TextField } from '@mui/material'

export interface SelectOption<T extends string | number = string> {
  value: T
  label: string
}

export function SearchableSelect<T extends string | number>({
  label,
  value,
  options,
  onChange,
  size = 'small',
  disabled = false,
  required = false,
}: {
  label: string
  value: T | null
  options: Array<SelectOption<T>>
  onChange: (value: T | null) => void
  size?: 'small' | 'medium'
  disabled?: boolean
  required?: boolean
}) {
  const selected = options.find((option) => option.value === value) ?? null

  return (
    <Autocomplete
      options={options}
      value={selected}
      disabled={disabled}
      autoHighlight
      openOnFocus
      isOptionEqualToValue={(option, selectedOption) => option.value === selectedOption.value}
      getOptionLabel={(option) => option.label}
      onChange={(_, option) => onChange(option?.value ?? null)}
      renderInput={(params) => <TextField {...params} label={label} size={size} required={required} />}
    />
  )
}

export function SearchableMultiSelect<T extends string | number>({
  label,
  values,
  options,
  onChange,
  required = false,
}: {
  label: string
  values: T[]
  options: Array<SelectOption<T>>
  onChange: (values: T[]) => void
  required?: boolean
}) {
  const selected = options.filter((option) => values.includes(option.value))

  return (
    <Autocomplete
      multiple
      options={options}
      value={selected}
      autoHighlight
      openOnFocus
      limitTags={2}
      isOptionEqualToValue={(option, selectedOption) => option.value === selectedOption.value}
      getOptionLabel={(option) => option.label}
      onChange={(_, next) => onChange(next.map((option) => option.value))}
      renderInput={(params) => <TextField {...params} label={label} size="small" required={required} />}
    />
  )
}
