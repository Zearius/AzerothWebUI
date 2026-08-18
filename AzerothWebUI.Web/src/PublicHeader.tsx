import { NavLink } from 'react-router'
import ThemeToggle from './ThemeToggle'
import { useAdminAuth } from './AdminAuthContext'
import { usePlayerAuth } from './PlayerAuthContext'
import ServerBanner from './ServerBanner'

function PublicHeader() {
  const { username: adminUsername } = useAdminAuth()
  const { username: playerUsername, logout: playerLogout } = usePlayerAuth()

  return (
    <>
      <header className="public-header">
        <nav className="public-nav">
          <NavLink to="/armory/characters">Armory</NavLink>
          <NavLink to="/armory/items">Items</NavLink>
          {adminUsername && <NavLink to="/admin">Admin</NavLink>}
          {playerUsername ? (
            <>
              <span className="public-nav-user">{playerUsername}</span>
              <button type="button" className="link-button" onClick={() => playerLogout()}>
                Sign out
              </button>
            </>
          ) : (
            <NavLink to="/login">Log In</NavLink>
          )}
          <NavLink to="/">Register</NavLink>
        </nav>
        <ThemeToggle />
      </header>
      <ServerBanner />
    </>
  )
}

export default PublicHeader
