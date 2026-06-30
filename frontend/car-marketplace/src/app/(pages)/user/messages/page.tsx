import { getServerSession } from "next-auth"
import { authOptions } from "@/auth" 
import ChatPageClient from "@/components/Chat/ChatPageClient"

export default async function MessagesPage() {
  const session = await getServerSession(authOptions)

  if (!session) {
    return <div>Unauthorized</div>
  }

  return (
    <ChatPageClient
      token={session.token}
      myId={session.user.id}
    />
  )
}