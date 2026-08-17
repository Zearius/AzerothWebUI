import { useEffect, useState } from 'react'
import { adminApi } from '../adminApi'

function AdminStatus() {
  const [output, setOutput] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    setError('')
    adminApi
      .status()
      .then((status) => setOutput(status.rawOutput))
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  return (
    <section>
      <div className="page-header">
        <h2>Server Status</h2>
        <button type="button" className="counter" onClick={load} disabled={loading}>
          {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      {error && <p className="form-message error">{error}</p>}
      {!error && <pre className="status-output">{output || (loading ? 'Loading…' : 'No data.')}</pre>}
    </section>
  )
}

export default AdminStatus
