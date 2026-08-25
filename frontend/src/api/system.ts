export interface ModuleSummary {
  code: string
  name: string
  status: 'active' | 'foundation' | 'planned' | string
}

export interface SystemInfo {
  name: string
  version: string
  singleTenant: boolean
  modules: ModuleSummary[]
}

export async function getSystemInfo(signal?: AbortSignal): Promise<SystemInfo> {
  const response = await qmsFetch('/api/v1/system/info', {
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Sistem bilgisi alınamadı (${response.status}).`)
  }

  return response.json() as Promise<SystemInfo>
}
import { qmsFetch } from './http'
