"use client"

import { useRef, useEffect, useState } from "react"
import MessageBubble from "./MessageBubble"
import { Message, Conversation } from "@/Interface/ChatInterface"
import { ArrowLeftCircle, Send, User } from "lucide-react" 
import { getConnection } from "@/services/signalr"
import { formatTimeAgoMinus3Hours } from "@/Helpers/formatTimeAgoMinus"

export default function ChatWindow({
  messages,
  onSend,
  onDelete,
  myId,
  activeUser,
  conversations,
  onBack ,
  loading ,
  fallbackName,
  fallbackAvatar ,
  presence ,
  typingUsers
}: any) {

  const [input, setInput] = useState("")
  const bottomRef = useRef<HTMLDivElement | null>(null)
  const userPresence = presence?.[activeUser]
  const isTyping = typingUsers?.[activeUser]
  const typingTimeout = useRef<NodeJS.Timeout | null>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [messages])

  const currentUser = conversations?.find(
    (c: Conversation) => c.otherUserId === activeUser
  )

  const name = currentUser?.otherUserName || fallbackName
  const avatar = currentUser?.otherUserAvatar || fallbackAvatar

  const handleSend = () => {
    if (!input.trim()) return
    onSend(input)
    setInput("")
  }

  if (!activeUser) {
    return (
      <div className="flex-1 flex items-center justify-center text-gray-500">
        Select a conversation
      </div>
    )
  }

  return (
    <div className="flex flex-col flex-1 min-h-0 h-full">

      {/* HEADER UPDATED */}
      <div className="px-3 py-2 border-b sticky top-0 bg-background z-10 flex items-center gap-2">

        {/* يظهر في الموبايل بس */}
          <button
            onClick={onBack}
            className="md:hidden text-gray-600 text-lg cursor-pointer"
          >
            <ArrowLeftCircle className="text-sm text-popover-foreground hover:text-primary flex gap-1.5 font-bold pl-0 hover:pl-2 transition-all cursor-pointer"/>
          </button>
          <div className="relative">
            {avatar && avatar != "null" && avatar != undefined ? (
                <img
                    src={avatar}
                    alt="avatar"
                    className="w-9 h-9 rounded-full object-cover"
                />
                ) : (
                <div className="w-9 h-9 rounded-full bg-gray-300 flex items-center justify-center">
                    <User className="w-5 h-5 text-gray-600" />
                </div>
            )}
            {userPresence?.isOnline && (
              <span className="absolute bottom-0 right-0 w-2.5 h-2.5 bg-green-500 rounded-full border border-white" />
            )}

          </div>

      <div className="flex flex-col ">

        <div className="font-medium text-sm">
          {name}
        </div>
        
        <div className="text-[11px] text-gray-500">
          {userPresence?.isOnline
            ? "Online"
            : userPresence?.lastSeen
            ? `Last seen ${formatTimeAgoMinus3Hours(userPresence.lastSeen)}`
            : ""}
        </div>

        {isTyping && (
          <div className="flex gap-1 items-center">
            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce" />
            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce delay-100" />
            <span className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce delay-200" />
          </div>
        )}
      </div>
      </div>

      {/* MESSAGES */}
      <div className="flex-1 min-h-0 overflow-y-auto overflow-x-hidden p-4 space-y-2 relative" 

        style={{
          backgroundImage: "url('/chat/BG.png')",
          backgroundRepeat: "repeat",
          backgroundSize: "auto",
        }} >
        {/* <div className="absolute top-0 -bottom-2 end-0 start-0 bg-white/5 backdrop-blur-[1px] pointer-events-none" /> */}

          <div className=" space-y-2 relative z-10"> 

            {loading ? (
            <div className="space-y-3">
              {[1,2,3,4].map((i) => {
                const isMe = i % 2 === 0

                return (
                  <div
                    key={i}
                    className={`flex ${isMe ? "justify-end" : "justify-start"}`}
                  >
                    <div
                      className={`
                        h-10 rounded-2xl animate-pulse
                        ${isMe ? "bg-blue-300 w-32" : "bg-gray-300 w-40"}
                      `}
                    />
                  </div>
                )
              })}
            </div>
            ) : (
            messages.map((msg: Message) => (
              <MessageBubble
                key={msg.id}
                message={msg}
                isMe={msg.senderId === myId}
                onDelete={onDelete}
              />
            )))}
        </div>

        <div ref={bottomRef} />
      </div>

      {/* INPUT */}
      <div className="px-3 py-2  border-t sticky bottom-0 bg-[#01112a]">
        <div className="flex gap-2">
          <input
            value={input}
            onChange={(e) => {
            setInput(e.target.value)

            const conn = getConnection()
            if (!conn || !activeUser) return

            // 🔥 ابعت Typing
            conn.invoke("Typing", activeUser)

            // 🔥 debounce علشان StopTyping
            if (typingTimeout.current) {
              clearTimeout(typingTimeout.current)
            }

            typingTimeout.current = setTimeout(() => {
              conn.invoke("StopTyping", activeUser)
            }, 500)
          }}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault()
                handleSend()
              }
            }}
            className="flex-1 border-2 rounded-lg px-2 py-1.5 text-sm  outline-none focus:border-blue-200 bg-popover"
            placeholder="Type a message..."
          />

          <button
            type="submit"
            onClick={handleSend}
            disabled={!input.trim()}
            className={`px-3 py-1.5 rounded-lg text-white transition cursor-pointer ${
              input.trim()
                ? "bg-blue-500 hover:bg-blue-600"
                : "bg-gray-300 cursor-not-allowed"
            }`}
          >
            <Send />
          </button>
        </div>
      </div>
    </div>
  )
}