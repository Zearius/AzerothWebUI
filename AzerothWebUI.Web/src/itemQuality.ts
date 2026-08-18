const QUALITY_NAMES = [
  'Poor',
  'Common',
  'Uncommon',
  'Rare',
  'Epic',
  'Legendary',
  'Artifact',
  'Heirloom',
]

const QUALITY_CLASSES = ['q-poor', 'q-common', 'q-uncommon', 'q-rare', 'q-epic', 'q-legendary', 'q-artifact', 'q-heirloom']

export function qualityName(quality: number): string {
  return QUALITY_NAMES[quality] ?? 'Unknown'
}

export function qualityClass(quality: number): string {
  return QUALITY_CLASSES[quality] ?? 'q-common'
}

const DROP_SOURCE_LABELS: Record<string, string> = {
  creature_loot_template: 'Dropped by',
  fishing_loot_template: 'Fished from zone',
  gameobject_loot_template: 'Found in object',
  skinning_loot_template: 'Skinned from',
  disenchant_loot_template: 'Disenchanted from item',
  pickpocketing_loot_template: 'Pickpocketed from',
  prospecting_loot_template: 'Prospected from',
  milling_loot_template: 'Milled from',
  mail_loot_template: 'Received by mail',
  item_loot_template: 'Found inside item',
  player_loot_template: 'Looted from player',
  spell_loot_template: 'Granted by spell',
}

export function dropSourceLabel(sourceType: string): string {
  return DROP_SOURCE_LABELS[sourceType] ?? sourceType
}
