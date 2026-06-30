"use client"

import { useState } from "react"
import { Message } from "@/Interface/ChatInterface"
import { formatTimeAgo } from "@/Helpers/formatTimeAgo"
import { Check, CheckCheck } from "lucide-react"

export default function MessageBubble({
  message,
  isMe,
  onDelete,
}: {
  message: Message
  isMe: boolean
  onDelete: (id: string | number) => void
}) {
  const [open, setOpen] = useState(false)

  return (
    <div className={`flex ${isMe ? "justify-end" : "justify-start"}`}>
      <div className="relative group">

        {/* MESSAGE BOX */}
        <div
          className={`px-4 py-2 rounded-2xl max-w-xs text-sm shadow relative ${
            isMe ? "bg-[#144d37] text-white" : "bg-popover"
          }`}
        >
          {/* deleted message UI */}
          {message.deleted ? (
            <span className="italic opacity-60">
              This message was deleted
            </span>
          ) : (
            message.content
          )}

          <div className="text-[10px] mt-1 flex justify-end items-center gap-2">
            <span className={message.isSeen && isMe ? `text-gray-400` : `text-gray-400`}>
                {formatTimeAgo(message.createdAt)}  
            </span> 
            {isMe && (
                <>
                  {!message.isDelivered ? (
                    // ✔ sent
                    <Check className="size-3.5 text-gray-400" />
                  ) : (
                    // ✔✔ delivered / seen
                    <CheckCheck
                      className={`size-3.5 ${
                        message.isSeen ? "text-[#2badd4]" : "text-gray-400"
                      }`}
                    />
                  )}
                </>
)}
          </div>

          {/* 3 dots (🔥 يظهر بس لرسالتي) */}
          {!message.deleted && isMe && (
            <button
              onClick={() => setOpen(!open)}
              className="absolute top-1 right-2 opacity-0 group-hover:opacity-100 transition cursor-pointer"
            >
              ⋮
            </button>
          )}

          {/* dropdown (🔥 برضو بس لرسالتي) */}
          {open && !message.deleted && isMe && (
            <div className="absolute right-0 top-6 bg-white hover:bg-red-500 shadow rounded-md text-black text-xs z-10">
              <button
                onClick={() => onDelete(message.id)}
                className="px-3 py-2 w-full text-right cursor-pointer"
              >
                Delete
              </button>
            </div>
          )}
        </div>

      </div>
    </div>
  )
}