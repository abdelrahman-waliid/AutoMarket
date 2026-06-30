 "use client"
 
 
import { Controller, useForm } from "react-hook-form" 
import * as z from "zod"
import {signIn} from 'next-auth/react'

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import toast from "react-hot-toast"
import { useRouter, useSearchParams } from "next/navigation"
import { useEffect, useState } from "react"
import { Car, Loader2 } from "lucide-react"
import Link from "next/link"
import { zodResolver } from "@hookform/resolvers/zod"
 

const formSchema = z.object({
  email: z.email('Please enter a valid email').nonempty('Email is Required') ,
  password: z.string().nonempty('Password is Required').min(6,"Password must be at least 6 characters") 
})

type FormData = z.infer<typeof formSchema>

export default function LoginForm() {

  const [isLoading, setIsLoading] = useState(false)

  const searchParams = useSearchParams()

useEffect(() => {
  const error = searchParams.get("error")

  if (error === "CredentialsSignin") {
    toast.error("Invalid email or password")
    window.history.replaceState({}, "", "/auth/login")
  }
}, [searchParams]) 
  

  const form = useForm< FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  })
  
const router = useRouter()
async  function onSubmit(data: FormData) { 
    setIsLoading(true)
    const response = await signIn('credentials' , {
      email:data.email,
      password:data.password , 
      redirect: true ,   // 3alashan ye reload w el navbar te3raf w da 3alashan mesta5dem el getServerSession() fel Navbar.tsx
      callbackUrl: searchParams.get("callbackUrl") ?? "/market-place"       // ywadeeh direct b3d el login lel products 
    }) 
    // dool malhomsh lazma fe 7alet eny 3amel callbackUrl w redirect be true la2n howa kda elly haywadeny bdon tada5ol mny m3 reload 3alashan ay te8yeer yesama3 fel app kolo
    // if(response?.ok){
    //   toast.success('Login Successfully') 
    //   router.push('/products')
    // }else{
    //   toast.error(response?.error! )
    // }
    setIsLoading(false)
      
  }

  return <>
    <div className="min-h-screen bg-background/5 flex items-center justify-center px-4 py-10">

  <div className="w-full max-w-md space-y-6">

    {/* Logo + Title */}
    <div className="text-center space-y-3">
      <div className="flex justify-center">
        <div className="h-12 w-12 rounded-xl bg-primary flex items-center justify-center text-primary-foreground">
          <Car className="h-7 w-7"/>
        </div>
      </div>

      <h1 className="text-2xl sm:text-3xl font-heading font-extrabold tracking-tight">
        AutoMarket
      </h1>

      <p className="text-sm sm:text-base text-muted-foreground">
        Sign in to manage your listings and messages
      </p>
    </div>


    {/* Card */}
    <Card className="w-full">

      <CardHeader>
        <CardTitle className="text-center text-xl sm:text-2xl font-bold">
          Login here please !
        </CardTitle>
      </CardHeader>


      <CardContent>
        <form
          id="form-rhf-demo"
          onSubmit={form.handleSubmit(onSubmit)}
          className="space-y-5"
        >

          <FieldGroup>

            {/* Email */}
            <Controller
              name="email"
              control={form.control}
              render={({ field, fieldState }) => (
                <Field data-invalid={fieldState.invalid}>
                  <FieldLabel htmlFor="form-rhf-demo-email">
                    Email
                  </FieldLabel>

                  <Input
                    {...field}
                    type="email"
                    id="form-rhf-demo-email"
                    aria-invalid={fieldState.invalid}
                    placeholder="example@gmail.com"
                  />

                  {fieldState.invalid && (
                    <FieldError errors={[fieldState.error]} />
                  )}
                </Field>
              )}
            />

            {/* Password */}
            <Controller
              name="password"
              control={form.control}
              render={({ field, fieldState }) => (
                <Field data-invalid={fieldState.invalid}>
                  <FieldLabel htmlFor="form-rhf-demo-password">
                    Password
                  </FieldLabel>

                  <Input
                    {...field}
                    type="password"
                    id="form-rhf-demo-password"
                    aria-invalid={fieldState.invalid}
                    placeholder="Enter your Password"
                  />

                  <div className="flex justify-end mt-1">
                    <Link href="/forgot-password" className="text-xs text-primary hover:underline">
                      Forgot password?
                    </Link>
                  </div>

                  {fieldState.invalid && (
                    <FieldError errors={[fieldState.error]} />
                  )}
                </Field>
              )}
            />

          </FieldGroup>

        </form>
      </CardContent>


      <CardFooter>
        <Button
          disabled={isLoading}
          type="submit"
          form="form-rhf-demo"
          className="w-full"
        >
          {isLoading && <Loader2 className="animate-spin mr-2"/>}
          Login
        </Button>
      </CardFooter>

    </Card>

  </div>

</div>
  </>
}

