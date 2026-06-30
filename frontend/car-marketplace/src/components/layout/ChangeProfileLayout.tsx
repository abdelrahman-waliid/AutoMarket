"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod" 
import { updateProfile } from "@/actions/profileActions"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { toast } from "react-hot-toast"
import { UpdatedData, UserProfileInterface } from "@/Interface/ProfileInterface" 
import { z } from "zod"

const profileSchema = z.object({
  fullName: z
    .string()
    .min(3, "Full name must be at least 3 characters"),

  email: z
    .string() 
})

export type ProfileFormData = UpdatedData

export default function ChangeProfileLayout({ profile }: {profile : UserProfileInterface}) {

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting , isDirty },
    reset
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      fullName: profile.fullName,
      email: profile.email,
    },
  })

  async function onSubmit(data: ProfileFormData) { 
    
    const res = await updateProfile(data)

    if (res.success) {
      toast.success("Profile updated successfully")
      reset(data)
    } else {
      toast.error(res.message + "")
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">

      {/* Inputs */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">

        {/* Full Name */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Full Name</label>

          <Input {...register("fullName")} />

          {errors.fullName && (
            <p className="text-red-500 text-xs">
              {errors.fullName.message}
            </p>
          )}
        </div>

        {/* Email */}
        <div className="space-y-2">
          <label className="text-sm font-medium">Email</label>

          <Input {...register("email")} />

          {errors.email && (
            <p className="text-red-500 text-xs">
              {errors.email.message}
            </p>
          )}
        </div>

      </div>

      {/* Role */}
      <div className="space-y-2">
        <label className="text-sm font-medium">Role</label>
        <div className="bg-primary p-2 rounded-2xl w-fit text-sm text-white font-bold">
          {profile.role}
        </div>
      </div>

      {/* Submit */}
      <div className="flex justify-end">
        <Button
          type="submit"
          disabled={isSubmitting || !isDirty}
          className="bg-blue-600 hover:bg-blue-700"
        >
          {isSubmitting ? "Saving..." : "Save Changes"}
        </Button>
      </div>

    </form>
  )
}