import { useState } from 'react'
import { Link } from 'react-router'
import PublicHeader from '../PublicHeader'

type Status = 'idle' | 'submitting' | 'success' | 'error'

function Register() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [email, setEmail] = useState('')
  const [status, setStatus] = useState<Status>('idle')
  const [message, setMessage] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (password !== confirmPassword) {
      setStatus('error')
      setMessage('Passwords do not match.')
      return
    }

    setStatus('submitting')
    setMessage('')

    try {
      const response = await fetch('/api/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password, email }),
      })

      if (response.ok) {
        setStatus('success')
        setMessage(`Account "${username}" created. You can now log in with the game client.`)
        setUsername('')
        setPassword('')
        setConfirmPassword('')
        setEmail('')
        return
      }

      const errorText = await response.text()
      setStatus('error')
      setMessage(errorText || `Registration failed (${response.status}).`)
    } catch {
      setStatus('error')
      setMessage('Could not reach the server. Please try again.')
    }
  }

  return (
    <>
      <PublicHeader />
      <section id="center">
        <div className="center-heading">
          <h1>Create Account</h1>
          <p>Register a new account for the server.</p>
        </div>

        <form className="stack-form" onSubmit={handleSubmit}>
          <label>
            Username
            <input
              className="input"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              maxLength={16}
              required
            />
          </label>

          <label>
            Email
            <input
              className="input"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
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
              maxLength={16}
              required
            />
          </label>

          <label>
            Confirm password
            <input
              className="input"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              maxLength={16}
              required
            />
          </label>

          <button type="submit" className="btn btn-primary" disabled={status === 'submitting'}>
            {status === 'submitting' ? 'Creating…' : 'Create Account'}
          </button>

          {message && (
            <p className={status === 'success' ? 'form-message success' : 'form-message error'}>
              {message}
            </p>
          )}

          <p className="auth-switch">
            Already have an account? <Link to="/login">Log in</Link>
          </p>
        </form>
      </section>
    </>
  )
}

export default Register
