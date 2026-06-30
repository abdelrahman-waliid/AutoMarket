"use client"

import { CarsResponse } from "@/Interface/CarInterface"
import Slider from "../ReuseComponents/Slider"
import { formatCurrency } from "@/Helpers/formatCurrency"
import { MapPin } from "lucide-react"
import Link from "next/link"
import { Button } from "../ui/button"

export default function MarketPlace({ data }: { data: CarsResponse }) {

  return (

    
    
    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">

      {data?.items.map((car) => (
        
        <Link key={car.id} href={'/market-place/' + car.id}>
        <div 
          className="bg-card rounded-2xl border shadow-sm hover:shadow-md transition overflow-hidden group"
        >

          {/* Image */}
          <div className="relative">

            <Slider images={car.imageUrls} title={car.title} />

            {/* Year Badge */}
            <span className="absolute top-3 right-3 bg-popover text-secondary text-xs font-semibold px-3 py-1 rounded-full shadow">
              {car.year}
            </span>

          </div>

          {/* Content */}
          <div className="p-5 space-y-3">

            <h3 className="text-lg font-semibold">
              {car.brand} {car.model}
            </h3>

            <div className="flex items-center gap-1 text-sm text-gray-500">
              <MapPin size={14} />
              {car.location}
            </div>

            <p className="text-blue-600 font-bold text-xl">
              {formatCurrency(car.price)}
            </p>

            <p className="text-sm text-gray-500 line-clamp-2">
              {car.description}
            </p>

                <Button className="w-full bg-blue-500 hover:bg-blue-600 text-primary-foreground py-2 rounded-lg font-medium transition">  View Details  </Button> 

          </div>

        </div>
            </Link>

      ))}

    </div>

  )
}