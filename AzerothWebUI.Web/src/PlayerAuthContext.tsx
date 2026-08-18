import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { playerApi } from './playerApi'

type AuthState = {
  username: string | null
  loading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const PlayerAuthContext = createContext<AuthState | null>(null)

export function PlayerAuthProvider({ children }: { children: ReactNode }) {
  const [username, setUsername] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    playerApi
      .me()
      .then((me) => setUsername(me.username))
      .catch(() => setUsername(null))
      .finally(() => setLoading(false))
  }, [])

  const login = async (user: string, password: string) => {
    const result = await playerApi.login(user, password)
    setUsername(result.username)
  }

  const logout = async () => {
    await playerApi.logout()
    setUsername(null)
  }

  return (
    <PlayerAuthContext.Provider value={{ username, loading, login, logout }}>
      {children}
    </PlayerAuthContext.Provider>
  )
}

export function usePlayerAuth() {
  const context = useContext(PlayerAuthContext)
  if (!context) {
    throw new Error('usePlayerAuth must be used within a PlayerAuthProvider')
  }
  return context
}
