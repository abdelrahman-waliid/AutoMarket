"use client"
import React, { useState } from 'react'
import { Button } from '../ui/button'
import { FilterIcon } from 'lucide-react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Sheet, SheetContent, SheetTrigger } from '../ui/sheet'
import BrandModelFilter from './BrandModelFilter'

export default function Filters() {

    const [open, setOpen] = useState(false)

    const router = useRouter()
    const searchParams = useSearchParams()

    const [brand, setBrand] = useState(searchParams.get("brand") || "")
    const [model, setModel] = useState(searchParams.get("model") || "")
    const [minPrice, setMinPrice] = useState(searchParams.get("minPrice") || "")
    const [maxPrice, setMaxPrice] = useState(searchParams.get("maxPrice") || "")
    const [minYear, setMinYear] = useState(searchParams.get("minYear") || "")
    const [maxYear, setMaxYear] = useState(searchParams.get("maxYear") || "")

    const applyFilters = () => {
        const params = new URLSearchParams()

        if (brand) params.set("brand", brand) 
        if (model) params.set("model", model)
        if (minPrice) params.set("minPrice", minPrice)
        if (maxPrice) params.set("maxPrice", maxPrice) 
        if (minYear) params.set("minYear", minYear)
        if (maxYear) params.set("maxYear", maxYear)
        
        params.set("page" , "1")

        router.push(`/market-place?${params.toString()}`)
        router.refresh()
    }


    const resetFilters = () => { 
        setBrand("")
        setMinPrice("")
        setMaxPrice("")
        setMinYear("")
        setMaxYear("")

        router.push("/market-place")

  }


  return  <>
  {/* Desktop */}
    <div className='hidden lg:block sticky top-4 max-h-[calc(100vh-3rem)] overflow-y-auto'>
        
    
        <div className="border rounded-xl p-6 h-fit space-y-6">
          <div className="flex gap-2  items-center">
            <FilterIcon className="text-primary"/>
            <h2 className="font-bold text-lg">Filters</h2>
          </div>

          {/* Brand */}
          <div> 

            <BrandModelFilter
                brand={brand}
                setBrand={setBrand}
                model={model}
                setModel={setModel}
            />

          </div>

          {/* Price */}
          <div>

            <p className="text-sm mb-2 font-medium">Price Range</p>

            <div className="flex gap-2">

                <input
                    type="number"
                    value={minPrice}
                    onChange={(e)=>setMinPrice(e.target.value)}
                    placeholder="Min"
                    className="border rounded-lg p-2 w-full"
                />

                <input
                    type="number"
                    value={maxPrice}
                    onChange={(e)=>setMaxPrice(e.target.value)}
                    placeholder="Max"
                    className="border rounded-lg p-2 w-full"
                />

            </div>

          </div>

          {/* Year */}
          <div>

            <p className="text-sm mb-2 font-medium">Year Range</p>

            <div className="flex gap-2">

                <input
                    type="number"
                    value={minYear}
                    onChange={(e)=>setMinYear(e.target.value)}
                    placeholder="From"
                    className="border rounded-lg p-2 w-full"
                />

                <input
                    type="number"
                    value={maxYear}
                    onChange={(e)=>setMaxYear(e.target.value)}
                    placeholder="To"
                    className="border rounded-lg p-2 w-full"
                />

            </div>

          </div>

          <Button 
            onClick={applyFilters}
            className="w-full text-primary-foreground py-2 rounded-lg">
            Apply Filters
          </Button>

          <Button 
            onClick={resetFilters}
            className="w-full border py-2 rounded-lg bg-muted-foreground text-primary-foreground hover:bg-muted-foreground/90">
            Reset
          </Button>

        </div>

    </div>


  {/* Mobile */}
    <div className="lg:hidden">

    <Sheet open={open} onOpenChange={setOpen}>

        <SheetTrigger asChild>

        <Button variant="outline" size="icon">
            <FilterIcon className="size-5 text-primary"/>
        </Button>

        </SheetTrigger>

        <SheetContent side="left" className="w-[320px] overflow-y-auto">

        <div className="space-y-6 mt-6 px-4">

            <div className="flex gap-2 items-center">
            <FilterIcon className="text-primary"/>
            <h2 className="font-bold text-lg">Filters</h2>
            </div>

            {/* Brand */}
            <div> 

            {/* <input
                value={brand}
                onChange={(e)=>setBrand(e.target.value)}
                placeholder="BMW, Audi..."
                className="w-full border rounded-lg p-2"
            /> */}

            <BrandModelFilter
                brand={brand}
                setBrand={setBrand}
                model={model}
                setModel={setModel}
            />

            </div>

            {/* Price */}
            <div>

            <p className="text-sm mb-2 font-medium">Price Range</p>

            <div className="flex gap-2">

                <input
                type="number"
                value={minPrice}
                onChange={(e)=>setMinPrice(e.target.value)}
                placeholder="Min"
                className="border rounded-lg p-2 w-full"
                />

                <input
                type="number"
                value={maxPrice}
                onChange={(e)=>setMaxPrice(e.target.value)}
                placeholder="Max"
                className="border rounded-lg p-2 w-full"
                />

            </div>

            </div>

            {/* Year */}
            <div>

            <p className="text-sm mb-2 font-medium">Year Range</p>

            <div className="flex gap-2">

                <input
                type="number"
                value={minYear}
                onChange={(e)=>setMinYear(e.target.value)}
                placeholder="From"
                className="border rounded-lg p-2 w-full"
                />

                <input
                type="number"
                value={maxYear}
                onChange={(e)=>setMaxYear(e.target.value)}
                placeholder="To"
                className="border rounded-lg p-2 w-full"
                />

            </div>

            </div>

            <Button 
            onClick={()=>{
                applyFilters()
                setOpen(false)
            }}
            className="w-full text-primary-foreground py-2 rounded-lg">
            Apply Filters
            </Button>

            <Button 
            onClick={()=>{
                resetFilters()
                setOpen(false)
            }}
            className="w-full border py-2 rounded-lg bg-muted-foreground text-primary-foreground hover:bg-muted-foreground/90">
            Reset
            </Button>

        </div>

        </SheetContent>

    </Sheet>

    </div>



  </>
}
