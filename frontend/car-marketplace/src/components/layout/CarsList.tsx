import { Car } from "lucide-react"  
import CreateCarDialog from "./CreateCarDialog"
import CarItemMobile from "./CarItemMobile"
import CarItemDesktop from "./CarItemDesktop"
import { CarResponse} from "@/Interface/CarInterface"
import { getServerSession, Session } from "next-auth"
import { authOptions } from "@/auth"

export default async function CarsList({ items, totalCount }:  {items : CarResponse[] , totalCount : number}) {
  const session : Session | null = await getServerSession(authOptions)
  const isEmpty = !items || items.length === 0

  return (
    <div className="bg-background rounded-xl border shadow-sm p-6">

      <h2 className="text-lg font-semibold mb-4">
        Listings ({totalCount})
      </h2>

      {isEmpty ? (
        <div className="flex flex-col items-center justify-center py-16 text-center text-gray-500">

          <Car className="w-12 h-12 mb-4 opacity-40" />

          <p className="text-lg font-medium mb-1">
            No cars yet
          </p>

          <p className="text-sm mb-4">
            You haven’t added any cars.
          </p>

          {session && <CreateCarDialog session ={session}/>}

        </div>
      ) : (
        <>
          {/* 📱 MOBILE (Cards) */}
          <div className="flex flex-col gap-4 md:hidden">
            {items.map((car: CarResponse) => (
              <CarItemMobile key={car.id} car={car} />
            ))}
          </div>

          {/* 💻 DESKTOP (Table) */}
          <table className="w-full hidden md:table">
            <tbody>
              {items.map((car: CarResponse) => (
                <CarItemDesktop key={car.id} car={car} />
              ))}
            </tbody>
          </table>
        </>
      )}

    </div>
  )
}