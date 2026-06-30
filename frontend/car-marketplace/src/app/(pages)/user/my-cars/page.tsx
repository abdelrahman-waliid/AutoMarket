import { getMyCars } from "@/actions/carsActions"
import { authOptions } from "@/auth"
import CarsList from "@/components/layout/CarsList"
import CreateCarDialog from "@/components/layout/CreateCarDialog"
import Pagination from "@/components/layout/Pagination"
import ForceLogout from "@/components/ReuseComponents/ForceLogout" 
import { AlertTriangle } from "lucide-react"
import { getServerSession, Session } from "next-auth"
import Link from "next/link" 
 

export default async function MyCarsPage({ searchParams }: any) {

  const session : Session|null = await getServerSession(authOptions)
  const params = await searchParams  
  const page = Number(params.page) || 1

  const response = await getMyCars(page)

   

  // error UI 
if (!response.success) {
  if(response.unauthorized) {
    return <ForceLogout/>    // لو الايرور 401 ودا يعني unauthoraized فاهم
  }
 
  return (
    <div className="container mx-auto py-16 flex justify-center">

      <div className="bg-white border rounded-xl shadow-sm p-8 text-center max-w-md w-full">

        <AlertTriangle className="w-10 h-10 text-red-500 mx-auto mb-4" />

        <h2 className="text-lg font-semibold mb-2">
          Something went wrong
        </h2>

        <p className="text-gray-500 text-sm mb-6">
          {response.message}
        </p>

        <div className="flex justify-center gap-3">
          
          <Link
            href="/user/my-cars"
            className="px-4 py-2 bg-primary text-white rounded-lg"
          >
            Refresh
          </Link>

          <Link
            href="/"
            className="px-4 py-2 border rounded-lg"
          >
            Go Home
          </Link>

        </div>

      </div>

    </div>
  )
}

  const data = response.data

  return (
    <div className="container  mx-auto px-4 py-8">

      {/* HEADER */}
      <div className="flex flex-col md:flex-row md:justify-between md:items-center gap-4 mb-6">
        <div>
          <h1 className="text-xl md:text-2xl font-bold">My Cars</h1>
          <p className="text-muted-foreground text-sm">
            Manage your car listings.
          </p>
        </div>

         {session && <CreateCarDialog session={session}/>}
      </div>

      {/* LIST */}
      <CarsList items={data.items} totalCount={data.totalCount} />

      {/* PAGINATION */}
      <Pagination
        totalPages={data.totalPages}
        currentPage={data.pageNumber}
      />

    </div>
  )
}