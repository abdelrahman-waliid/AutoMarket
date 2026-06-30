export interface DashBoardResponse {
  totalCarsOwned: number
  unreadMessagesCount: number
  totalViewsAcrossListings: number
  recentActivity: RecentActivityData[]
}

export interface RecentActivityData {
  type: string
  description: string
  occurredAt: string
}
