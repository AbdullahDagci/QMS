import { describe,expect,it } from 'vitest'
import { emptyDocumentFilters,toDocumentColumnFilters } from './documentFilterModel'
describe('document filters',()=>{it('builds typed server filters',()=>{const result=toDocumentColumnFilters({...emptyDocumentFilters,documentCode:' SOP ',status:'Effective',effectiveFrom:'2026-08-01'});expect(result).toEqual([{field:'documentCode',operator:'contains',value:'SOP'},{field:'status',operator:'equals',value:'Effective'},{field:'plannedEffectiveDateUtc',operator:'between',value:'2026-08-01',valueTo:'2999-12-31'}])})})
