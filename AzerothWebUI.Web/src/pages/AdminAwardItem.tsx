import { useState } from 'react'
import { armoryApi, type ItemSearchResult } from '../armoryApi'
import { qualityClass } from '../itemQuality'
import { request } from '../apiClient'

function AdminAwardItem() {
  const [itemQuery, setItemQuery] = useState('')
  const [itemResults, setItemResults] = useState<ItemSearchResult[]>([])
  const [selectedItem, setSelectedItem] = useState<ItemSearchResult | null>(null)
  const [characterName, setCharacterName] = useState('')
  const [count, setCount] = useState(1)
  const [subject, setSubject] = useState('')
  const [message, setMessage] = useState('')
  const [status, setStatus] = useState<{ text: string; kind: 'success' | 'error' } | null>(null)
  const [searching, setSearching] = useState(false)
  const [sending, setSending] = useState(false)

  const searchItems = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!itemQuery.trim()) return
    setSearching(true)
    try {
      setItemResults(await armoryApi.searchItems(itemQuery.trim()))
    } catch (err) {
      setStatus({ text: err instanceof Error ? err.message : 'Search failed.', kind: 'error' })
    } finally {
      setSearching(false)
    }
  }

  const handleAward = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedItem || !characterName.trim()) return

    setSending(true)
    setStatus(null)
    try {
      const result = await request<{ result: string }>('/api/admin/items/award', {
        method: 'POST',
        body: JSON.stringify({
          characterName: characterName.trim(),
          itemId: selectedItem.entry,
          count,
          subject: subject.trim() || undefined,
          message: message.trim() || undefined,
        }),
      })
      setStatus({ text: result.result, kind: 'success' })
    } catch (err) {
      setStatus({ text: err instanceof Error ? err.message : 'Failed to award item.', kind: 'error' })
    } finally {
      setSending(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <h2>Award Item</h2>
      </div>

      <p className="config-hint">
        Mails an item to a character by name. Works whether or not the character is currently
        online.
      </p>

      <div className="panel" style={{ padding: '16px', maxWidth: '600px' }}>
        <form className="armory-search" onSubmit={searchItems}>
          <input
            className="input"
            type="text"
            placeholder="Search for an item…"
            value={itemQuery}
            onChange={(e) => setItemQuery(e.target.value)}
          />
          <button type="submit" className="btn btn-secondary" disabled={searching}>
            {searching ? 'Searching…' : 'Search'}
          </button>
        </form>

        {itemResults.length > 0 && (
          <div className="armory-results" style={{ marginBottom: '16px' }}>
            {itemResults.map((item) => (
              <button
                type="button"
                key={item.entry}
                className="armory-result-row"
                style={{ cursor: 'pointer', textAlign: 'left', font: 'inherit' }}
                onClick={() => {
                  setSelectedItem(item)
                  setItemResults([])
                  setItemQuery(item.name)
                }}
              >
                <span className={qualityClass(item.quality)}>{item.name}</span>
                <span className="armory-result-meta">#{item.entry}</span>
              </button>
            ))}
          </div>
        )}

        {selectedItem && (
          <form className="stack-form" style={{ maxWidth: 'none', boxShadow: 'none', border: 'none', padding: 0 }} onSubmit={handleAward}>
            <label>
              Selected item
              <input className="input" type="text" value={`${selectedItem.name} (#${selectedItem.entry})`} disabled />
            </label>

            <label>
              Character name
              <input
                className="input"
                type="text"
                value={characterName}
                onChange={(e) => setCharacterName(e.target.value)}
                required
              />
            </label>

            <label>
              Count
              <input
                className="input"
                type="number"
                min={1}
                value={count}
                onChange={(e) => setCount(Number(e.target.value))}
                required
              />
            </label>

            <label>
              Subject (optional)
              <input
                className="input"
                type="text"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                placeholder="Item Delivery"
              />
            </label>

            <label>
              Message (optional)
              <input
                className="input"
                type="text"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                placeholder="An item has been sent to you by an administrator."
              />
            </label>

            <button type="submit" className="btn btn-primary" disabled={sending}>
              {sending ? 'Sending…' : 'Send Item'}
            </button>
          </form>
        )}

        {status && (
          <p className={`form-message ${status.kind === 'error' ? 'error' : 'success'}`}>{status.text}</p>
        )}
      </div>
    </section>
  )
}

export default AdminAwardItem
