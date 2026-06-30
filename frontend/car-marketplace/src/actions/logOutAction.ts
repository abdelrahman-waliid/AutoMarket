"use server"

import { authOptions } from "@/auth"
import { getServerSession } from "next-auth" 
import { success } from "zod"

export async function LogOutAction() {
    
    const session = await getServerSession(authOptions)
    const token = session?.token

    if(token){
        try {
            const response = await fetch(`${process.env.BASE_URL}api/Auth/logout` , {
                method : "POST",
                headers : {
                    Authorization: `Bearer ${token}`
                } ,
                cache :"no-store"
            })
            if(!response.ok) {
                return {success : false}
            }
            return {success : true}
            
        } catch (error) {
            console.log(error);
            return {success : false}
        }
    }
    return {success : false}
}