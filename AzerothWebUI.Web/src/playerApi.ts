import { request } from './apiClient'

export const playerApi = {
  login: (username: string, password: string) =>
    request<{ username: string }>('/api/player/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  logout: () => request<void>('/api/player/logout', { method: 'POST' }),
  me: () => request<{ username: string }>('/api/player/me'),
}
