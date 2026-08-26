export const QMS_PROFILE_KEY = 'qms-development-profile'
export const QMS_SESSION_KEY = 'qms-session-token'

export function currentProfileKey() {
  if (typeof window === 'undefined') return 'quality'
  return window.localStorage.getItem(QMS_PROFILE_KEY) ?? 'quality'
}

export function qmsFetch(input: RequestInfo | URL, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  headers.set('X-QMS-Profile', currentProfileKey())
  const token = typeof window === 'undefined' ? null : window.localStorage.getItem(QMS_SESSION_KEY)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  return fetch(input, { ...init, headers })
}
