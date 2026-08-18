import { useEffect, useState } from 'react'
import { ahBotApi, type AhBotHouse, type AhBotQualitySettings, type AhBotDisabledItem } from '../ahBotApi'
import { armoryApi, type ItemSearchResult } from '../armoryApi'

const QUALITY_FIELDS: { key: keyof AhBotHouse; label: string }[] = [
  { key: 'grey', label: 'Grey' },
  { key: 'white', label: 'White' },
  { key: 'green', label: 'Green' },
  { key: 'blue', label: 'Blue' },
  { key: 'purple', label: 'Purple' },
  { key: 'orange', label: 'Orange' },
  { key: 'yellow', label: 'Yellow' },
]

function QualityRow({
  label,
  settings,
  onChange,
}: {
  label: string
  settings: AhBotQualitySettings
  onChange: (settings: AhBotQualitySettings) => void
}) {
  const field = (key: keyof AhBotQualitySettings) => (
    <input
      className="input"
      type="number"
      value={settings[key]}
      onChange={(e) => onChange({ ...settings, [key]: Number(e.target.value) })}
    />
  )

  return (
    <tr>
      <td>{label}</td>
      <td>{field('percentTradeGoods')}</td>
      <td>{field('percentItems')}</td>
      <td>{field('minPrice')}</td>
      <td>{field('maxPrice')}</td>
      <td>{field('minBidPrice')}</td>
      <td>{field('maxBidPrice')}</td>
      <td>{field('maxStack')}</td>
      <td>{field('buyerPrice')}</td>
    </tr>
  )
}

function HouseEditor({ house, onSaved }: { house: AhBotHouse; onSaved: (message: string) => void }) {
  const [draft, setDraft] = useState(house)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => setDraft(house), [house])

  const save = async () => {
    setSaving(true)
    setError('')
    try {
      await ahBotApi.updateHouse(draft.auctionHouse, draft)
      onSaved(`${draft.name} settings saved.`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="panel" style={{ padding: '16px', marginBottom: '16px' }}>
      <div className="page-header">
        <h2>{draft.name}</h2>
        <button type="button" className="btn btn-primary btn-sm" onClick={save} disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </button>
      </div>

      <div className="config-text-input" style={{ marginBottom: '12px', gap: '16px' }}>
        <label>
          Min items{' '}
          <input
            className="input"
            type="number"
            value={draft.minItems}
            onChange={(e) => setDraft({ ...draft, minItems: Number(e.target.value) })}
          />
        </label>
        <label>
          Max items{' '}
          <input
            className="input"
            type="number"
            value={draft.maxItems}
            onChange={(e) => setDraft({ ...draft, maxItems: Number(e.target.value) })}
          />
        </label>
        <label>
          Bidding interval (min){' '}
          <input
            className="input"
            type="number"
            value={draft.buyerBiddingInterval}
            onChange={(e) => setDraft({ ...draft, buyerBiddingInterval: Number(e.target.value) })}
          />
        </label>
        <label>
          Bids per interval{' '}
          <input
            className="input"
            type="number"
            value={draft.buyerBidsPerInterval}
            onChange={(e) => setDraft({ ...draft, buyerBidsPerInterval: Number(e.target.value) })}
          />
        </label>
      </div>

      {error && <p className="form-message error">{error}</p>}

      <div className="table-wrap">
        <table className="accounts-table">
          <thead>
            <tr>
              <th>Quality</th>
              <th>% Trade Goods</th>
              <th>% Items</th>
              <th>Min Price</th>
              <th>Max Price</th>
              <th>Min Bid</th>
              <th>Max Bid</th>
              <th>Max Stack</th>
              <th>Buyer Price</th>
            </tr>
          </thead>
          <tbody>
            {QUALITY_FIELDS.map(({ key, label }) => (
              <QualityRow
                key={key}
                label={label}
                settings={draft[key] as AhBotQualitySettings}
                onChange={(settings) => setDraft({ ...draft, [key]: settings })}
              />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function DisabledItemsEditor() {
  const [items, setItems] = useState<AhBotDisabledItem[]>([])
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<ItemSearchResult[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    ahBotApi
      .disabledItems()
      .then(setItems)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const search = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!query.trim()) return
    try {
      setResults(await armoryApi.searchItems(query.trim()))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed.')
    }
  }

  const add = async (itemId: number) => {
    await ahBotApi.addDisabledItem(itemId)
    setResults([])
    setQuery('')
    load()
  }

  const remove = async (itemId: number) => {
    await ahBotApi.removeDisabledItem(itemId)
    load()
  }

  return (
    <div className="panel" style={{ padding: '16px' }}>
      <h2>Disabled Items</h2>
      <p className="config-hint">Items in this list will never be stocked by the AH bot.</p>

      <form className="armory-search" onSubmit={search}>
        <input
          className="input"
          type="text"
          placeholder="Search for an item to disable…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <button type="submit" className="btn btn-secondary">
          Search
        </button>
      </form>

      {results.length > 0 && (
        <div className="armory-results" style={{ marginBottom: '16px' }}>
          {results.map((item) => (
            <div key={item.entry} className="armory-result-row">
              <span>{item.name}</span>
              <button type="button" className="btn btn-sm btn-primary" onClick={() => add(item.entry)}>
                Disable
              </button>
            </div>
          ))}
        </div>
      )}

      {error && <p className="form-message error">{error}</p>}
      {loading ? (
        <p>Loading…</p>
      ) : (
        <div className="table-wrap">
          <table className="accounts-table">
            <thead>
              <tr>
                <th>Item</th>
                <th>ID</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.itemId}>
                  <td>{item.itemName ?? 'Unknown item'}</td>
                  <td>{item.itemId}</td>
                  <td>
                    <button type="button" className="btn btn-sm btn-secondary" onClick={() => remove(item.itemId)}>
                      Enable
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function AdminAhBot() {
  const [houses, setHouses] = useState<AhBotHouse[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState('')

  const load = () => {
    setLoading(true)
    setError('')
    ahBotApi
      .houses()
      .then(setHouses)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  return (
    <section>
      <div className="page-header">
        <h2>AH Bot</h2>
        <button type="button" className="btn btn-secondary" onClick={load} disabled={loading}>
          {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      {error && <p className="form-message error">{error}</p>}
      {message && <p className="form-message success">{message}</p>}

      {houses.map((house) => (
        <HouseEditor key={house.auctionHouse} house={house} onSaved={setMessage} />
      ))}

      <DisabledItemsEditor />
    </section>
  )
}

export default AdminAhBot
