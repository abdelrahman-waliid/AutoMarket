import * as z from 'zod' ;

export const registerSchema = z.object({
  fullName : z.string().nonempty('Name is required')
        .min(3 , 'The name must be more than 2 characters')
        .max(20 , 'The name must be less than 21 characters') ,
  email: z.string().nonempty("Email is required")
         .regex(/^[a-zA-Z0-9]+@[a-zA-Z0-9.-]+\.[a-zA-Z\d]{2,}$/ , 'Invalid Email') , 
  password: z.string().nonempty('Password Required') 
            .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,15}$/ , 'Pasword must be more than 8 characters')  ,
  rePassword: z.string().nonempty('rePassword Required')   
   
  }  
                 
).refine((data)=> data.password === data.rePassword , {path:["rePassword"] , message:'password and rePassword dont match'}) 