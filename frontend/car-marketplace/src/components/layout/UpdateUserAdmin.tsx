"use client"

import { useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button" 
import { useRouter } from "next/navigation"
import { Pencil, Loader2 } from "lucide-react"
import toast from "react-hot-toast"
import { useForm } from "react-hook-form"
import { CarResponse } from "@/Interface/CarInterface"
import {  updateUserAdmin } from "@/actions/adminActions"
import { AdminUsersResponse } from "@/Interface/AdminUsers"


type FormValues = {
  name: string
  email: string
  role: string
}

export default function UpdateUserAdmin({ user , onUserUpdated  }: { user: AdminUsersResponse , onUserUpdated: (user: any) => void }) {
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  const router = useRouter()

  const { register, handleSubmit } = useForm<FormValues>({
    defaultValues: {
      name: user.fullName,
      email:user.email,
      role: user.role,

    },
  })

  const onSubmit = async (data: FormValues) => {
    setLoading(true)

    const res = await updateUserAdmin ({
      ...user,
      ...data,
    })

    if (res.success) {
      
      toast.success("USER updated successfully")
      onUserUpdated({
    ...user,
    ...data,
  })
      setOpen(false)
    }

    setLoading(false)
  }

  return <>
  
    <Dialog open={open} onOpenChange={setOpen}>
     
      <DialogTrigger asChild>
        <button>
            <Pencil className="w-4 h-4 hover:text-primary cursor-pointer" />
        </button>
      </DialogTrigger>

      
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <DialogTitle className="text-xl font-semibold">
            Edit User Role 
          </DialogTitle>

          
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
                <label className="label">Name</label>
                <input {...register("name")} disabled placeholder="name" className="input col-span-2" />
            </div>
            <div>
                <label className="label">Email</label>
                <input {...register("email")} disabled placeholder="email" className="input" />
            </div>
            <div>
                <label className="label">Role</label>
                <input {...register("role", { required: true })} placeholder="role" className="input" />
            </div>
            

          </div>


          
          <Button type="submit" className="w-full bg-primary text-white flex justify-center">
            {loading ? (
              <span className="flex items-center gap-2">
                <Loader2 className="animate-spin w-4 h-4" />
                Updating...
              </span>
            ) : (
              "Update User"
            )}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  </>
}
