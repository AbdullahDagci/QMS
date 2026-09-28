import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { NextAssigneeNotice } from './NextAssigneeNotice'

describe('NextAssigneeNotice', () => {
  it('atama matrisinden gelen kullanıcıyı ve rolü gösterir', () => {
    render(
      <NextAssigneeNotice
        when="Gönderildiğinde"
        nextAssignee={{ taskRole: 'ProcessAuthority', userId: 'u1', userName: 'Mert Kaya', departmentName: 'Kalite Güvence' }}
      />,
    )

    expect(screen.getByText('Gönderildiğinde görev Mert Kaya kullanıcısına atanacak')).toBeInTheDocument()
    expect(screen.getByText(/Kalite Güvence · İşlem yetkilisi/)).toBeInTheDocument()
  })

  it('eşleşen kural yoksa iş akışının duracağını uyarır', () => {
    render(
      <NextAssigneeNotice
        when="Gönderildiğinde"
        nextAssignee={{ taskRole: 'Investigator', userId: null, userName: null, departmentName: null }}
      />,
    )

    expect(screen.getByText(/araştırmacı görevi için eşleşen atama kuralı yok/)).toBeInTheDocument()
  })
})
