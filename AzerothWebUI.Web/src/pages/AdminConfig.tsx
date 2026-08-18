import { useEffect, useMemo, useState } from 'react'
import { adminApi, type ConfigEntry, type ConfigFile } from '../adminApi'

function flipBoolean(value: string): string {
  const lower = value.toLowerCase()
  if (lower === 'true') return 'false'
  if (lower === 'false') return 'true'
  return value === '1' ? '0' : '1'
}

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
  const isOn = entry.currentValue.toLowerCase() === 'true' || entry.currentValue === '1'

  const save = async (value: string) => {
    setSaving(true)
    try {
      await onSave(entry.key, value)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={`config-row ${entry.requiresRestart ? 'restart-required' : ''}`}>
      <div className="config-row-label">
        <span className="config-key">
          {entry.key}
          {entry.requiresRestart && <span className="restart-badge">restart required</span>}
        </span>
        {entry.description && <span className="config-description">{entry.description}</span>}
      </div>

      {entry.isToggle ? (
        <button
          type="button"
          role="switch"
          aria-checked={isOn}
          className={`toggle ${isOn ? 'on' : 'off'}`}
          disabled={saving}
          onClick={() => save(flipBoolean(entry.currentValue))}
        >
          <span className="toggle-thumb" />
        </button>
      ) : (
        <div className="config-text-input">
          <input
            className="input"
            type="text"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            disabled={saving}
          />
          <button
            type="button"
            className="btn btn-sm btn-primary"
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
  const [files, setFiles] = useState<ConfigFile[]>([])
  const [activeFile, setActiveFile] = useState('worldserver')
  const [entries, setEntries] = useState<ConfigEntry[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [actionMessage, setActionMessage] = useState<{ text: string; kind: 'live' | 'restart' | 'error' } | null>(null)
  const [search, setSearch] = useState('')
  const [openSections, setOpenSections] = useState<Set<string>>(new Set())

  useEffect(() => {
    adminApi.configFiles().then(setFiles).catch(() => setFiles([]))
  }, [])

  const load = (file: string) => {
    setLoading(true)
    setError('')
    adminApi
      .config(file)
      .then(setEntries)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }

  useEffect(() => load(activeFile), [activeFile])

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
    setActionMessage(null)
    try {
      const result = await adminApi.updateConfig(activeFile, key, value)
      setEntries((prev) => prev.map((e) => (e.key === key ? result.entry : e)))
      setActionMessage(
        result.requiresRestart
          ? { text: `${key} saved. Restart the worldserver to apply this change.`, kind: 'restart' }
          : { text: `${key} updated. ${result.reloadResult ?? ''}`, kind: 'live' },
      )
    } catch (err) {
      setActionMessage({ text: err instanceof Error ? err.message : 'Save failed.', kind: 'error' })
    }
  }

  return (
    <section>
      <div className="page-header">
        <h2>Config</h2>
        <button type="button" className="btn btn-secondary" onClick={() => load(activeFile)} disabled={loading}>
          {loading ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      <div className="config-file-tabs">
        {files.map((file) => (
          <button
            key={file.id}
            type="button"
            className={`config-file-tab ${activeFile === file.id ? 'active' : ''}`}
            onClick={() => setActiveFile(file.id)}
          >
            {file.displayName}
          </button>
        ))}
      </div>

      <p className="config-hint">
        Settings marked <span className="restart-badge inline">restart required</span> are saved to
        disk immediately but only take effect after the worldserver is restarted. Everything else
        applies live.
      </p>

      <input
        type="text"
        className="input config-search"
        placeholder="Search settings…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {error && <p className="form-message error">{error}</p>}
      {actionMessage && (
        <p className={`form-message ${actionMessage.kind === 'error' ? 'error' : ''} ${actionMessage.kind === 'restart' ? 'restart-message' : ''}`}>
          {actionMessage.text}
        </p>
      )}

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
