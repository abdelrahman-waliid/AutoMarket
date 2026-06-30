"use client"

import { useRouter } from "next/navigation"

export default function QuickActions() {
  const router = useRouter()

  return (
    <div className="rounded-2xl border border-gray-200 p-5 bg-background shadow-sm space-y-3">
      
      <h2 className="text-lg font-semibold mb-4">
        Quick Actions
      </h2>

      <button
        onClick={() => router.push("/user/my-cars")}
        className="w-full border border-gray-200 rounded-lg p-3 text-left hover:bg-muted cursor-pointer"
      >
        Add New Listing
      </button>

      <button
        onClick={() => router.push("/user/messages")}
        className="w-full border border-gray-200 rounded-lg p-3 text-left hover:bg-muted cursor-pointer"
      >
        Check Inbox
      </button>

      <button
        onClick={() => router.push("/profile")}
        className="w-full border border-gray-200 rounded-lg p-3 text-left hover:bg-muted cursor-pointer"
      >
        Update Profile
      </button>

    </div>
  )
}