export interface AdminUsersResponse {
  items: AdminUsersResponse[] | []
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface AdminUsersResponse {
  id: string
  fullName: string
  email: string
  role: string
  avatarUrl: any
  createdAt: string
  updatedAt: string
}
