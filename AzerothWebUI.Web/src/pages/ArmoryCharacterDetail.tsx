import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { armoryApi, type CharacterDetail, type EquippedItem } from '../armoryApi'
import {
  raceName,
  className,
  equipmentSlotName,
  classColor,
  factionName,
  EQUIPMENT_LEFT_COLUMN,
  EQUIPMENT_RIGHT_COLUMN,
} from '../wowEnums'
import { qualityClass } from '../itemQuality'
import PublicHeader from '../PublicHeader'

function EquipmentSlot({ item }: { item?: EquippedItem }) {
  if (!item) {
    return <div className="equipment-slot equipment-slot-empty" />
  }

  return (
    <Link to={`/armory/items/${item.itemEntry}`} className="equipment-slot">
      <div className={`equipment-slot-icon q-border-${item.quality}`} />
      <div className="equipment-slot-info">
        <span className="equipment-slot-label">{equipmentSlotName(item.slot)}</span>
        <span className={qualityClass(item.quality)}>{item.name}</span>
      </div>
      <span className="equipment-slot-ilvl">{item.itemLevel}</span>
    </Link>
  )
}

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

  const bySlot = useMemo(() => {
    const map = new Map<number, EquippedItem>()
    for (const item of character?.equippedItems ?? []) {
      map.set(item.slot, item)
    }
    return map
  }, [character])

  const averageItemLevel = useMemo(() => {
    const items = (character?.equippedItems ?? []).filter((item) => item.itemLevel > 0)
    if (items.length === 0) return 0
    return Math.round(items.reduce((sum, item) => sum + item.itemLevel, 0) / items.length)
  }, [character])

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
            <div className={`armory-character-banner faction-${factionName(character.race).toLowerCase()}`}>
              <div className={`faction-badge faction-badge-${factionName(character.race).toLowerCase()}`} />
              <div className="armory-character-banner-info">
                <h1 style={{ color: classColor(character.class) }}>{character.name}</h1>
                <span className="armory-detail-meta">
                  Level {character.level} {raceName(character.race)} {className(character.class)}
                  {character.guildName ? ` · <${character.guildName}>` : ''}
                  {character.online ? ' · Online' : ' · Offline'}
                </span>
              </div>
              {averageItemLevel > 0 && (
                <div className="armory-ilvl-badge">
                  <span className="armory-ilvl-value">{averageItemLevel}</span>
                  <span className="armory-ilvl-label">Item Level</span>
                </div>
              )}
            </div>

            <div className="paperdoll">
              <div className="paperdoll-column">
                {EQUIPMENT_LEFT_COLUMN.map((slot) => (
                  <EquipmentSlot key={slot} item={bySlot.get(slot)} />
                ))}
              </div>
              <div className="paperdoll-column">
                {EQUIPMENT_RIGHT_COLUMN.map((slot) => (
                  <EquipmentSlot key={slot} item={bySlot.get(slot)} />
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    </>
  )
}

export default ArmoryCharacterDetail
