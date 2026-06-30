"use client"

import { useEffect, useState, useRef } from "react"
import { startConnection, getConnection } from "@/services/signalr"
import { Conversation, Message } from "@/Interface/ChatInterface"
import Sidebar from "./Sidebar"
import ChatWindow from "./ChatWindow"
import { useSearchParams } from "next/navigation"

interface Props {
  token: string
  myId: string
}

export default function ChatPageClient({ token, myId }: Props) {
  const searchParams = useSearchParams()
  const initialUserId = searchParams.get("userId")
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [loadingConversations, setLoadingConversations] = useState(true)
  const [messages, setMessages] = useState<Message[]>([])
  const [activeUser, setActiveUser] = useState<string | null>(initialUserId || null)
  const [isNewChat, setIsNewChat] = useState(false)
  const [loadingMessages, setLoadingMessages] = useState(false)
  const activeUserRef = useRef<string | null>(null)
  const [presence, setPresence] = useState<Record<string,{
    isOnline : boolean 
    lastSeen?: string
  }>>({})
  const [typingUsers, setTypingUsers] = useState<Record<string, boolean>>({})

  const fallbackName = searchParams.get("name")
  const fallbackAvatar = searchParams.get("avatar")

  const refreshConversations = async () => {
  const res = await fetch(
    "http://localhost:5127/api/Messages/conversations",
    {
      headers: { Authorization: `Bearer ${token}` },
    }
  )

  const data = await res.json()
  setConversations(data)
}


  useEffect(() => {
    activeUserRef.current = activeUser
  }, [activeUser])

  useEffect(() => {
  if (initialUserId) {
    setActiveUser(initialUserId)
  }
}, [initialUserId])

  // ======================
  // LOAD CONVERSATIONS
  // ====================== 

useEffect(() => {
  console.log("RAW RESPONSE START")

  fetch("http://localhost:5127/api/Messages/conversations", {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then(async (res) => {
      console.log("STATUS:", res.status)

      const text = await res.text()
      console.log("RAW TEXT:", text)

      return JSON.parse(text)
    })
    .then((data) => {
      console.log("PARSED DATA:", data)
      setConversations(data)
      const presenceMap: any = {}

    data.forEach((c: Conversation) => {
      presenceMap[c.otherUserId] = {
        isOnline: c.isOnline,
        lastSeen: c.lastSeen
      }
    })

    setPresence(presenceMap)
    })
    .finally(() => {
      setLoadingConversations(false)
    })
}, [token])

//هل الشات دا جديد ولا لا

useEffect(() => {
  if (!activeUser) return

  const exists = conversations.some(
    (c) => c.otherUserId === activeUser
  )

  setIsNewChat(!exists)
}, [activeUser, conversations])

  // ======================
  // LOAD MESSAGES
  // ======================
  useEffect(() => {
    if (!activeUser) return
    setLoadingMessages(true)

    fetch(
      `http://localhost:5127/api/Messages/between/${myId}/${activeUser}`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    )
      .then((res) => res.json())
      .then(setMessages)
      .then(() => {
        setLoadingMessages(false)
      })
  }, [activeUser, myId, token])
 

  // ======================
  // MARK AS READ
  // ======================
  useEffect(() => {
    if (!activeUser) return

    setConversations((prev) =>
      prev.map((c) =>
        c.otherUserId === activeUser
          ? { ...c, unreadCount: 0 }
          : c
      )
    )

    fetch(
      `http://localhost:5127/api/Messages/mark-as-read/${activeUser}`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
      }
    )
  }, [activeUser, token])

  // ======================
  // SEND MESSAGE
  // ======================
  const sendMessage = async (content: string) => {
    if (!activeUser) return

    await fetch("http://localhost:5127/api/Messages", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        content,
        receiverId: activeUser,
      }),
    })

    //مره واحدة بس لوالشات جديد

    if (isNewChat) {
    await refreshConversations()
    setIsNewChat(false)
    }

    }

  // ======================
  // DELETE MESSAGE 
  // ======================
  const deleteMessage = async (id: string | number) => {
    const msg = messages.find((m) => m.id === id)

    // 🔥 يمنع مسح رسالة مش بتاعتي
    if (!msg || msg.senderId !== myId) {
      alert("You can't delete this message")
      return
    }

    await fetch(`http://localhost:5127/api/Messages/${id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
    })

    setMessages((prev) =>
      prev.map((m) =>
        m.id === id
          ? { ...m, content: "Message deleted", deleted: true }
          : m
      )
    )
  }

  // ======================
  // SIGNALR
  // ======================
  useEffect(() => {
    if (!token) return

    const init = async () => {
      const conn = await startConnection(token)

      conn.off("ReceiveMessage")
      conn.off("MessageDeleted")
      conn.off("MessagesRead")
      conn.off("MessageDelivered")
      conn.off("MessageSeen")

      conn.on("ReceiveMessage", (message: Message) => {
        const currentChat = activeUserRef.current

        setMessages((prev) => {
          const existsIndex = prev.findIndex((m) => m.id === message.id)

          if (existsIndex !== -1) {
            const updated = [...prev]
            updated[existsIndex] = message
            return updated
          }

          return [...prev, message].sort(
            (a, b) =>
              new Date(a.createdAt).getTime() -
              new Date(b.createdAt).getTime()
          )
        })
 
        setConversations((prev) =>
          prev.map((c) => {
            if (
              c.otherUserId === message.senderId ||
              c.otherUserId === message.receiverId
            ) {
              const isIncoming = message.receiverId === myId

              return {
                ...c,
                lastMessage: message.content,
                lastMessageAt: message.createdAt,
                unreadCount:
                  c.otherUserId === currentChat
                    ? 0
                    : isIncoming
                    ? c.unreadCount + 1
                    : c.unreadCount,
              }
            }
            return c
          })
        )
      })

      conn.on("MessageDeleted", ({ messageId }) => {
        setMessages((prev) =>
          prev.map((m) =>
            m.id === messageId
              ? { ...m, deleted: true, content: "Message deleted" }
              : m
          )
        )
      })

      conn.on("MessagesRead", ({ userId }) => {
        setConversations((prev) =>
          prev.map((c) =>
            c.otherUserId === userId
              ? { ...c, unreadCount: 0 }
              : c
          )
        )
      })

      conn.on("MessageDelivered", ({ messageId, deliveredAt }) => {
        setMessages((prev) =>
          prev.map((m) =>
            m.id === messageId
              ? { ...m, isDelivered: true, deliveredAt }
              : m
          )
        )
      })

      conn.on("MessageSeen", ({ messageId, seenAt }) => {
        setMessages((prev) =>
          prev.map((m) =>
            m.id === messageId
              ? { ...m, isSeen: true, seenAt }
              : m
          )
        )
      })

      conn.on("UserOnline", ({ userId }) => {
        setPresence(prev => ({
          ...prev,
          [userId]: { isOnline: true }
        }))
      })

      conn.on("UserOffline", ({ userId, lastSeen }) => {
        setPresence(prev => ({
          ...prev,
          [userId]: { isOnline: false, lastSeen }
        }))
      })

      conn.on("Typing", ({ fromUserId }) => {
        setTypingUsers(prev => ({
          ...prev,
          [fromUserId]: true
        }))
      })

      conn.on("StopTyping", ({ fromUserId }) => {
        setTypingUsers(prev => ({
          ...prev,
          [fromUserId]: false
        }))
      })
    }

    init()

    return () => {
      const conn = getConnection()
      conn?.off("ReceiveMessage")
      conn?.off("MessageDeleted")
      conn?.off("MessagesRead")
      conn?.off("MessageDelivered")
      conn?.off("MessageSeen")
    }
  }, [token, myId])

  return (
  <div className="h-[calc(100vh-165px)] flex md:flex-row flex-col overflow-hidden">

    {/* 🔥 SIDEBAR */}
    <div
      className={`
        ${activeUser ? "hidden md:flex" : "flex"}
        flex-col
        w-full
        md:w-80
        lg:w-96 
        border-r
      `}
    >
      <Sidebar
        conversations={conversations}
        setActiveUser={setActiveUser}
        activeUser={activeUser}
        loading={loadingConversations}
      />
    </div>

    {/* 🔥 CHAT */}
    <div
      className={`
        ${activeUser ? "flex" : "hidden md:flex"}
        flex-col
        flex-1
        min-h-0
      `}
    >
      <ChatWindow
        messages={messages}
        onSend={sendMessage}
        onDelete={deleteMessage}
        myId={myId}
        activeUser={activeUser}
        conversations={conversations}
        onBack={() => setActiveUser(null)}
        loading={loadingMessages} 
        fallbackName={fallbackName}
        fallbackAvatar={fallbackAvatar}
        presence={presence}
        typingUsers={typingUsers}
      />
    </div>

  </div>
)
}