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
import { deleteCarList} from "@/actions/adminActions"

export default function DeleteListCarLayout({ listId , onDeleted }: { listId: string , onDeleted: (id: string) => void}) {
  const router = useRouter()
  const [loading, setLoading] = useState(false)

  const handleDelete = async () => {
    try {
      setLoading(true)
        
      const res = await deleteCarList(listId)
    
      if (res.success) {
        toast.success("car deleted successfully")
        onDeleted(listId)
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
      
      
      <AlertDialogTrigger asChild>
        <Trash
          className={`w-4 h-4 cursor-pointer text-red-500 ${
            loading
              ? "opacity-50 pointer-events-none"
              : "hover:text-red-800"
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