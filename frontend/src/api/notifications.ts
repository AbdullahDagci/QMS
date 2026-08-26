import { qmsFetch } from './http'

export interface UserNotification {
  id: string
  moduleCode: string
  title: string
  message: string
  link: string | null
  createdAtUtc: string
  readAtUtc: string | null
}

export interface NotificationList { unreadCount: number; items: UserNotification[] }

async function json<T>(response: Response): Promise<T> {
  if (!response.ok) throw new Error(`Bildirimler alınamadı (${response.status})`)
  return response.json() as Promise<T>
}

export const getNotifications = (signal?: AbortSignal) => qmsFetch('/api/v1/notifications', { signal }).then(json<NotificationList>)
export const markNotificationRead = async (id: string) => {
  const response = await qmsFetch(`/api/v1/notifications/${id}/read`, { method: 'POST' })
  if (!response.ok) throw new Error(`Bildirim güncellenemedi (${response.status})`)
}
export const markAllNotificationsRead = async () => {
  const response = await qmsFetch('/api/v1/notifications/read-all', { method: 'POST' })
  if (!response.ok) throw new Error(`Bildirimler güncellenemedi (${response.status})`)
}
