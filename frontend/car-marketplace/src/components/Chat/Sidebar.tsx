import { Conversation } from "@/Interface/ChatInterface"
import { User } from "lucide-react"

export default function Sidebar({
  conversations,
  setActiveUser,
  activeUser,
  loading
}: {
  conversations: Conversation[]
  setActiveUser: (id: string) => void
  activeUser: string | null
  loading : boolean
}) {
  return (
    <div className="h-full border-r flex flex-col w-full">

      {/* HEADER */}
      <div className="p-4 font-bold text-lg border-b border-border sticky top-0 bg-background z-10 shadow-lg">
        Chats
      </div>

      {/* CONTENT */}
      <div className="flex-1 overflow-y-auto">

        
        {/* EMPTY STATE */}
        {loading ? (
  <div className="p-4 space-y-4">
    {[1,2,3,4,5].map(i => (
      <div key={i} className="flex items-center gap-3 animate-pulse">
        
        <div className="w-10 h-10 rounded-full bg-muted" />

        <div className="flex-1 space-y-2">
          <div className="h-3 bg-muted rounded w-1/2" />
          <div className="h-3 bg-muted rounded w-3/4" />
        </div>

      </div>
    ))}
  </div>
) : conversations.length === 0 ? (
          <div className="h-full flex flex-col items-center justify-center text-center p-6 text-gray-500">
            
            <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mb-3">
              <User className="w-7 h-7 text-gray-400" />
            </div>

            <p className="font-semibold text-gray-600">
              No conversations yet
            </p>

            <p className="text-sm text-gray-400 mt-1">
              Start a new chat to see messages here
            </p>

          </div>
        ) : (
          conversations.map((conv) => (
            <div
              key={conv.otherUserId}
              onClick={() => setActiveUser(conv.otherUserId)}
              className={`p-4 cursor-pointer hover:bg-muted flex gap-3 items-center border-b border-border ${
                activeUser === conv.otherUserId ? "bg-muted" : ""
              }`}
            >

              {/* AVATAR */}
              {conv.otherUserAvatar ? (
                <img
                  src={conv.otherUserAvatar}
                  alt="avatar"
                  className="w-10 h-10 rounded-full object-cover"
                />
              ) : (
                <div className="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center">
                  <User className="w-5 h-5 text-gray-600" />
                </div>
              )}

              <div className="flex-1">
                <div className="flex justify-between items-center">
                  <div className="font-semibold">
                    {conv.otherUserName}
                  </div>

                  {conv.unreadCount > 0 && (
                    <span className="text-xs bg-blue-500 text-white px-2 py-0.5 rounded-full">
                      {conv.unreadCount}
                    </span>
                  )}
                </div>

                <div className="text-sm text-muted-foreground truncate">
                  {conv.lastMessage}
                </div>
              </div>

            </div>
          ))
        )}

      </div>
    </div>
  )
}