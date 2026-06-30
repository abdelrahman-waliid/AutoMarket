import { getDashboard } from "@/actions/dashboardActions"
import QuickActions from "@/components/layout/QuickActions"
import RecentActivity from "@/components/layout/RecentActivity" 
import ForceLogout from "@/components/ReuseComponents/ForceLogout"
import CreateCarDialog from "@/components/layout/CreateCarDialog"
import StatsCards from "@/components/layout/StatsCards"
import { getServerSession, Session } from "next-auth"
import { authOptions } from "@/auth"

export default async function DashboardPage() {
  const session : Session|null = await getServerSession(authOptions)
  const res = await getDashboard()

  if (!res.success || !res.data || res.unauthorized) {
      return <ForceLogout /> 
  }

  const data = res.data
  const UserName: string = res.userName

  return (
    <div className="space-y-6 container mx-auto pt-10 pb-6 px-4">
      
      {/*  Header */}
      <div className="flex flex-col gap-3 md:flex-row md:justify-between md:items-end">
        <div>
          <h1 className="text-3xl font-bold text-secondary">
            Dashboard
          </h1>
          <p className="text-sm text-gray-500 mt-1">
            Welcome back, {UserName}
          </p>
        </div>

        <div>
          {session && <CreateCarDialog session={session}/>}
        </div>
      </div>

      {/* Create Button */}

      {/* Stats */}
      <StatsCards data={data} />

      {/* Bottom Section */}
      <div className="flex flex-col gap-6 lg:grid lg:grid-cols-3">
        
        {/* Recent */}
        <div className="lg:col-span-2 order-1">
          <RecentActivity activities={data.recentActivity} />
        </div>

        {/* Quick */}
        <div className="order-2">
          <QuickActions />
        </div>

      </div>
    </div>
  )
}