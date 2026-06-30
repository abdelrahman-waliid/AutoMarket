"use client"

import { ArrowLeftCircle } from "lucide-react"
import { useRouter } from "next/navigation"

export default function BackButton() {
  const router = useRouter()

  return (
    <button
      onClick={() => {
            if (window.history.length > 1) {
                router.back()
            } else {
                router.push("/market-place")
            }
        }}
      className="text-sm text-foreground hover:text-primary flex gap-1.5 font-bold pl-0 hover:pl-2 transition-all  items-center cursor-pointer"
    >
       <ArrowLeftCircle/>
       <span>Back</span>
    </button>
  )
}