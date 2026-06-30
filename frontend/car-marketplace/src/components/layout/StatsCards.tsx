import Link from "next/link"
import { Car, MessageCircle, Eye } from "lucide-react"
import { DashBoardResponse } from "@/Interface/DashBoardInterface"

export default function StatsCards({
  data,
}: {
  data: DashBoardResponse
}) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">

      {/*  Cars */}
      <Link href="/user/my-cars" className="block">
        <div className="
          relative rounded-2xl border border-gray-200 p-5 bg-background shadow-sm
          transition-all duration-200 hover:shadow-md hover:-translate-y-1 hover:bg-background/50 cursor-pointer
        ">
          <div className="absolute top-4 right-4 bg-blue-100 text-blue-600 p-2 rounded-lg">
            <Car size={18} />
          </div>

          <p className="text-sm text-secondary">
            My Listed Cars
          </p>

          <h2 className="text-3xl font-extrabold mt-2 text-secondary">
            {data.totalCarsOwned}
          </h2>
        </div>
      </Link>

      {/*  Messages */}
      <Link href="/user/messages" className="block">
        <div className="
          relative rounded-2xl border border-gray-200 p-5 bg-background shadow-sm
          transition-all duration-200 hover:shadow-md hover:-translate-y-1 hover:bg-background/50 cursor-pointer
        ">
          <div className="absolute top-4 right-4 bg-yellow-100 text-yellow-600 p-2 rounded-lg">
            <MessageCircle size={18} />
          </div>

          <p className="text-sm text-secondary">
            Unread Messages
          </p>

          <h2 className="text-3xl font-extrabold mt-2 text-secondary">
            {data.unreadMessagesCount}
          </h2>
        </div>
      </Link>

      {/* Views */}
      <Link href="/user/my-cars" className="block">
        <div className="
          relative rounded-2xl border border-gray-200 p-5 bg-background shadow-sm
          transition-all duration-200 hover:shadow-md hover:-translate-y-1 hover:bg-background/50 cursor-pointer
        ">
          <div className="absolute top-4 right-4 bg-green-100 text-green-600 p-2 rounded-lg">
            <Eye size={18} />
          </div>

          <p className="text-sm text-secondary">
            Total Views
          </p>

          <h2 className="text-3xl font-extrabold mt-2 text-secondary">
            {data.totalViewsAcrossListings}
          </h2>
        </div>
      </Link>

    </div>
  )
}