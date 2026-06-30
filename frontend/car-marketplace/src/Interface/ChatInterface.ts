export interface Message {
  id: string | number
  senderId: string
  receiverId: string
  content: string
  createdAt: string
  isRead?: boolean
  deleted?: boolean 
  isDelivered?: boolean
  deliveredAt?: string
  isSeen?: boolean
  seenAt?: string
}

export interface Conversation {
  otherUserId: string
  otherUserName: string
  otherUserAvatar: string
  lastMessage: string
  lastMessageAt: string
  unreadCount: number
  isOnline : boolean
  lastSeen : string
}