import { Navigate, NavLink, Outlet } from 'react-router'
import { useAdminAuth } from '../AdminAuthContext'
import ThemeToggle from '../ThemeToggle'

function AdminLayout() {
  const { username, loading, logout } = useAdminAuth()

  if (loading) {
    return (
      <section id="center">
        <p>Loading…</p>
      </section>
    )
  }

  if (!username) {
    return <Navigate to="/admin/login" replace />
  }

  return (
    <div className="admin-shell">
      <header className="admin-header">
        <nav className="admin-nav">
          <NavLink to="/admin/status">Status</NavLink>
          <NavLink to="/admin/accounts">Accounts</NavLink>
          <NavLink to="/admin/config">Config</NavLink>
          <NavLink to="/admin/ahbot">AH Bot</NavLink>
          <NavLink to="/admin/award-item">Award Item</NavLink>
        </nav>
        <div className="admin-user">
          <ThemeToggle />
          <span>{username}</span>
          <button type="button" className="btn btn-secondary" onClick={() => logout()}>
            Sign out
          </button>
        </div>
      </header>
      <main className="admin-content">
        <Outlet />
      </main>
    </div>
  )
}

export default AdminLayout
