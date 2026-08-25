export interface DashboardDeviation {
  id: string
  recordNumber: string
  title: string
  status: string
  classification: 'Minor' | 'Major' | 'Critical'
  riskScore: number
  targetDateUtc: string
}

export interface DashboardSummary {
  totalDeviations: number
  openDeviations: number
  majorOrCriticalDeviations: number
  overdueDeviations: number
  capaRequiredDeviations: number
  recentDeviations: DashboardDeviation[]
}

export async function getDashboardSummary(signal?: AbortSignal): Promise<DashboardSummary> {
  const response = await qmsFetch('/api/v1/dashboard/summary', { signal })
  if (!response.ok) throw new Error(`Dashboard yüklenemedi (${response.status})`)
  return response.json() as Promise<DashboardSummary>
}
import { qmsFetch } from './http'
