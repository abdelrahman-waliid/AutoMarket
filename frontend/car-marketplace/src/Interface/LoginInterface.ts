export interface SuccessLoginInterface {
  user: UserInterface
  token: string
}

export interface UserInterface {
  id: string
  fullName: string
  email: string
  role: string
  avatarUrl: any
  createdAt: string
  updatedAt: string
}
