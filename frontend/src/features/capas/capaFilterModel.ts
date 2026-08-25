import type { ColumnFilter } from '../../api/deviations'

export interface CapaFilterState {
  recordNumber: string
  sourceRecordNumber: string
  title: string
  owner: string
  status: string
  targetFrom: string
  targetTo: string
}

export const emptyCapaFilters: CapaFilterState = {
  recordNumber: '',
  sourceRecordNumber: '',
  title: '',
  owner: '',
  status: '',
  targetFrom: '',
  targetTo: '',
}

export function toCapaColumnFilters(filters: CapaFilterState): ColumnFilter[] {
  const result: ColumnFilter[] = []
  if (filters.recordNumber.trim()) result.push({ field: 'recordNumber', operator: 'contains', value: filters.recordNumber.trim() })
  if (filters.sourceRecordNumber.trim()) result.push({ field: 'sourceRecordNumber', operator: 'contains', value: filters.sourceRecordNumber.trim() })
  if (filters.title.trim()) result.push({ field: 'title', operator: 'contains', value: filters.title.trim() })
  if (filters.owner.trim()) result.push({ field: 'owner', operator: 'contains', value: filters.owner.trim() })
  if (filters.status) result.push({ field: 'status', operator: 'equals', value: filters.status })
  if (filters.targetFrom && filters.targetTo) result.push({ field: 'targetDateUtc', operator: 'between', value: filters.targetFrom, valueTo: filters.targetTo })
  else if (filters.targetFrom) result.push({ field: 'targetDateUtc', operator: 'after', value: filters.targetFrom })
  else if (filters.targetTo) result.push({ field: 'targetDateUtc', operator: 'before', value: filters.targetTo })
  return result
}
