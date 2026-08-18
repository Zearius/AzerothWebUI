import { useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import { publicApi, type PublicServerStatus } from './publicApi'

function ServerBanner() {
  const [status, setStatus] = useState<PublicServerStatus | null>(null)
  const [motd, setMotd] = useState('')

  useEffect(() => {
    publicApi.status().then(setStatus).catch(() => setStatus(null))
    publicApi.motd().then((result) => setMotd(result.content)).catch(() => setMotd(''))
  }, [])

  if (!status && !motd) {
    return null
  }

  return (
    <div className="server-banner">
      {status && (
        <div className="server-banner-status">
          <span>{status.playersOnline} online</span>
          <span>{status.charactersInWorld} characters</span>
          {status.uptime && <span>Up {status.uptime}</span>}
        </div>
      )}
      {motd && (
        <div className="server-banner-motd motd-content">
          <ReactMarkdown>{motd}</ReactMarkdown>
        </div>
      )}
    </div>
  )
}

export default ServerBanner
