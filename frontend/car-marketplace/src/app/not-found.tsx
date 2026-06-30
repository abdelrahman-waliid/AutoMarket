import Link from "next/link"
import { Car, ArrowLeft } from "lucide-react"

export default function NotFound() {

  return (

    <div className="min-h-[70vh] flex flex-col items-center justify-center text-center px-6">

      {/* Icon */}
      <div className="bg-blue-100 p-6 rounded-full mb-6">
        <Car className="size-10 text-blue-600"/>
      </div>

      {/* Title */}
      <h1 className="text-4xl font-bold mb-3">
        Page Not Found
      </h1>

      {/* Description */}
      <p className="text-gray-500 max-w-md mb-8">
        The page you're looking for doesn’t exist or may have been moved.
        Let's get you back to exploring cars.
      </p>

      {/* Button */}
      <Link
        href="/market-place"
        className="flex items-center gap-2 bg-blue-600 text-white px-6 py-3 rounded-xl hover:bg-blue-700 transition"
      >
        <ArrowLeft className="size-4"/>
        Back to Marketplace
      </Link>

    </div>

  )
}

