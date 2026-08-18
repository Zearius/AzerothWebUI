import { request } from './apiClient'

export type CharacterSummary = {
  guid: number
  name: string
  race: number
  class: number
  level: number
  guildName: string | null
  online: boolean
}

export type EquippedItem = {
  slot: number
  itemEntry: number
  name: string
  quality: number
  displayId: number
  itemLevel: number
}

export type CharacterDetail = CharacterSummary & {
  gender: number
  equippedItems: EquippedItem[]
}

export type ItemSearchResult = {
  entry: number
  name: string
  quality: number
  displayId: number
  itemLevel: number
}

export type ItemStat = {
  type: number
  value: number
}

export type ItemDetail = {
  entry: number
  name: string
  quality: number
  displayId: number
  itemLevel: number
  requiredLevel: number
  class: number
  subclass: number
  inventoryType: number
  description: string | null
  stats: ItemStat[]
}

export type DropSource = {
  sourceType: string
  sourceEntry: number
  sourceName: string | null
  chance: number
  minCount: number
  maxCount: number
}

export type ItemDetailResult = {
  item: ItemDetail
  dropSources: DropSource[]
}

export const armoryApi = {
  searchCharacters: (query: string) =>
    request<CharacterSummary[]>(`/api/armory/characters?q=${encodeURIComponent(query)}`),
  getCharacter: (name: string) =>
    request<CharacterDetail>(`/api/armory/characters/${encodeURIComponent(name)}`),
  searchItems: (query: string) =>
    request<ItemSearchResult[]>(`/api/armory/items/search?q=${encodeURIComponent(query)}`),
  getItem: (id: number) => request<ItemDetailResult>(`/api/armory/items/${id}`),
}
