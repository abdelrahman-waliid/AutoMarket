export interface AiResponse {
  estimatedPrice: number
  minPrice: number
  maxPrice: number
  confidenceScore: number
  priceStatus?: string
  percentageDifference?: number
  insights: string[]
}
