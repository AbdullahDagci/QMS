export async function qmsFetch(input: RequestInfo | URL, init: RequestInit = {}) {
  const headers = new Headers(init.headers)
  headers.set('X-QMS-CSRF', '1')
  const response = await fetch(input, { ...init, headers, credentials: 'same-origin' })
  if (response.status === 401 && typeof window !== 'undefined')
    window.dispatchEvent(new Event('qms:unauthorized'))
  return response
}
