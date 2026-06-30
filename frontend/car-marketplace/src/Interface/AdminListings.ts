export interface AdminListingsResponse {
  items: AdminListingsResponse[] | []
}

export interface AdminListingsResponse {
  id: string
  title: string
  brand: string
  model: string
  description: string
  price: number
  location: string
  status: string
  views: number
  year: number
  mileage: number
  fuelType: string
  transmissionType: string
  ownerId: string
  ownerFullName: string
  createdAt: string
  updatedAt: string
  imageUrls: string[]
}
