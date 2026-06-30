import { Pencil } from "lucide-react"
import DeleteCarLayout from "./DeleteCarLayout"
import Link from "next/link"
import { CarResponse } from "@/Interface/CarInterface"
import UpdateCarDialog from "./UpdateCarDialog"

export default function CarItemMobile({ car }:  {car : CarResponse}) {
  return (
    <div className="border rounded-xl p-4 flex flex-col gap-3 shadow-sm">
        <Link href={`/market-place/${car.id}`}>
        
      {/* IMAGE */}
      <img
        src={car.imageUrls?.[0] || "/placeholder.jpg"}
        className="w-full h-40 object-cover rounded-lg"
        />

      {/* INFO */}
      <div>
        <p className="font-semibold">
          {car.brand} {car.model}
        </p>

        <p className="text-sm text-gray-500">
          {car.year} • {car.location}
        </p>
      </div>

        </Link>

      {/* PRICE + STATUS */}
      <div className="flex justify-between items-center">
        <p className="font-medium text-popover-foreground">
          {car.price.toLocaleString()} EGP
        </p>

        <span
          className={`px-3 py-1 text-xs rounded-full ${
            car.status === "Active"
              ? "bg-green-100 text-green-600"
              : "bg-gray-100 text-gray-500"
          }`}
        >
          {car.status}
        </span>
      </div>

      {/* ACTIONS */}
      <div className="flex justify-end gap-3 text-gray-500">

        <UpdateCarDialog car={car}/>

        <DeleteCarLayout carId={car.id} />

      </div>

    </div>
  )
}