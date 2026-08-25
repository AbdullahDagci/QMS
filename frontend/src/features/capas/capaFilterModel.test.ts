import { describe, expect, it } from 'vitest'
import { emptyCapaFilters, toCapaColumnFilters } from './capaFilterModel'

describe('toCapaColumnFilters', () => {
  it('does not send filters while the applied model is empty', () => {
    expect(toCapaColumnFilters(emptyCapaFilters)).toEqual([])
  })

  it('maps text, enum and date range filters to server-side column filters', () => {
    expect(toCapaColumnFilters({
      ...emptyCapaFilters,
      recordNumber: '000001',
      owner: 'Kalite',
      status: 'Closed',
      targetFrom: '2026-08-01',
      targetTo: '2026-08-31',
    })).toEqual([
      { field: 'recordNumber', operator: 'contains', value: '000001' },
      { field: 'owner', operator: 'contains', value: 'Kalite' },
      { field: 'status', operator: 'equals', value: 'Closed' },
      { field: 'targetDateUtc', operator: 'between', value: '2026-08-01', valueTo: '2026-08-31' },
    ])
  })
})
