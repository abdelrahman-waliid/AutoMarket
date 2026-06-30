import { getCarById } from '@/actions/carsActions'
import { authOptions } from '@/auth'
import BackButton from '@/components/ReuseComponents/BackButton'
import Slider from '@/components/ReuseComponents/Slider'
import { Button } from '@/components/ui/button'
import { formatCurrency } from '@/Helpers/formatCurrency'    
import { formatTimeAgoMinus3Hours } from '@/Helpers/formatTimeAgoMinus'
import { CarResponse } from '@/Interface/CarInterface'
import {Calendar, Clock, Gauge, MapPin, MessageCircleIcon, ShieldCheck, User } from 'lucide-react'
import { getServerSession } from 'next-auth'
import Image from 'next/image'
import Link from 'next/link' 

export default async function CarDetails({params} : {params : {carId : string}}) {
   const {carId} = await params
   const car : CarResponse = await getCarById(carId)
   let avatar : string | null = car.ownerAvatarUrl 
   if(avatar === ""){
    avatar = null
   }
   
   if(!car){
     return <div className='text-center py-20'>Car Not Found</div>
    }
    
    const session = await getServerSession(authOptions)
    const isOwner = car.ownerId === session?.user.id
   
  return  <>
    
    <div className="container bg-background mx-auto px-6 py-10 space-y-8">

      {/* Back */}
       <BackButton/>

      <div className="grid lg:grid-cols-[2fr_1fr] gap-8 mt-5">

        {/* LEFT */}
        <div className="space-y-6">

          {/* Image Slider */}
          <Slider images={car.imageUrls} title={car.title} />

          {/* Title + Price */}
          <div className="flex items-center justify-between">

            <div>
              <h1 className="text-2xl font-bold">
                {car.brand} {car.model}
              </h1>

              <div className="flex items-center gap-3 text-gray-500 text-sm mt-1 flex-wrap">
                <div className="flex items-center gap-1.5">
                  <MapPin size={14} />
                  <span>{car.location}</span>
                </div>

                <span className="text-gray-300">•</span>

                <div className="flex items-center gap-1.5">
                  <Clock size={14} />
                  <span>{formatTimeAgoMinus3Hours(car.createdAt)}</span>
                </div>
              </div>
            </div>

            <p className="text-blue-600 font-bold text-2xl">
              {formatCurrency(car.price)}
            </p>

          </div>

          {/* Info */}
          <div className="grid grid-cols-3 text-center border-y py-6">

            <div className='flex flex-col items-center justify-center p-3'>
              <Calendar className="h-5 w-5 text-primary mb-1"/>
              <p className="font-semibold">{car.year}</p>
              <p className="text-sm text-gray-500">Year</p>
            </div>

            <div className='flex flex-col items-center justify-center p-3'>
              <Gauge className="h-5 w-5 text-primary mb-1"/>
              <p className="font-semibold">{car.transmissionType}</p>
              <p className="text-sm text-gray-500">Transmission</p>
            </div>

            <div className='flex flex-col items-center justify-center p-3'>
              <ShieldCheck className="h-5 w-5 text-primary mb-1"/>
              <p className="font-semibold">{"Verified"}</p>
              <p className="text-sm text-gray-500">Condition</p>
            </div>

          </div>

          {/* Description */}
          <div>
            <h2 className="font-semibold text-lg mb-2">Description</h2>
            <p className="text-muted-foreground leading-relaxed">
              {car.description}
            </p>
          </div>

          {/* Features */}
          <div>
            <h2 className="font-semibold text-lg mb-2">Features</h2>

            <div className="flex flex-wrap gap-3">
               
                <span className="bg-popover text-sm px-3 py-1 rounded-full border border-primary" >
                   Bluetooth
                </span>
                <span className="bg-popover text-secondary text-sm px-3 py-1 rounded-full border border-primary" >
                   Navigation
                </span>
                <span className="bg-popover text-secondary text-sm px-3 py-1 rounded-full border border-primary" >
                   Leather Seats
                </span>
                <span className="bg-popover text-secondary text-sm px-3 py-1 rounded-full border border-primary" >
                   Sunroof
                </span>
                <span className="bg-popover text-secondary text-sm px-3 py-1 rounded-full border border-primary" >
                   Backup Camera
                </span>
                <span className="bg-popover text-secondary text-sm px-3 py-1 rounded-full border border-primary" >
                   Heated Seats
                </span>
               
            </div>
          </div>

        </div>

        {/* RIGHT (Seller Card) */} 
        <div className="bg-card border rounded-2xl p-5 shadow-sm space-y-4 h-fit sticky top-20">

          <div className="flex items-center gap-3">
            <div className="relative w-12 h-12 rounded-full overflow-hidden bg-gray-200 border border-gray-300 shadow-sm flex items-center justify-center">
              {avatar ? 
                <Image
                  src={car.ownerAvatarUrl}
                  alt={car.brand}
                  fill
                  className="object-cover"
                  unoptimized
                />
              :
              <User className="w-4 h-4 text-gray-500" />
            
              }
            </div>

            <div>
              <p className="font-semibold">{car.ownerFullName}</p>
              <div className="flex items-center gap-1.5">
                <Clock size={14} className='text-gray-500'/>
                <span className="text-sm text-gray-500">
                  {formatTimeAgoMinus3Hours(car.createdAt)}
                </span>
              </div>
            </div>
          </div>


          {isOwner ? (
            <div className="flex gap-2">
              <Link href={`/user/my-cars`} className='w-full'>
              
                <Button className="w-full py-2 rounded-lg flex justify-center items-center gap-2">
                  View your listing
                </Button> 

              </Link> 
            </div>
          ) : (
          <Link href={`/user/messages?userId=${car.ownerId}&name=${car.ownerFullName}&avatar=${car.ownerAvatarUrl}`}>
          <Button className="w-full py-2 rounded-lg flex justify-center items-center gap-2">
              <MessageCircleIcon />
              <span>Message Seller</span>
            </Button>
          </Link> 
          )}
    

          {/* <div className="flex gap-3">
            <button className="flex-1 border py-2 rounded-lg hover:bg-gray-50">
              Save
            </button>
            <button className="flex-1 border py-2 rounded-lg hover:bg-gray-50">
              Share
            </button>
          </div> */}

          {/* <p className="text-xs text-gray-400 text-center">
            Safety tip: Avoid transferring money before seeing the vehicle.
          </p> */}

        </div>

      </div>
    </div>
  
  </>
}
