import { qmsFetch } from './http'
import { QMS_SESSION_KEY } from './http'

export interface UserProfileOption { key: string; displayName: string; roles: string[] }
export interface CurrentUser {
  id: string
  displayName: string
  profile: string
  roles: string[]
  permissions: string[]
  availableProfiles: UserProfileOption[]
}

export interface LoginResult { token: string; expiresAtUtc: string; profileKey: string; displayName: string }
async function loginJson(response: Response) { if (!response.ok) throw new Error('E-posta veya parola hatalı.'); const result = await response.json() as LoginResult; window.localStorage.setItem(QMS_SESSION_KEY, result.token); return result }
export const login = (email: string, password: string) => qmsFetch('/api/v1/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }) }).then(loginJson)
export const quickLogin = (profileKey: string) => qmsFetch('/api/v1/auth/quick-login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ profileKey }) }).then(loginJson)
export const getQuickProfiles = async (): Promise<UserProfileOption[]> => { const response = await fetch('/api/v1/auth/quick-profiles'); if (!response.ok) return []; return response.json() as Promise<UserProfileOption[]> }
export const logout = async () => { await qmsFetch('/api/v1/auth/logout', { method: 'POST' }).catch(() => undefined); window.localStorage.removeItem(QMS_SESSION_KEY) }

export async function getCurrentUser(signal?: AbortSignal): Promise<CurrentUser> {
  const response = await qmsFetch('/api/v1/auth/me', { signal })
  if (!response.ok) throw new Error(`Kullanıcı bilgisi alınamadı (${response.status})`)
  return response.json() as Promise<CurrentUser>
}
