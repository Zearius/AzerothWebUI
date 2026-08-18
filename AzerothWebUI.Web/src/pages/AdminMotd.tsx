import { useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import { adminApi } from '../adminApi'
import { publicApi } from '../publicApi'

function AdminMotd() {
  const [content, setContent] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    publicApi
      .motd()
      .then((motd) => setContent(motd.content))
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  const save = async () => {
    setSaving(true)
    setError('')
    setMessage('')
    try {
      await adminApi.updateMotd(content)
      setMessage('Message of the day saved.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section>
      <div className="page-header">
        <h2>Message of the Day</h2>
        <button type="button" className="btn btn-primary" onClick={save} disabled={loading || saving}>
          {saving ? 'Saving…' : 'Save'}
        </button>
      </div>

      <p>Shown on the public landing page. Supports Markdown.</p>

      {error && <p className="form-message error">{error}</p>}
      {message && <p className="form-message success">{message}</p>}

      {!loading && (
        <div className="motd-editor">
          <textarea
            className="input motd-textarea"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Write an announcement…"
            rows={12}
          />
          <div className="motd-preview">
            <div className="motd-preview-label">Preview</div>
            <div className="motd-content">
              <ReactMarkdown>{content || '*Nothing to preview yet.*'}</ReactMarkdown>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

export default AdminMotd
