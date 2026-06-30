"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "react-hot-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { changePassword } from "@/actions/profileActions"
import { changedPasswordData } from "@/Interface/ProfileInterface"
import { useState } from "react"
import { signOut } from "next-auth/react"

type PasswordFormData = z.infer<typeof passwordSchema>

const passwordSchema = z.object({
  currentPassword: z.string().nonempty("Current password is required"),

  newPassword: z
    .string()
    .nonempty("Password Required")
    .regex(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,15}$/,
      "Password must be 8–15 chars, include uppercase, lowercase, number & special char"
    ),

    confirmPassword: z.string().nonempty("Please confirm your password"),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  
})

export default function ChangePasswordLayout() {

    const [formKey, setFormKey] = useState(0)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isValid}
  } = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
    mode : "onChange" ,
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmPassword: ""
    },
  })

  async function onSubmit(data: PasswordFormData) {

    const payload: changedPasswordData = {
      currentPassword: data.currentPassword,
      newPassword: data.newPassword,
    }

    const res = await changePassword(payload)

    if (res.success) {
      toast.success("Password changed successfully")
      reset({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      })
      setFormKey(prev => prev + 1)
      setTimeout(() => {
      signOut({
        callbackUrl: "/login", // 👈 يوديه لصفحة اللوجين
      })
    }, 1000)
    } else {
      toast.error(res.message + "") 
      
    }
  }

  return (
    <form key={formKey} onSubmit={handleSubmit(onSubmit)} className="space-y-4">

      <h2 className="text-lg font-semibold">Change Password</h2>

      {/* Current Password */}
      <div className="space-y-1">
        <Input
          type="password"
          placeholder="Current Password"
          {...register("currentPassword")}
        />
        {errors.currentPassword && (
          <p className="text-xs text-red-500">
            {errors.currentPassword.message}
          </p>
        )}
      </div>

      {/* New Password */}
      <div className="space-y-1">
        <Input
          type="password"
          placeholder="New Password"
          {...register("newPassword")}
        />
        {errors.newPassword && (
          <p className="text-xs text-red-500">
            {errors.newPassword.message}
          </p>
        )}
      </div>
      {/* Confirm Password */}
    <div className="space-y-1">
    <Input
        type="password"
        placeholder="Confirm New Password"
        {...register("confirmPassword")}
    />

    {errors.confirmPassword && (
        <p className="text-xs text-red-500">
        {errors.confirmPassword.message}
        </p>
    )}
    </div>

      {/* Submit */}
      <div className="flex justify-end">
        <Button
          type="submit"
          disabled={isSubmitting || !isValid}
          className="bg-blue-600 hover:bg-blue-700"
        >
          {isSubmitting ? "Updating..." : "Change Password"}
        </Button>
      </div>

    </form>
  )
}