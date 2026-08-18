import { useEffect, useMemo, useState } from 'react'
import { adminApi, type ConfigEntry } from '../adminApi'

function ConfigRow({
  entry,
  onSave,
}: {
  entry: ConfigEntry
  onSave: (key: string, value: string) => Promise<void>
}) {
  const [draft, setDraft] = useState(entry.currentValue)
  const [saving, setSaving] = useState(false)

  useEffect(() => setDraft(entry.currentValue), [entry.currentValue])

  const dirty = draft !== entry.currentValue

  const save = async (value: string) => {
    setSaving(true)
    try {
      await onSave(entry.key, value)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="config-row">
      <div className="config-row-label">
        <span className="config-key">{entry.key}</span>
        {entry.description && <span className="config-description">{entry.description}</span>}
      </div>

      {entry.isToggle ? (
        <button
          type="button"
          role="switch"
          aria-checked={entry.currentValue === '1'}
          className={`toggle ${entry.currentValue === '1' ? 'on' : 'off'}`}
          disabled={saving}
          onClick={() => save(entry.currentValue === '1' ? '0' : '1')}
        >
          <span className="toggle-thumb" />
        </button>
      ) : (
        <div className="config-text-input">
          <input
            type="text"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            disabled={saving}
          />
          <button
            type="button"
            className="counter"
            disabled={saving || !dirty}
            onClick={() => save(draft)}
          >
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      )}
    </div>
  )
}

function AdminConfig() {
  const [entries, setEntries] = useState<ConfigEntry[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [actionMessage, setActionMessage] = useState('')
  const [search, setSearch] = useState('')
  const [openSections, setOpenSections] = useState<Set<string>>(new Set())

  const load = () => {
    setLoading(true)
    setError('')
    adminApi
      .config()
      .then(setEntries)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return entries
    return entries.filter(
      (e) => e.key.toLowerCase().includes(term) || e.description.toLowerCase().includes(term),
    )
  }, [entries, search])

  const grouped = useMemo(() => {
    const map = new Map<string, ConfigEntry[]>()
    for (const entry of filtered) {
      const list = map.get(entry.section) ?? []
      list.push(entry)
      map.set(entry.section, list)
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b))
  }, [filtered])

  const toggleSection = (section: string) => {
    setOpenSections((prev) => {
      const next = new Set(prev)
      if (next.has(section)) {
        next.delete(section)
      } else {
        next.add(section)
      }
      return next
    })
  }

  const handleSave = async (key: string, value: string) => {
    setActionMessage('')
    try {
      const result = await adminApi.updateConfig(key, value)
      setEntries((prev) => prev.map((e) => (e.key === key ? result.entry : e)))
      setActionMessage(`${key} updated. ${result.reloadResult}`)
    } catch (err) {
      setActionMessage(err instanceof Error ? err.message : 'Save failed.')
    }
  }

  return (
    <section>
      <div className="page-header">
        <h2>Worldserver Config</h2>
        <button type="button" className="counter" onClick={load} disabled={loading}>
          {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      <p className="config-hint">
        Only settings that apply live (via <code>reload config</code>) are shown here. Settings
        that require a worldserver restart are not editable from this page.
      </p>

      <input
        type="text"
        className="config-search"
        placeholder="Search settings…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {error && <p className="form-message error">{error}</p>}
      {actionMessage && <p className="form-message">{actionMessage}</p>}

      <div className="config-sections">
        {grouped.map(([section, sectionEntries]) => {
          const isOpen = search.trim().length > 0 || openSections.has(section)
          return (
            <div className="config-section" key={section}>
              <button
                type="button"
                className="config-section-header"
                onClick={() => toggleSection(section)}
              >
                <span>{isOpen ? '▾' : '▸'} {section || '(uncategorized)'}</span>
                <span className="config-section-count">{sectionEntries.length}</span>
              </button>

              {isOpen && (
                <div className="config-section-body">
                  {sectionEntries.map((entry) => (
                    <ConfigRow key={entry.key} entry={entry} onSave={handleSave} />
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </div>
    </section>
  )
}

export default AdminConfig
