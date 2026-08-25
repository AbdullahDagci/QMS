import { qmsFetch } from './http'

export interface UserProfileOption { key: string; displayName: string; roles: string[] }
export interface CurrentUser {
  id: string
  displayName: string
  profile: string
  roles: string[]
  permissions: string[]
  availableProfiles: UserProfileOption[]
}

export async function getCurrentUser(signal?: AbortSignal): Promise<CurrentUser> {
  const response = await qmsFetch('/api/v1/auth/me', { signal })
  if (!response.ok) throw new Error(`Kullanıcı bilgisi alınamadı (${response.status})`)
  return response.json() as Promise<CurrentUser>
}
