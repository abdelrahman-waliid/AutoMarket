"use client"

import React from 'react'
import { DropdownMenuItem } from '../ui/dropdown-menu'
import { LogOutIcon } from 'lucide-react'
import { signOut } from 'next-auth/react'
import { LogOutAction } from '@/actions/logOutAction'
import toast from 'react-hot-toast'

export default function Logout() {

   async function handleLogOut() {
        const result = await LogOutAction()

        if(result?.success){
            await signOut({callbackUrl : '/login'})
            toast.success("Logged Out Successfully")
        }else{
            console.log("logout api failed"); 
        }
    }
  return  <>
  
  
        <DropdownMenuItem variant="destructive" onClick={handleLogOut}>
             <LogOutIcon/> Logout
        </DropdownMenuItem>
  
  </>
}
