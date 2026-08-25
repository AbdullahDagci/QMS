import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

describe('App', () => {
  beforeEach(() => {
    window.history.pushState({}, '', '/')
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('API çevrimdışı')))
  })

  it('renders the dashboard and all modules in the sidebar', () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })

    render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>,
    )

    expect(screen.getByText('Kalite kontrol merkezi')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Dashboard/ })).toBeInTheDocument()
    expect(screen.getByText('Sapma Yönetimi')).toBeInTheDocument()
    expect(screen.getByText('Tedarikçi Değerlendirme')).toBeInTheDocument()
  })

  it('opens the deviation lifecycle details from the work list', async () => {
    const now = new Date().toISOString()
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString()
      if (url.endsWith('/api/v1/dashboard/summary')) {
        return Response.json({
          totalDeviations: 1, openDeviations: 1, majorOrCriticalDeviations: 1,
          overdueDeviations: 0, capaRequiredDeviations: 1, recentDeviations: [],
        })
      }
      if (url.endsWith('/api/v1/deviations/search')) {
        return Response.json({ items: [{
          id: '01991f70-6f40-7000-8000-000000000010', recordNumber: 'SP-2026-000001',
          title: 'Dolum sıcaklığı sapması', detectedDepartment: 'Üretim', riskScore: 27,
          classification: 'Major', capaRequired: true, status: 'Submitted',
          targetDateUtc: now, createdAtUtc: now, version: 2,
        }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 })
      }
      if (url.includes('/details')) {
        return Response.json({
          record: {
            id: '01991f70-6f40-7000-8000-000000000010', qualityRecordId: '01991f70-6f40-7000-8000-000000000011',
            recordNumber: 'SP-2026-000001', title: 'Dolum sıcaklığı sapması', description: 'Limit aşıldı.',
            expectedState: '25°C altında olmalı.', immediateAction: 'Hat durduruldu.', deviationType: 'Proses',
            detectedDepartment: 'Üretim', processStage: 'Dolum', occurredAtUtc: now, detectedAtUtc: now,
            targetDateUtc: now, likelihood: 3, severity: 3, detectability: 3, riskScore: 27,
            classification: 'Major', capaRequired: true, status: 'Submitted', preliminaryReviewNote: null,
            qualityAssessmentNote: null, effectivenessRequired: false, effectivenessAssessmentNote: null,
            closureJustification: null, closedAtUtc: null, createdAtUtc: now, updatedAtUtc: now, version: 2,
          },
          investigations: [], batchImpacts: [],
          auditTrail: [{ id: '1', version: 2, eventType: 'DeviationSubmitted', actor: 'KG', occurredAtUtc: now, reason: null }],
          availableTransitions: [{ code: 'start-preliminary-review', label: 'Ön incelemeyi başlat', noteRequired: true }],
        })
      }
      return new Response(null, { status: 404 })
    }))
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    render(<QueryClientProvider client={queryClient}><App /></QueryClientProvider>)

    await userEvent.click(screen.getAllByRole('link', { name: /Sapma Yönetimi/ })[0])
    await userEvent.click(await screen.findByRole('button', { name: 'Aç' }))

    expect(await screen.findByRole('button', { name: 'Sapma ayrıntısını kapat' })).toBeInTheDocument()
    expect(screen.getByRole('tablist', { name: 'Sapma detay bölümleri' })).toBeInTheDocument()
    expect(screen.getByText('Kontrollü iş akışı')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: /Karar ve Aksiyon/ }))
    expect(await screen.findByText('Sıradaki kontrollü adım')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: /Geçmiş/ }))
    expect(screen.getByLabelText('Kronolojik durum geçmişi')).toBeInTheDocument()
    expect(screen.getByText('Sapma değerlendirmeye gönderildi')).toBeInTheDocument()
  })

  it('shows table skeleton rows while the server-side list is loading', async () => {
    vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>(() => undefined)))
    window.history.pushState({}, '', '/modules/deviations')
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })

    render(<QueryClientProvider client={queryClient}><App /></QueryClientProvider>)

    expect(await screen.findByLabelText('Liste yükleniyor')).toBeInTheDocument()
    expect(screen.getByLabelText('Uygulama verileri yükleniyor')).toBeInTheDocument()
  })
})
