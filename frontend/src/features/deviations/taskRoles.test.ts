import { describe, expect, it } from 'vitest'
import { taskRoleLabel, taskRoleOptions } from './taskRoles'

describe('taskRoleLabel', () => {
  it('matris dışındaki hazırlayan görevini Türkçe gösterir', () => {
    expect(taskRoleLabel('Initiator')).toBe('Hazırlayan')
    expect(taskRoleOptions.map((option) => option.value)).not.toContain('Initiator')
  })

  it('matris rollerini ve bilinmeyen rolleri etiketler', () => {
    expect(taskRoleLabel('ProcessAuthority')).toBe('İşlem yetkilisi')
    expect(taskRoleLabel('Custom')).toBe('Custom')
  })
})
