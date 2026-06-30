"use client"

import { Button } from "@/components/ui/button"
import { AlertTriangle, RefreshCcw, Home } from "lucide-react"
import { useRouter } from "next/navigation"

export default function GlobalError({
  error,
  reset,
}: {
  error: Error
  reset: () => void
}) {

  const router = useRouter()

  return (
    <div className="min-h-screen flex items-center justify-center px-6">

      <div className="bg-background text-center max-w-lg">

        {/* Icon */}
        <div className="mx-auto w-fit bg-red-100 text-red-600 p-6 rounded-full mb-6">
          <AlertTriangle size={42} />
        </div>

        {/* Title */}
        <h1 className="text-3xl font-bold mb-3">
          Something went wrong
        </h1>

        {/* Description */}
        <p className="text-gray-500 mb-8">
          An unexpected error occurred while loading the page.
          Please try again or go back to the homepage.
        </p>

        {/* Buttons */}
        <div className="flex gap-4 justify-center flex-wrap">

          <Button
            onClick={() => reset()}
            className="flex items-center gap-2"
          >
            <RefreshCcw size={18} />
            Try Again
          </Button>

          <Button
            variant="outline"
            onClick={() => router.push("/")}
            className="flex items-center gap-2"
          >
            <Home size={18} />
            Go Home
          </Button>

        </div>

      </div>

    </div>
  )
}