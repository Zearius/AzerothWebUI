import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { adminApi } from './adminApi'

type AuthState = {
  username: string | null
  loading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AdminAuthContext = createContext<AuthState | null>(null)

export function AdminAuthProvider({ children }: { children: ReactNode }) {
  const [username, setUsername] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    adminApi
      .me()
      .then((me) => setUsername(me.username))
      .catch(() => setUsername(null))
      .finally(() => setLoading(false))
  }, [])

  const login = async (user: string, password: string) => {
    const result = await adminApi.login(user, password)
    setUsername(result.username)
  }

  const logout = async () => {
    await adminApi.logout()
    setUsername(null)
  }

  return (
    <AdminAuthContext.Provider value={{ username, loading, login, logout }}>
      {children}
    </AdminAuthContext.Provider>
  )
}

export function useAdminAuth() {
  const context = useContext(AdminAuthContext)
  if (!context) {
    throw new Error('useAdminAuth must be used within an AdminAuthProvider')
  }
  return context
}
