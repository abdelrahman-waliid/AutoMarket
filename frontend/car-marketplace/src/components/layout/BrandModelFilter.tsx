"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from "@/components/ui/command"

import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"

import { ChevronsUpDown, Check } from "lucide-react" 
import { carData } from "@/CarStaticData/CarStaticData"

interface Props {
  brand: string
  setBrand: (val: string) => void
  model: string
  setModel: (val: string) => void
}

export default function BrandModelFilter({
  brand,
  setBrand,
  model,
  setModel,
}: Props) {
  const [openBrand, setOpenBrand] = useState(false)
  const [openModel, setOpenModel] = useState(false)

  const brands = Object.keys(carData)
  const models = brand ? carData[brand] : []

  return (
    <div className="space-y-4">

      {/* BRAND */}
      <div>
        <p className="text-sm mb-2 font-medium">Brand</p>

        <Popover open={openBrand} onOpenChange={setOpenBrand}>
          <PopoverTrigger asChild>
            <Button variant="outline" className="w-full justify-between">
              {brand || "Select brand"}
              <ChevronsUpDown className="ml-2 h-4 w-4 opacity-50" />
            </Button>
          </PopoverTrigger>

          <PopoverContent align="start" side="bottom" className="w-[--radix-popover-trigger-width] p-0">
            <Command>
              <CommandInput autoFocus  placeholder="Search brand..." />
              <CommandEmpty>No brand found</CommandEmpty>

              <CommandGroup className="max-h-60 overflow-y-auto">
                {brands.map((b) => (
                  <CommandItem
                    key={b}
                    value={b.toLowerCase()}
                    keywords={[b.toLowerCase()]}
                    onSelect={() => {
                      setBrand(b)
                      setModel("")
                      setOpenBrand(false)
                    }}
                    className="cursor-pointer rounded-md px-2 py-1.5 hover:bg-sky-200!"
                  >
                    {b}
                    <Check
                      className={`ml-auto ${
                        brand === b ? "opacity-100" : "opacity-0"
                      }`}
                    />
                  </CommandItem>
                ))}
              </CommandGroup>
            </Command>
          </PopoverContent>
        </Popover>
      </div>

      {/* MODEL */}
      <div>
        <p className="text-sm mb-2 font-medium">Model</p>

        <Popover open={openModel} onOpenChange={setOpenModel}>
          <PopoverTrigger asChild>
            <Button
              variant="outline"
              className="w-full justify-between"
              disabled={!brand}
            >
              {model || "Select model"}
              <ChevronsUpDown className="ml-2 h-4 w-4 opacity-50" />
            </Button>
          </PopoverTrigger>

          <PopoverContent align="start" side="bottom" sideOffset={5} avoidCollisions={false} className="w-[--radix-popover-trigger-width] p-0">
            <Command>
              <CommandInput autoFocus  placeholder="Search model..." />
              <CommandEmpty>No model found</CommandEmpty>

              <CommandGroup className="max-h-60 overflow-y-auto">
                {models.map((m) => (
                  <CommandItem
                    key={m}
                    value={m.toLowerCase()}
                    keywords={[m.toLowerCase()]} 
                    onSelect={() => {
                      setModel(m)
                      setOpenModel(false)
                    }}
                    className="cursor-pointer rounded-md px-2 py-1.5 hover:bg-sky-200!"
                  >
                    {m}
                    <Check
                      className={`ml-auto ${
                        model === m ? "opacity-100" : "opacity-0"
                      }`}
                    />
                  </CommandItem>
                ))}
              </CommandGroup>
            </Command>
          </PopoverContent>
        </Popover>
      </div>

    </div>
  )
}