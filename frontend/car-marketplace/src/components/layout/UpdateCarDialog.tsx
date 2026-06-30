"use client"

import { useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button" 
import { useRouter } from "next/navigation"
import { Pencil, Loader2 } from "lucide-react"
import toast from "react-hot-toast"
import { useForm } from "react-hook-form"
import { CarResponse } from "@/Interface/CarInterface"
import { updateCar } from "@/actions/carsActions"

type FormValues = {
  title: string
  brand: string
  model: string
  price: number
  year: number
  location: string
  mileage: number
  fuelType: string
  transmissionType: string
  description: string 
}

export default function UpdateCarDialog({ car }: { car: CarResponse }) {
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  const router = useRouter()

  const { register, handleSubmit } = useForm<FormValues>({
    defaultValues: {
      title: car.title,
      brand: car.brand,
      model: car.model,
      price: car.price,
      year: car.year,
      location: car.location,
      mileage: car.mileage,
      fuelType: car.fuelType,
      transmissionType: car.transmissionType,
      description: car.description
    },
  })

  const onSubmit = async (data: FormValues) => {
    setLoading(true)

    const res = await updateCar ({
      ...car,
      ...data,
    })

    if (res.success) {
      toast.success("Car updated successfully")
      setOpen(false)
      router.refresh()
    }

    setLoading(false)
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      {/* BUTTON */}
      <DialogTrigger asChild>
        <button>
            <Pencil className="w-4 h-4 hover:text-primary cursor-pointer" />
        </button>
      </DialogTrigger>

      {/* MODAL */}
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <DialogTitle className="text-xl font-semibold">
            Edit Car
          </DialogTitle>

         
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
                <label className="label">Title</label>
                <input {...register("title", { required: true })} placeholder="Title" className="input col-span-2" />
            </div>
            <div>
                <label className="label">Brand</label>
                <input {...register("brand", { required: true })} placeholder="Brand" className="input" />
            </div>
            <div>
                <label className="label">Model</label>
                <input {...register("model", { required: true })} placeholder="Model" className="input" />
            </div>
            <div>
                <label className="label">Price</label>
                <input {...register("price", { required: true })} type="number" placeholder="Price" className="input" />
            </div>
            <div>
                <label className="label">Year</label>
                <input {...register("year", { required: true })} type="number" placeholder="Year" className="input" />
            </div>
            <div>
                <label className="label">Location</label>
                <input {...register("location", { required: true })} placeholder="Location" className="input col-span-2" />
            </div>
            <div>
                <label className="label">Mileage</label>
                <input {...register("mileage", { required: true })} type="number" placeholder="Mileage" className="input" />
            </div>
            <div>
                <label className="label">Fuel Type</label>
                <select {...register("fuelType", { required: true })} className="input">
                <option value="Gasoline" className="option">Gasoline</option>
                <option value="Diesel" className="option">Diesel</option>
                <option value="Electric" className="option">Electric</option>
                <option value="Hybrid" className="option">Hybrid</option>
                <option value="PlugInHybrid" className="option">Plug-In Hybrid</option>
                <option value="CNG" className="option">CNG</option>
                </select>
            </div>
            <div>
                <label className="label">Transmission</label>
                <select {...register("transmissionType", { required: true })} className="input">
                <option value="Automatic" className="option">Automatic</option>
                <option value="Manual" className="option">Manual</option>
                <option value="CVT" className="option">CVT</option>
                <option value="SemiAutomatic" className="option">Semi Automatic</option>
                </select>
            </div>

            <div className="col-span-2">
                <label className="label">Description</label>
                <textarea
                {...register("description", { required: true })}
                placeholder="Description"
                className="input col-span-2 h-24"
                /> 
            </div>
          </div>

        
          <div>
            <label className="label mb-2">Car Images</label>

            <div className="grid grid-cols-3 gap-3">
              {car.imageUrls?.map((img, index) => (
                <img
                  key={index}
                  src={img}
                  className="w-full h-28 object-cover rounded-lg border"
                />
              ))}
            </div>

            <p className="text-xs text-gray-400 mt-2">
              Images cannot be edited yet
            </p>
          </div>

        
          <Button type="submit" className="w-full bg-primary text-white flex justify-center" disabled={loading}>
            {loading ? (
              <span className="flex items-center gap-2">
                <Loader2 className="animate-spin w-4 h-4" />
                Updating...
              </span>
            ) : (
              "Update Car"
            )}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
