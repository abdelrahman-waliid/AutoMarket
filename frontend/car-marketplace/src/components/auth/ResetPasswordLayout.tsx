"use client"

import { useSearchParams, useRouter } from "next/navigation"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useState } from "react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Card, CardContent, CardHeader, CardTitle, CardDescription
} from "@/components/ui/card"
import toast from "react-hot-toast"
import { Loader2 } from "lucide-react"

const schema = z
  .object({
    password: z
      .string()
      .min(8, "Min 8 characters")
      .regex(/[A-Z]/, "Must include capital letter")
      .regex(/[0-9]/, "Must include number")
      .regex(/[@$!%*?&]/, "Must include symbol"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })

type FormData = z.infer<typeof schema>

export default function ResetPasswordLayout() {

  const params = useSearchParams()
  const token = params.get("token")
  const router = useRouter()

  const [success, setSuccess] = useState(false) 

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting, isValid },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

// لو مفيش token
  if (!token) {
    return (
      <Card className="max-w-md w-full text-center p-6">
        <h2 className="text-xl font-bold">Invalid Link</h2>
        <p className="text-gray-500 text-sm">
          This reset link is invalid or expired.
        </p>
      </Card>
    )
  }

  const password = watch("password")

  async function onSubmit(data: FormData) {  
    try {
      const res = await fetch("http://localhost:5127/api/Auth/reset-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          token,
          newPassword: data.password,
        }),
      })
  
      if (res.ok) {
        toast.success("Password updated successfully")
        setSuccess(true)
  
        setTimeout(() => {
          router.push("/login")
        }, 2000)
      }else{
        toast.error("Something went wrong. Try again.")
      }
      
    } catch (error) {
      toast.error("Network error. Please try again.")
    }

  }

  if (success) {
    return (
      <Card className="max-w-md w-full text-center p-6">
        <h2 className="text-xl font-bold">Password Updated</h2>
        <div className="text-gray-500 text-sm flex items-center justify-center gap-2">
          <span>Redirecting</span> <Loader2 className="animate-spin"/>
        </div>
      </Card>
    )
  }

  return (
    <Card className="w-full max-w-md rounded-2xl">
      <CardHeader className="text-center">
        <CardTitle>Reset Password</CardTitle> 
      </CardHeader>

      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

          <div>
            <Input type="password" placeholder="New Password" {...register("password")} /> 
            {errors.password && (
              <p className="text-red-500 text-xs">
                {errors.password.message}
              </p>
            )}
          </div>
          <div>
            <Input
              type="password"
              placeholder="Confirm New Password"
              {...register("confirmPassword")}
            />
            {errors.confirmPassword && (
              <p className="text-red-500 text-xs">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>
  
          <Button type="submit" disabled={isSubmitting || !isValid} className="w-full">
            {isSubmitting ? "Saving..." : "Reset Password"}
          </Button>

        </form>
      </CardContent>
    </Card>
  )
}