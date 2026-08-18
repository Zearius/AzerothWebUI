import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { armoryApi, type CharacterDetail } from '../armoryApi'
import { raceName, className, equipmentSlotName } from '../wowEnums'
import { qualityClass } from '../itemQuality'
import PublicHeader from '../PublicHeader'

function ArmoryCharacterDetail() {
  const { name } = useParams<{ name: string }>()
  const [character, setCharacter] = useState<CharacterDetail | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!name) return
    setLoading(true)
    setError('')
    armoryApi
      .getCharacter(name)
      .then(setCharacter)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [name])

  return (
    <>
      <PublicHeader />
      <div className="armory-page">
        <p>
          <Link to="/armory/characters">← Back to search</Link>
        </p>

        {loading && <p>Loading…</p>}
        {error && <p className="form-message error">{error}</p>}

        {character && (
          <>
            <div className="armory-detail-header">
              <h1>{character.name}</h1>
              <span className="armory-detail-meta">
                Level {character.level} {raceName(character.race)} {className(character.class)}
                {character.guildName ? ` · <${character.guildName}>` : ''}
                {character.online ? ' · Online' : ' · Offline'}
              </span>
            </div>

            <h2>Equipment</h2>
            <div className="equipment-grid">
              {character.equippedItems.map((item) => (
                <Link
                  key={item.slot}
                  to={`/armory/items/${item.itemEntry}`}
                  className="equipment-slot"
                >
                  <span className="equipment-slot-label">{equipmentSlotName(item.slot)}</span>
                  <span className={qualityClass(item.quality)}>{item.name}</span>
                </Link>
              ))}
              {character.equippedItems.length === 0 && <p>No equipped items.</p>}
            </div>
          </>
        )}
      </div>
    </>
  )
}

export default ArmoryCharacterDetail
