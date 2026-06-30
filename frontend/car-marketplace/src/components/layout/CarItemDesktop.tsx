import { Pencil } from "lucide-react"
import DeleteCarLayout from "./DeleteCarLayout"
import Link from "next/link"
import { CarResponse } from "@/Interface/CarInterface"
import UpdateCarDialog from "./UpdateCarDialog"

export default function CarItemDesktop({car} : {car : CarResponse}) {
  return (
    
    <tr className="border-b border-border text-sm md:text-base">
       
      <td className="py-4">
        <Link href={`/market-place/${car.id}`}>
    
        <div className="flex items-center gap-4">

          <img
            src={car.imageUrls?.[0] || "/placeholder.jpg"}
            className="w-16 h-12 md:w-20 md:h-16 rounded-lg object-cover"
          />

          <div>
            <p className="font-semibold">
              {car.brand} {car.model}
            </p>

            <p className="text-xs md:text-sm text-muted-foreground">
              {car.year} • {car.location}
            </p>
          </div>

        </div>

        </Link>

      </td>

      <td className="py-4 font-medium text-popover-foreground whitespace-nowrap">
        {car.price.toLocaleString("en-Us")} EGP
      </td>

      <td className="py-4">
        <span
          className={`px-2 md:px-3 py-1 text-xs md:text-sm rounded-full ${
            car.status === "Active"
              ? "bg-green-100 text-green-600"
              : "bg-gray-100 text-gray-500"
          }`}
        >
          {car.status}
        </span>
      </td>

      <td className="py-4">
        <div className="flex gap-3 text-gray-500 justify-end">

          <UpdateCarDialog car={car}/>

          <DeleteCarLayout carId={car.id} />

        </div>
      </td>

    </tr>
  )
}