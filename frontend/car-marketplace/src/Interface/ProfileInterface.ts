export interface UserProfileInterface {
  id: string
  fullName: string
  email: string
  role: string
  avatarUrl: any
  createdAt: string
  updatedAt: string
}

export interface UpdatedData{
  "fullName": string
  "email": string
}
export interface changedPasswordData{
  currentPassword : string 
  newPassword : string
}