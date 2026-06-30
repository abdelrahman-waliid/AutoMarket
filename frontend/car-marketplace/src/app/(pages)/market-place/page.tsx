import { getAllCars } from "@/actions/carsActions"
import Filters from "@/components/layout/Filters"
import MarketPlace from "@/components/layout/MarketPlace"
import SearchCars from "@/components/layout/SearchCars"
import EmptyCars from "@/components/ReuseComponents/EmptyCars"
import Pagination from "@/components/layout/Pagination"
import { Button } from "@/components/ui/button"
import { CarsResponse } from "@/Interface/CarInterface"
import { FilterIcon, Search } from "lucide-react"

export const dynamic = "force-dynamic";
export default async function MarketplacePage({searchParams} : {searchParams : any}) {

    const params = await searchParams 
    

     const data : CarsResponse= await getAllCars(
    {   page: params.page ?? 1,
        brand: params.brand ?? "",
        model : params.model ?? "" ,
        minPrice: params.minPrice ?? 0,
        maxPrice: params.maxPrice ?? 9000000000,
        minYear: params.minYear ?? 1800,
        maxYear: params.maxYear ?? new Date().getFullYear() ,
        search: params.search ?? ""
    }
) 
 

  return (

    <div className="container mx-auto px-6 py-10">

      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-10">

        <div>
          <h1 className="text-3xl font-bold">Explore Cars</h1>
          <p className="text-gray-500">
            Find the perfect car for your journey
          </p>
        </div>

        {/* Search */}
        <SearchCars/>

      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[260px_1fr] gap-8">

        {/* Filters */}
         <Filters/>

        {data.totalCount === 0 || !data.totalCount
        ? (<EmptyCars/>)

        : (<div className="space-y-10"> 
          {/* Cars Section */}
          <MarketPlace data={data}/>

          {/* Pagination */}
          <Pagination totalPages={data.totalPages} currentPage={data.pageNumber}/>

          </div>
          )}
         

      </div>

    </div>

  )
}