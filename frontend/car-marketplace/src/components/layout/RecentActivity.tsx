import Link from "next/link"
import { formatTimeAgo } from "@/Helpers/formatTimeAgo"
import { RecentActivityData } from "@/Interface/DashBoardInterface"
import { MessageCircle } from "lucide-react"

export default function RecentActivity({
  activities,
}: {
  activities: RecentActivityData[]
}) {

  const latestActivities = activities
    .slice()
    .sort(
      (a, b) =>
        new Date(b.occurredAt).getTime() -
        new Date(a.occurredAt).getTime()
    )
    .slice(0, 3)

  return (
    <div className="rounded-2xl border border-gray-200 p-5 bg-background shadow-sm">
      
      <h2 className="text-lg font-semibold mb-4">
        Recent Activity
      </h2>

      <div className="space-y-4">
        {latestActivities.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-10 text-center">
            
            <div className="bg-blue-100 text-blue-500 p-4 rounded-full mb-3">
              <MessageCircle size={22} />
            </div>

            <p className="text-sm font-semibold text-gray-800">
              No activity yet
            </p>

            <p className="text-xs text-gray-500 mt-1 max-w-50">
              When you start receiving messages or interactions, they will appear here.
            </p>

            <Link
              href="/market-place"
              className="mt-4 text-sm font-medium text-blue-600 hover:underline"
            >
              Explore marketplace
            </Link>

          </div>
        ) : (
          latestActivities.map((item, i) => (
            <Link key={i} href="/user/messages" className="block">
              <div className="
                flex items-center justify-between
                p-3 rounded-xl
                hover:bg-muted
                transition
                border-b
                border-border
                cursor-pointer
              ">

                <div className="flex items-center gap-3">
                  <div className="bg-blue-100 text-blue-600 p-2 rounded-lg">
                    <MessageCircle size={16} />
                  </div>

                  <div>
                    <p className="text-sm font-semibold text-popover-foreground">
                      New Message Activity
                    </p>

                    <p className="text-xs text-muted-foreground mt-1">
                      {item.description}
                    </p>
                  </div>
                </div>

                <span className="text-xs text-muted-foreground whitespace-nowrap">
                  {formatTimeAgo(item.occurredAt)}
                </span>

              </div>
            </Link>
          ))
        )}
      </div>

      {activities.length > 3 && (
        <div className="mt-4 text-center">
          <Link
            href="/user/messages"
            className="text-sm font-medium text-blue-600 hover:underline"
          >
            View more
          </Link>
        </div>
      )}

    </div>
  )
}