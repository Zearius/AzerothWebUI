const RACE_NAMES: Record<number, string> = {
  1: 'Human',
  2: 'Orc',
  3: 'Dwarf',
  4: 'Night Elf',
  5: 'Undead',
  6: 'Tauren',
  7: 'Gnome',
  8: 'Troll',
  9: 'Goblin',
  10: 'Blood Elf',
  11: 'Draenei',
}

const CLASS_NAMES: Record<number, string> = {
  1: 'Warrior',
  2: 'Paladin',
  3: 'Hunter',
  4: 'Rogue',
  5: 'Priest',
  6: 'Death Knight',
  7: 'Shaman',
  8: 'Mage',
  9: 'Warlock',
  11: 'Druid',
}

// WotLK paperdoll equipment slot ids, in display order.
const EQUIPMENT_SLOTS: Record<number, string> = {
  0: 'Head',
  1: 'Neck',
  2: 'Shoulder',
  3: 'Shirt',
  4: 'Chest',
  5: 'Waist',
  6: 'Legs',
  7: 'Feet',
  8: 'Wrist',
  9: 'Hands',
  10: 'Finger 1',
  11: 'Finger 2',
  12: 'Trinket 1',
  13: 'Trinket 2',
  14: 'Back',
  15: 'Main Hand',
  16: 'Off Hand',
  17: 'Ranged',
  18: 'Tabard',
}

export function raceName(race: number): string {
  return RACE_NAMES[race] ?? `Race ${race}`
}

export function className(wowClass: number): string {
  return CLASS_NAMES[wowClass] ?? `Class ${wowClass}`
}

export function equipmentSlotName(slot: number): string {
  return EQUIPMENT_SLOTS[slot] ?? `Slot ${slot}`
}

// item_template stat_type enum (ItemModType).
const STAT_TYPE_NAMES: Record<number, string> = {
  0: 'Mana',
  1: 'Health',
  3: 'Agility',
  4: 'Strength',
  5: 'Intellect',
  6: 'Spirit',
  7: 'Stamina',
  12: 'Defense Rating',
  13: 'Dodge Rating',
  14: 'Parry Rating',
  15: 'Block Rating',
  16: 'Hit Rating',
  17: 'Critical Strike Rating',
  20: 'Resilience Rating',
  21: 'Haste Rating',
  28: 'Expertise Rating',
  31: 'Block Value',
}

export function statTypeName(type: number): string {
  return STAT_TYPE_NAMES[type] ?? `Stat ${type}`
}
