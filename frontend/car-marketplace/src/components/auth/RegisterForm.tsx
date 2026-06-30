'use client' 
import { registerBody, signUpAction } from "@/actions/registerAction" 
import { zodResolver } from "@hookform/resolvers/zod" 
import { useForm } from "react-hook-form"
import { Field } from "../ui/field"
import { Input } from "../ui/input" 
import { useState } from "react"
import { Button } from "../ui/button"
import toast from "react-hot-toast"
import { useRouter } from "next/navigation"
import { Car, Loader2 } from "lucide-react" 
import { registerSchema } from "@/schema/RegisterSchema" 
import { FaildRegisterInterface, SuccessRegisterInterface } from "@/Interface/RegisterInterface"
 
 
 
 

export default function RegisterForm() {

  const router = useRouter()
  const [isLoading, setIsLoading] = useState(false)
   

    const {handleSubmit , register , formState} = useForm({
        defaultValues: {
            fullName: '' , 
            email: '' ,
            password: '' , 
            rePassword:''  
        } , 
        resolver : zodResolver(registerSchema) ,
        mode:'all'
    }) 

    async function signUp(values:registerBody) {
      setIsLoading(true)
      const response : SuccessRegisterInterface | FaildRegisterInterface  = await signUpAction(values)  
      console.log(response);
      
      if('status' in response){
        toast.error(response.detail) 
      }else{
        toast.success('Registered Successfully')
        router.push('/login')
      }
      
      setIsLoading(false)
 
        
    }

  return <>
  <div className="min-h-screen bg-background/5 flex flex-col items-center justify-center px-4 py-10">

  {/* Header */}
  <div className="w-full max-w-md space-y-6 mb-8">
    <div className="text-center space-y-2">

      <div className="flex justify-center">
        <div className="h-12 w-12 rounded-xl bg-primary flex items-center justify-center text-primary-foreground">
          <Car className="h-8 w-8"/>
        </div>
      </div>

      <h2 className="text-2xl sm:text-3xl font-heading font-extrabold tracking-tight">
        AutoMarket
      </h2>

      <p className="text-sm sm:text-base text-muted-foreground">
        Sign up to manage your listings and messages
      </p>

    </div>
  </div>


  {/* Form */}
  <div className="w-full max-w-md">

    <div className="bg-background py-8 px-6 shadow-2xl rounded-2xl border w-full">

      <form onSubmit={handleSubmit(signUp)} className="flex flex-col gap-4">

        {/* Full Name */}
        <Field className="flex flex-col gap-1">
          <label className="text-sm font-bold" htmlFor="name-demo-api-key">
            Full Name
          </label>

          <Input
            aria-invalid={Boolean(formState.errors.fullName?.message)}
            id="name-demo-api-key"
            type="text"
            placeholder="Enter Your Name"
            {...register('fullName')}
          />

          {formState.errors.fullName && (
            <p className="text-red-500 text-sm">
              {formState.errors.fullName.message}
            </p>
          )}
        </Field>


        {/* Email */}
        <Field className="flex flex-col gap-1">
          <label className="text-sm font-bold" htmlFor="email-demo-api-key">
            Email
          </label>

          <Input
            aria-invalid={Boolean(formState.errors.email?.message)}
            id="email-demo-api-key"
            type="email"
            placeholder="example@gmail.com"
            {...register('email')}
          />

          {formState.errors.email && (
            <p className="text-red-500 text-sm">
              {formState.errors.email.message}
            </p>
          )}
        </Field>


        {/* Password */}
        <Field className="flex flex-col gap-1">
          <label className="text-sm font-bold" htmlFor="password-demo-api-key">
            Password
          </label>

          <Input
            aria-invalid={Boolean(formState.errors.password?.message)}
            id="password-demo-api-key"
            type="password"
            placeholder="Enter Your Password"
            {...register('password')}
          />

          {formState.errors.password && (
            <p className="text-red-500 text-sm">
              {formState.errors.password.message}
            </p>
          )}
        </Field>


        {/* Confirm Password */}
        <Field className="flex flex-col gap-1">
          <label className="text-sm font-bold" htmlFor="repassword-demo-api-key">
            Confirm Password
          </label>

          <Input
            aria-invalid={Boolean(formState.errors.rePassword?.message)}
            id="rePassword-demo-api-key"
            type="password"
            placeholder="Confirm Your Password"
            {...register('rePassword')}
          />

          {formState.errors.rePassword && (
            <p className="text-red-500 text-sm">
              {formState.errors.rePassword.message}
            </p>
          )}
        </Field>


        <Button
          disabled={isLoading}
          type="submit"
          className="mt-4 w-full"
        >
          {isLoading && <Loader2 className="animate-spin mr-2"/>}
          Sign Up
        </Button>

      </form>

    </div>

  </div>

</div>
  </>
}
