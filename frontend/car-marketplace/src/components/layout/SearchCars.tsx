"use client"

import { Search } from 'lucide-react'
import { useRouter, useSearchParams } from 'next/navigation'
import React, { useState } from 'react'
import { Button } from '../ui/button'

export default function SearchCars() {

    const router = useRouter()
    const searchParams = useSearchParams()
    const [search, setSearch] = useState(searchParams.get("search") || "")

    const handleSearch = (e : React.FormEvent<HTMLFormElement>) => {
        e.preventDefault()

        const params = new URLSearchParams(searchParams.toString())

        if (search) {
            params.set("search", search)
            } else {
            params.delete("search")
            }

            params.set("page", "1")

            router.push(`/market-place?${params.toString()}`)
            }
  return  <>
  
        <form
    onSubmit={handleSearch}
    className="flex items-center w-full md:w-87.5 gap-3"
    >

    {/* Input Container */}
    <div className="relative flex-1">

        {/* Search Icon inside input */}
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 size-4" />

        <input
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search brand or model..."
        className="w-full pl-10 pr-4 py-2 border rounded-xl outline-none"
        />

    </div>

    {/* Search Button */}
    <Button type="submit" className="hidden lg:flex items-center justify-center px-4 py-2">
        <Search className="size-5" />
    </Button>

        </form>
  
  </>
}
