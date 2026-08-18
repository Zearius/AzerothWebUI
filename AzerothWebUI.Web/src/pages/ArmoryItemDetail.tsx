import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { armoryApi, type ItemDetailResult } from '../armoryApi'
import { qualityClass, qualityName, dropSourceLabel } from '../itemQuality'
import { statTypeName } from '../wowEnums'
import PublicHeader from '../PublicHeader'

function ArmoryItemDetail() {
  const { id } = useParams<{ id: string }>()
  const [result, setResult] = useState<ItemDetailResult | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    setLoading(true)
    setError('')
    armoryApi
      .getItem(Number(id))
      .then(setResult)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [id])

  const groupedDropSources = useMemo(() => {
    if (!result) return []
    const map = new Map<string, typeof result.dropSources>()
    for (const source of result.dropSources) {
      const list = map.get(source.sourceType) ?? []
      list.push(source)
      map.set(source.sourceType, list)
    }
    return [...map.entries()]
  }, [result])

  return (
    <>
      <PublicHeader />
      <div className="armory-page">
        <p>
          <Link to="/armory/items">← Back to search</Link>
        </p>

        {loading && <p>Loading…</p>}
        {error && <p className="form-message error">{error}</p>}

        {result && (
          <>
            <div className={`item-card q-border-${result.item.quality}`}>
              <div className={`item-card-icon q-border-${result.item.quality}`} />
              <div className="item-card-info">
                <h1 className={qualityClass(result.item.quality)}>{result.item.name}</h1>
                <span className={`item-card-quality ${qualityClass(result.item.quality)}`}>
                  {qualityName(result.item.quality)}
                </span>
                <span className="armory-detail-meta">
                  Item Level {result.item.itemLevel}
                  {result.item.requiredLevel > 0 ? ` · Requires Level ${result.item.requiredLevel}` : ''}
                </span>

                {result.item.stats.length > 0 && (
                  <div className="item-stats">
                    {result.item.stats.map((stat) => (
                      <span key={stat.type}>
                        +{stat.value} {statTypeName(stat.type)}
                      </span>
                    ))}
                  </div>
                )}

                {result.item.description && <p className="item-card-description">{result.item.description}</p>}
              </div>
            </div>

            <h2 style={{ marginTop: '24px' }}>Where to find it</h2>
            {groupedDropSources.length === 0 && <p>No known drop sources.</p>}
            <div className="drop-sources">
              {groupedDropSources.map(([sourceType, sources]) => (
                <div key={sourceType}>
                  <div className="drop-source-group-title">{dropSourceLabel(sourceType)}</div>
                  {sources.map((source, i) => (
                    <div className="drop-source-row" key={`${source.sourceEntry}-${i}`}>
                      <span>{source.sourceName ?? `#${source.sourceEntry}`}</span>
                      <span>{source.chance.toFixed(2)}%</span>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </>
  )
}

export default ArmoryItemDetail
