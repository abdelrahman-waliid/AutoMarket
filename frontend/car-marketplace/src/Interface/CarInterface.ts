export interface CarsResponse {
  items: CarResponse[] | []
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface CarResponse {
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
  ownerEmail: string
  ownerAvatarUrl: string
  ownerRole: string
  ownerCreatedAt: string
  ownerUpdatedAt: string
  createdAt: string
  updatedAt: string
  imageUrls: string[]
}


export interface DetailsOfNewCar {
  title: string
  brand: string
  model: string
  description: string
  price: number
  location: string
  year: number
  mileage: number
  fuelType: string
  transmissionType: string
  imageUrls: string[]
}
