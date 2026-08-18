export type AdminAccount = {
  id: number
  username: string
  email: string
  gmLevel: number
  banned: boolean
  online: boolean
}

export type ServerStatus = {
  rawOutput: string
}

export type ConfigDefaultOption = {
  value: string
  label: string | null
}

export type ConfigEntry = {
  key: string
  currentValue: string
  section: string
  description: string
  defaults: ConfigDefaultOption[]
  isToggle: boolean
  sourceFile: string
  requiresRestart: boolean
}

export type ConfigFile = {
  id: string
  displayName: string
  alwaysRestartRequired: boolean
}

export type UpdateConfigResult = {
  entry: ConfigEntry
  requiresRestart: boolean
  reloadResult: string | null
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(text || `Request failed (${response.status})`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export const adminApi = {
  login: (username: string, password: string) =>
    request<{ username: string }>('/api/admin/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  logout: () => request<void>('/api/admin/logout', { method: 'POST' }),
  me: () => request<{ username: string }>('/api/admin/me'),
  status: () => request<ServerStatus>('/api/admin/status'),
  accounts: () => request<AdminAccount[]>('/api/admin/accounts'),
  ban: (username: string) =>
    request<{ result: string }>(`/api/admin/accounts/${encodeURIComponent(username)}/ban`, { method: 'POST' }),
  unban: (username: string) =>
    request<{ result: string }>(`/api/admin/accounts/${encodeURIComponent(username)}/unban`, { method: 'POST' }),
  kick: (username: string) =>
    request<{ result: string }>(`/api/admin/accounts/${encodeURIComponent(username)}/kick`, { method: 'POST' }),
  configFiles: () => request<ConfigFile[]>('/api/admin/config/files'),
  config: (file: string) => request<ConfigEntry[]>(`/api/admin/config/${encodeURIComponent(file)}`),
  updateConfig: (file: string, key: string, value: string) =>
    request<UpdateConfigResult>(`/api/admin/config/${encodeURIComponent(file)}/${encodeURIComponent(key)}`, {
      method: 'PATCH',
      body: JSON.stringify({ value }),
    }),
}
