import { useEffect, useState } from 'react'
import { adminApi, type AdminAccount } from '../adminApi'

function AdminAccounts() {
  const [accounts, setAccounts] = useState<AdminAccount[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [actionMessage, setActionMessage] = useState('')
  const [busyUsername, setBusyUsername] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    setError('')
    adminApi
      .accounts()
      .then(setAccounts)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const runAction = async (username: string, action: 'ban' | 'unban' | 'kick') => {
    setBusyUsername(username)
    setActionMessage('')
    try {
      const result = await adminApi[action](username)
      setActionMessage(result.result)
      load()
    } catch (err) {
      setActionMessage(err instanceof Error ? err.message : 'Action failed.')
    } finally {
      setBusyUsername(null)
    }
  }

  return (
    <section>
      <div className="page-header">
        <h2>Accounts</h2>
        <button type="button" className="btn btn-secondary" onClick={load} disabled={loading}>
          {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      {error && <p className="form-message error">{error}</p>}
      {actionMessage && <p className="form-message">{actionMessage}</p>}

      <div className="table-wrap">
        <table className="accounts-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Email</th>
              <th>GM Level</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((account) => (
              <tr key={account.id}>
                <td>{account.username}</td>
                <td>{account.email}</td>
                <td>{account.gmLevel}</td>
                <td>
                  {account.banned ? 'Banned' : account.online ? 'Online' : 'Offline'}
                </td>
                <td className="account-actions">
                  <button
                    type="button"
                    className={`btn btn-sm ${account.banned ? 'btn-secondary' : 'btn-destructive'}`}
                    disabled={busyUsername === account.username}
                    onClick={() => runAction(account.username, account.banned ? 'unban' : 'ban')}
                  >
                    {account.banned ? 'Unban' : 'Ban'}
                  </button>
                  <button
                    type="button"
                    className="btn btn-sm btn-secondary"
                    disabled={busyUsername === account.username || !account.online}
                    onClick={() => runAction(account.username, 'kick')}
                  >
                    Kick
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export default AdminAccounts
