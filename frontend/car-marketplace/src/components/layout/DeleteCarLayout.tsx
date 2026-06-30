"use client"

import {Loader2, Trash } from "lucide-react" 
import { useRouter } from "next/navigation"
import { useState } from "react"
import { toast } from "react-hot-toast"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog"
import { deleteCar } from "@/actions/carsActions"

export default function DeleteCarLayout({ carId }: { carId: string }) {
  const router = useRouter()
  const [loading, setLoading] = useState(false)

  const handleDelete = async () => {
    try {
      setLoading(true)
        
      const res = await deleteCar(carId)
    
      if (res.success) {
        toast.success("Car deleted successfully")
        router.refresh()
      } else {
        toast.error(res.message || "Failed to delete")
      }

    } catch (error) {
      toast.error("Something went wrong")
    } finally {
      setLoading(false)
    }
  }

  return (
    <AlertDialog>
      
      {/* Trigger (icon) */}
      <AlertDialogTrigger asChild>
        <Trash
          className={`w-4 h-4 cursor-pointer ${
            loading
              ? "opacity-50 pointer-events-none"
              : "hover:text-red-500"
          }`}
        />
      </AlertDialogTrigger>

      {/* Modal */}
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            Delete this car?
          </AlertDialogTitle>

          <AlertDialogDescription>
            This action cannot be undone. This will permanently delete your car.
          </AlertDialogDescription>
        </AlertDialogHeader>

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading}>
            Cancel
          </AlertDialogCancel>

          <AlertDialogAction
            onClick={handleDelete}
            disabled={loading}
            className="bg-red-500 hover:bg-red-600"
          >
            {loading ? (
            <span className="flex items-center gap-2">
                <Loader2 className="animate-spin w-4 h-4" />
                Deleting...
            </span>
            ) : (
            "Delete"
            )}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>

    </AlertDialog>
  )
}