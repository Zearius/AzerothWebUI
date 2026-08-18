import { useState } from 'react'
import { useNavigate } from 'react-router'
import { useAdminAuth } from '../AdminAuthContext'
import PublicHeader from '../PublicHeader'

function AdminLogin() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const { login } = useAdminAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError('')

    try {
      await login(username, password)
      navigate('/admin')
    } catch {
      setError('Invalid username or password.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <PublicHeader />
      <section id="center">
        <div>
          <h1>Admin Login</h1>
          <p>Sign in to manage the server.</p>
        </div>

        <form className="stack-form" onSubmit={handleSubmit}>
          <label>
            Username
            <input
              className="input"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
          </label>

          <label>
            Password
            <input
              className="input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>

          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Signing in…' : 'Sign in'}
          </button>

          {error && <p className="form-message error">{error}</p>}
        </form>
      </section>
    </>
  )
}

export default AdminLogin
