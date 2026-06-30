//دا علشان نعمل logout لو في مكان التوكن فيه expired 
"use client"

import { useEffect } from "react"
import { signOut } from "next-auth/react"

export default function ForceLogout() {
  useEffect(() => {
    signOut({ callbackUrl: "/login" })
  }, [])

  return (
    <div className="flex justify-center items-center py-20">
      <p>Token expired....please login again!!</p>
    </div>
  )
}