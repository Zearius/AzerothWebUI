import { useState } from 'react'
import { Link } from 'react-router'
import { armoryApi, type CharacterSummary } from '../armoryApi'
import { usePlayerAuth } from '../PlayerAuthContext'
import PublicHeader from '../PublicHeader'

function ArmoryCharacters() {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<CharacterSummary[]>([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { username } = usePlayerAuth()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return

    setLoading(true)
    setError('')
    try {
      const found = await armoryApi.searchCharacters(query.trim())
      setResults(found)
      setSearched(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <PublicHeader />
      <div className="armory-page">
        <h1>Armory</h1>
        <p>Look up a character by name.</p>

        <form className="armory-search" onSubmit={handleSubmit}>
          <input
            className="input"
            type="text"
            placeholder="Character name…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Searching…' : 'Search'}
          </button>
        </form>

        {error && <p className="form-message error">{error}</p>}

        <div className="armory-results">
          {results.map((character) => (
            <Link
              key={character.guid}
              to={`/armory/characters/${encodeURIComponent(character.name)}`}
              className={`armory-result-row ${username === character.name.toUpperCase() ? 'own-character' : ''}`}
            >
              <span>{character.name}</span>
              <span className="armory-result-meta">
                Level {character.level}
                {character.guildName ? ` · <${character.guildName}>` : ''}
                {character.online ? ' · Online' : ''}
              </span>
            </Link>
          ))}
          {searched && results.length === 0 && !error && <p>No characters found.</p>}
        </div>
      </div>
    </>
  )
}

export default ArmoryCharacters
