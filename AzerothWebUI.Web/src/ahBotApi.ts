import { request } from './apiClient'

export type AhBotQualitySettings = {
  percentTradeGoods: number
  percentItems: number
  minPrice: number
  maxPrice: number
  minBidPrice: number
  maxBidPrice: number
  maxStack: number
  buyerPrice: number
}

export type AhBotHouse = {
  auctionHouse: number
  name: string
  minItems: number
  maxItems: number
  buyerBiddingInterval: number
  buyerBidsPerInterval: number
  grey: AhBotQualitySettings
  white: AhBotQualitySettings
  green: AhBotQualitySettings
  blue: AhBotQualitySettings
  purple: AhBotQualitySettings
  orange: AhBotQualitySettings
  yellow: AhBotQualitySettings
}

export type AhBotDisabledItem = {
  itemId: number
  itemName: string | null
}

export const ahBotApi = {
  houses: () => request<AhBotHouse[]>('/api/admin/ahbot/houses'),
  updateHouse: (auctionHouse: number, settings: AhBotHouse) =>
    request<void>(`/api/admin/ahbot/houses/${auctionHouse}`, {
      method: 'PUT',
      body: JSON.stringify(settings),
    }),
  disabledItems: () => request<AhBotDisabledItem[]>('/api/admin/ahbot/disabled-items'),
  addDisabledItem: (itemId: number) =>
    request<void>(`/api/admin/ahbot/disabled-items/${itemId}`, { method: 'POST' }),
  removeDisabledItem: (itemId: number) =>
    request<void>(`/api/admin/ahbot/disabled-items/${itemId}`, { method: 'DELETE' }),
}
