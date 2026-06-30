"use client"

import { useRouter, useSearchParams } from "next/navigation"
import { Button } from "../ui/button"

 

export default function Pagination({totalPages , currentPage} : {totalPages : number , currentPage : number}){

  const router = useRouter()
  const searchParams = useSearchParams()

  const changePage = (page : number) => {
    const params = new URLSearchParams(searchParams.toString())
    params.set("page" , page.toString())
    router.push(`?${params.toString()}`)
  }
  
  return (
    <div className="flex justify-center mt-10 gap-2">

      {/* Prev */}
      <Button
        onClick={() => changePage(currentPage - 1)}
        disabled={currentPage === 1}
        className={`px-4 py-2 border rounded-lg bg-secondary-foreground text-primary hover:bg-secondary-foreground/40  disabled:opacity-50 ${!totalPages  ? "hidden" : ""}`}
      >
        Prev
      </Button>

      {/* Page Numbers */}
      {Array.from({ length: totalPages }, (_, index) => {
        const page = index + 1

        return (
          <Button
            key={page}
            onClick={() => changePage(page)}
            className={`px-4 py-2 border rounded-lg bg-secondary-foreground text-secondary hover:bg-secondary-foreground/40 ${
              currentPage === page
                ? "border-primary text-primary"
                : ""
            }`}
          >
            {page}
          </Button>
        )
      })}

      {/* Next */}
      <Button
        onClick={() => changePage(currentPage + 1)}
        disabled={currentPage === totalPages}
        className={`px-4 py-2 border rounded-lg bg-secondary-foreground text-primary hover:bg-secondary-foreground/40 disabled:opacity-50 ${!totalPages ? "hidden" : ""}`}
      >
        Next
      </Button>

    </div>
  )
}