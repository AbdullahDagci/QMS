export const QMS_PROFILE_KEY = 'qms-development-profile'

export function currentProfileKey() {
  if (typeof window === 'undefined') return 'quality'
  return window.localStorage.getItem(QMS_PROFILE_KEY) ?? 'quality'
}

export function qmsFetch(input: RequestInfo | URL, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  headers.set('X-QMS-Profile', currentProfileKey())
  return fetch(input, { ...init, headers })
}
