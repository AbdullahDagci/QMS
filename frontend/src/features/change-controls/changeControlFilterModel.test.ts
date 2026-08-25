import { describe, expect, it } from 'vitest'
import { emptyChangeControlFilters, toChangeControlColumnFilters } from './changeControlFilterModel'

describe('change control filters', () => {
  it('creates typed server filters', () => { const result = toChangeControlColumnFilters({ ...emptyChangeControlFilters, title: 'PLC', status: 'Implementation', targetFrom: '2026-08-01', targetTo: '2026-08-31' }); expect(result).toEqual([{ field: 'title', operator: 'contains', value: 'PLC' }, { field: 'status', operator: 'equals', value: 'Implementation' }, { field: 'targetDateUtc', operator: 'between', value: '2026-08-01', valueTo: '2026-08-31' }]) })
})
