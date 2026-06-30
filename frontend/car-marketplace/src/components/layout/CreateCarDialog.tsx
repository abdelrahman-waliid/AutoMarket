"use client"

import { useEffect, useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { CreateNewCar } from "@/actions/carsActions"
import { useRouter } from "next/navigation"
import { BrainCogIcon, BrainIcon, Car, ChartLine, Loader2, Upload, X } from "lucide-react"
import toast from "react-hot-toast"
import { useForm } from "react-hook-form"
import { Session } from "next-auth"
import { AiResponse } from "@/Interface/AiResponseInterface"

type FormValues = {
  Title: string
  Brand: string
  Model: string
  Price: number
  Year: number
  Location: string
  Mileage: number
  FuelType: string
  TransmissionType: string
  Description: string
}

export default function CreateCarDialog({session} : {session : Session}) {
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [images, setImages] = useState<File[]>([])

  //AI states
  const [aiData, setAiData] = useState<AiResponse| null>(null)  // e3mly el any de 8ayarha
  const [aiLoading, setAiLoading] = useState(false)

  const router = useRouter()

  const { register, handleSubmit, reset, watch, setValue} = useForm<FormValues>()

  const brand = watch("Brand")
  const model = watch("Model")
  const year = watch("Year")
  const mileage = watch("Mileage")
  const fuelType = watch("FuelType")
  const transmission = watch("TransmissionType")
  const location = watch("Location")
  const price = watch("Price")

  
  //flag for Button 
  let isUsingAISuggestion
  if(aiData) {
    isUsingAISuggestion =  Math.round(price) === Math.round(aiData?.estimatedPrice)
  }

  useEffect(() => {
  if (!brand || !model || !year || !mileage || !fuelType || !transmission || !location) return

  const timeout = setTimeout(async () => {
    try {
      setAiLoading(true)

      const res = await fetch("http://localhost:5127/api/AI/price-estimate", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${session.token}`, 
        },
        body: JSON.stringify({
          brand,
          model,
          year: Number(year),
          mileage: Number(mileage),
          condition: "used",
          transmission,
          fuelType,
          location,
          userPrice: price ? Number(price) : undefined,
        }),
      })

      const data : AiResponse = await res.json()
      setAiData(data)
    } catch (err) {
      console.log(err)
    } finally {
      setAiLoading(false)
    }
  }, 500)

  return () => clearTimeout(timeout)
}, [brand, model, year, mileage, fuelType, transmission, location, price])



  // نضيف صورة
  const handleImages = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return

    const files: File[] = Array.from(e.target.files || [])
    setImages((prev) => [...prev, ...files])
  }

  // نمسح صورة
  const removeImage = (index: number) => {
    setImages((prev) => prev.filter((_, i) => i !== index))
  }

  // submit بـ RHF
  const onSubmit = async (data: FormValues) => {
    setLoading(true)

    const formData = new FormData()

    Object.entries(data).forEach(([key, value]) => {
      formData.append(key, String(value))
    })

    images.forEach((img) => {
      formData.append("Images", img)
    })

    const res = await CreateNewCar(formData)
    console.log(res)

    if (res.success) {
      setOpen(false)
      setImages([])
      reset()
      toast.success("Car Added Sucessfully")
      router.refresh()
    }

    setLoading(false)
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      {/* BUTTON */}
      <DialogTrigger asChild>
        <Button className="bg-primary text-white">
          + Add your car 
        </Button>
      </DialogTrigger>

      {/* MODAL */}
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <DialogTitle className="flex gap-2 text-xl font-semibold">
            List a Car <Car className="text-primary" />
          </DialogTitle>

          {/* BASIC INFO */}
          <div className="grid grid-cols-2 gap-4">
            <input {...register("Title", { required: true })} placeholder="Title" className="input col-span-2" />

            <input {...register("Brand", { required: true })} placeholder="Brand" className="input" />
            <input {...register("Model", { required: true })} placeholder="Model" className="input" /> 
            <input {...register("Year", { required: true })} type="number" placeholder="Year" className="input" /> 
            <input {...register("Location", { required: true })} placeholder="Location" className="input" /> 
            <input {...register("Mileage", { required: true })} type="number" placeholder="Mileage" className="input" /> 
            <select {...register("FuelType", { required: true })} className="input cursor-pointer" defaultValue="">
              <option value="" disabled className="option">
                Select Fuel
              </option>
              <option value="Gasoline" className="option">Gasoline</option>
              <option value="Diesel" className="option">Diesel</option>
              <option value="Electric" className="option">Electric</option>
              <option value="Hybrid" className="option">Hybrid</option>
              <option value="PlugInHybrid" className="option">Plug-In Hybrid</option>
              <option value="CNG" className="option">CNG</option>
            </select> 
            <select {...register("TransmissionType", { required: true })} className="input cursor-pointer" defaultValue="">
              <option value="" disabled className="option">
                Select Transmission
              </option>
              <option value="Automatic" className="option">Automatic</option>
              <option value="Manual" className="option">Manual</option>
              <option value="CVT" className="option">CVT</option>
              <option value="SemiAutomatic" className="option">Semi Automatic</option>
            </select> 
            <input {...register("Price", { required: true })} type="number" placeholder="Price" className="input" />

            {/* AI Ui */}

            {aiLoading && (
              <p className="text-sm text-gray-500 col-span-2 animate-ping">Analyzing price...... </p>
            )}

            {aiData && (
              <div className="col-span-2 mt-4 p-5 rounded-2xl border border-white/10 bg-zinc-900/90 shadow-lg shadow-black/30 space-y-4">
  
                  {/* Header */}
                  <div className="flex items-center gap-2">
                    <span className="text-yellow-400 text-lg"> <ChartLine/> </span>
                    <h3 className="text-white font-semibold text-sm">
                      Price Analysis
                    </h3>
                  </div>

                  {/* Range */}
                    <div className="text-center space-y-2">
                      
                      <p className="text-xs text-gray-400 uppercase tracking-wider">
                        Estimated Market Range
                      </p>

                      <div className="text-lg font-bold">
                        <span className="text-emerald-400">
                          {Math.round(aiData.minPrice / 1000).toLocaleString()}K
                        </span>

                        <span className="text-gray-400 mx-2">—</span>

                        <span className="text-emerald-400">
                          {Math.round(aiData.maxPrice / 1000).toLocaleString()}K
                        </span>

                        <span className="text-gray-300 text-sm ml-2">
                          EGP
                        </span>
                      </div>

                      {/* Suggested Price */}
                      <div className="mt-3 p-3 rounded-xl bg-white/5 border border-white/10">
                        <p className="text-xs text-gray-400 uppercase">
                          Our Suggested Price
                        </p>

                        <p className="text-emerald-400 font-bold text-lg">
                          {Math.round(aiData.estimatedPrice).toLocaleString()} EGP
                        </p>

                        <p className="text-xs text-gray-500">
                          Based on your car details & market trends
                        </p>
                      </div>
                    </div>
 
                  {/* Status */}
                  {aiData.priceStatus && (
                    <div
                      className={`text-sm font-medium p-2 rounded-lg ${
                        aiData.priceStatus === "low"
                          ? "bg-yellow-500/10 text-yellow-400"
                          : aiData.priceStatus === "high"
                          ? "bg-red-500/10 text-red-400"
                          : "bg-green-500/10 text-green-400"
                      }`}
                    >
                      {aiData.priceStatus === "low" && aiData.percentageDifference &&
                        `⚠ Price is lower than market (${Math.abs(aiData.percentageDifference)}%)`}
                      {aiData.priceStatus === "high" &&
                        `⚠ Price is higher than market`}
                      {aiData.priceStatus === "normal" &&
                        `✔ Price looks good`}
                    </div>
                  )}

                  {/* Button */}
                  <button
                    type="button"
                    disabled={isUsingAISuggestion}
                    onClick={() =>
                      setValue("Price", Math.round(aiData.estimatedPrice), {
                        shouldDirty: true,
                        shouldValidate: true,
                      })
                    }
                    className={`p-2 rounded-xl text-sm font-medium transition w-full
                      ${
                        isUsingAISuggestion
                          ? "bg-emerald-700/40 text-emerald-200 cursor-not-allowed"
                          : "bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer"
                      }
                    `}
                  >
                    {isUsingAISuggestion ? "Applied Suggested Price" : "Use Suggested Price"}
                  </button>
              </div>
            )}

            <textarea
              {...register("Description", { required: true })}
              placeholder="Description"
              className="input col-span-2 h-24"
            />
          </div>

          {/* IMAGE UPLOAD */}
          <div>
            <label className="label mb-2">Car Images</label>

            <label className="flex flex-col items-center justify-center w-full p-6 border-2 border-dashed border-gray-300 rounded-xl cursor-pointer hover:bg-card hover:border-primary transition">
              <Upload className="w-6 h-6 text-gray-400 mb-2" />

              <p className="text-sm font-medium text-gray-700">Upload car images</p>

              <p className="text-xs text-gray-400">Click to upload (multiple allowed)</p>

              <input
                type="file"
                multiple
                accept="image/*"
                className="hidden"
                onChange={handleImages}
              />
            </label>

            {/* PREVIEW */}
            {images.length > 0 && (
              <div className="grid grid-cols-3 gap-3 mt-4">
                {images.map((img, index) => (
                  <div key={index} className="relative group">
                    <img
                      src={URL.createObjectURL(img)}
                      className="w-full h-28 object-cover rounded-lg border"
                    />

                    <button
                      type="button"
                      onClick={() => removeImage(index)}
                      className="absolute top-1 right-1 bg-black/60 text-white p-1 rounded-full opacity-0 group-hover:opacity-100 transition"
                    >
                      <X size={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* SUBMIT */}
          <Button type="submit" className="w-full bg-primary text-white flex justify-center">
            {loading ? (
              <span className="flex items-center gap-2">
                <Loader2 className="animate-spin w-4 h-4" />
                Listing...
              </span>
            ) : (
              "List Car"
            )}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
