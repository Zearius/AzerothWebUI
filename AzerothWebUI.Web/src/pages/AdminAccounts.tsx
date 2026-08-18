import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { adminApi, type AdminAccount } from '../adminApi'
import type { CharacterSummary } from '../armoryApi'

function AdminAccounts() {
  const [accounts, setAccounts] = useState<AdminAccount[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [actionMessage, setActionMessage] = useState('')
  const [busyUsername, setBusyUsername] = useState<string | null>(null)
  const [expandedUsername, setExpandedUsername] = useState<string | null>(null)
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [charactersLoading, setCharactersLoading] = useState(false)
  const [charactersError, setCharactersError] = useState('')

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

  const toggleCharacters = async (username: string) => {
    if (expandedUsername === username) {
      setExpandedUsername(null)
      return
    }

    setExpandedUsername(username)
    setCharacters([])
    setCharactersError('')
    setCharactersLoading(true)
    try {
      setCharacters(await adminApi.accountCharacters(username))
    } catch (err) {
      setCharactersError(err instanceof Error ? err.message : 'Failed to load characters.')
    } finally {
      setCharactersLoading(false)
    }
  }

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
              <>
                <tr key={account.id}>
                  <td>
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => toggleCharacters(account.username)}
                    >
                      {account.username}
                    </button>
                  </td>
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
                {expandedUsername === account.username && (
                  <tr key={`${account.id}-characters`}>
                    <td colSpan={5} className="account-characters-row">
                      {charactersLoading && <p>Loading characters…</p>}
                      {charactersError && <p className="form-message error">{charactersError}</p>}
                      {!charactersLoading && !charactersError && characters.length === 0 && (
                        <p>No characters on this account.</p>
                      )}
                      {!charactersLoading && characters.length > 0 && (
                        <ul className="account-characters-list">
                          {characters.map((character) => (
                            <li key={character.guid}>
                              <Link to={`/armory/characters/${encodeURIComponent(character.name)}`}>
                                {character.name}
                              </Link>
                              <span className="armory-result-meta">
                                {' '}
                                Level {character.level}
                                {character.guildName ? ` · <${character.guildName}>` : ''}
                                {character.online ? ' · Online' : ''}
                              </span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </td>
                  </tr>
                )}
              </>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export default AdminAccounts
