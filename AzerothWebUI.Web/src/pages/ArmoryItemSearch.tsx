import { useState } from 'react'
import { Link } from 'react-router'
import { armoryApi, type ItemSearchResult } from '../armoryApi'
import { qualityClass } from '../itemQuality'
import PublicHeader from '../PublicHeader'

function ArmoryItemSearch() {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<ItemSearchResult[]>([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return

    setLoading(true)
    setError('')
    try {
      const found = await armoryApi.searchItems(query.trim())
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
        <h1>Item Lookup</h1>
        <p>Search for an item to see its stats and where it drops.</p>

        <form className="armory-search" onSubmit={handleSubmit}>
          <input
            className="input"
            type="text"
            placeholder="Item name…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Searching…' : 'Search'}
          </button>
        </form>

        {error && <p className="form-message error">{error}</p>}

        <div className="armory-results">
          {results.map((item) => (
            <Link key={item.entry} to={`/armory/items/${item.entry}`} className="armory-result-row">
              <span className={qualityClass(item.quality)}>{item.name}</span>
              <span className="armory-result-meta">#{item.entry}</span>
            </Link>
          ))}
          {searched && results.length === 0 && !error && <p>No items found.</p>}
        </div>
      </div>
    </>
  )
}

export default ArmoryItemSearch
