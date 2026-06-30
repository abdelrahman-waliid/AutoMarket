"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useRouter } from "next/navigation"
import { useState } from "react"

import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Card, CardContent, CardHeader, CardTitle, CardDescription
} from "@/components/ui/card"
import toast from "react-hot-toast"

const schema = z.object({
  email: z.string().email("Invalid email"),
})

type FormData = z.infer<typeof schema>

export default function ForgotPasswordLayout() {

  const [success, setSuccess] = useState(false) 
  const router = useRouter()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {  
    try {
      const res = await fetch( "http://localhost:5127/api/Auth/forgot-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify(data),
      }) 
  
      if (res.ok) { 
          toast.success("Email sent , Check your inbox for reset instructions") 
          setSuccess(true)
          setTimeout(() => {
          router.push("/login")
      }, 2500)
      }else{
        toast.error("Something went wrong. Try again.")
      }
      
    } catch (error) {
      console.log(error);
      toast.error("Network error. Please try again.")
    }


  }

  if (success) {
    return (
      <Card className="max-w-md w-full text-center p-6">
        <h2 className="text-xl font-bold">Check your email</h2>
        <p className="text-gray-500 text-sm">
          If an account exists, we sent reset instructions.
        </p>
      </Card>
    )
  }

  return (
    <Card className="w-full max-w-md rounded-2xl">
      <CardHeader className="text-center">
        <CardTitle>Forgot Password</CardTitle> 
      </CardHeader>

      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

          <div>
            <Input placeholder="Email" {...register("email")} />
            {errors.email && (
              <p className="text-red-500 text-xs">
                {errors.email.message}
              </p>
            )}
          </div> 

          <Button type="submit" disabled={isSubmitting || !isValid} className="w-full">
            {isSubmitting ? "Sending..." : "Send Reset Link"}
          </Button>

        </form>
      </CardContent>
    </Card>
  )
}