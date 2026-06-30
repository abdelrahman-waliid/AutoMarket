"use client" 
import React, { useEffect, useState } from "react" 

import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
  type CarouselApi,
} from "@/components/ui/carousel"

  
export default function Slider({ images, title }: {images : string[] , title : string}) {
  const [api, setApi] = useState<CarouselApi>()
  const [current, setCurrent] = useState(0)
  const showNavigation = images?.length > 1
  useEffect(() => {
    if (!api) return

    setCurrent(api.selectedScrollSnap())

    api.on("select", () => {
      setCurrent(api.selectedScrollSnap())
    })
  }, [api])

  return (
    <div className="relative group w-full">

      <Carousel
        setApi={setApi}
        opts={{ loop: true }} >

        <CarouselContent>

          {images?.map((image, index) => (
            <CarouselItem key={index}>
              <div className="overflow-hidden rounded-xl">

                <img
                  src={image}
                  alt={title} 
                  className="w-full h-[350px] md:h-[420px] object-cover transition-transform duration-500 hover:scale-110"
                />

              </div>
            </CarouselItem>
          ))}

        </CarouselContent>

        {/* Arrows */}

        {showNavigation && (
          <>
            <CarouselPrevious className="opacity-0 group-hover:opacity-100 transition left-4 top-1/2 -translate-y-1/2 bg-white/80 backdrop-blur hover:bg-white" />

            <CarouselNext className="opacity-0 group-hover:opacity-100 transition right-4 top-1/2 -translate-y-1/2 bg-white/80 backdrop-blur hover:bg-white" />
          </>
        )}

      </Carousel>

      {/* Dots */}

      <div className="flex justify-center gap-2 mt-4">

        {images?.map((_, index) => (
          <button
            key={index}
            onClick={() => api?.scrollTo(index)}
            className={`h-2 rounded-full transition-all ${
              current === index
                ? "w-6 bg-primary"
                : "w-2 bg-muted-foreground"
            }`}
          />
        ))}

      </div>

    </div>
  )
}