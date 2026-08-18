import { request } from './apiClient'

export type PublicServerStatus = {
  playersOnline: number
  charactersInWorld: number
  uptime: string | null
}

export const publicApi = {
  status: () => request<PublicServerStatus>('/api/status'),
  motd: () => request<{ content: string }>('/api/motd'),
}
