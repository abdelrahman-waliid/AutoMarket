export interface SuccessRegisterInterface {
  id: string
  fullName: string
  email: string
  role: string
  avatarUrl: any
  createdAt: string
  updatedAt: string
}

export interface FaildRegisterInterface {
  type: string
  title: string
  status: number
  detail: string
  instance: string
  traceId: string
  errorCode: string
  exceptionType: string
}