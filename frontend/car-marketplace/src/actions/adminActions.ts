"use server"

import { authOptions } from "@/auth"
import { AdminUsersResponse } from "@/Interface/AdminUsers" 
import { getServerSession, Session } from "next-auth" 

export async function getUsers() {
  try {
    const session : Session | null = await getServerSession(authOptions)

    if(!session){
      return {
        success : false ,
        message : "Unauthorized"
      }
    }
    const res = await fetch(`${process.env.BASE_URL}api/admin/users?pageNumber=1&pageSize=10` , {
      headers : {
        Authorization : `Bearer ${session?.token}`
      } ,
      cache : "no-store"
    })

    if(res.ok){
      const data : AdminUsersResponse = await res.json()
      return {
        success : true ,
        data
      }
    }
    return {
      success : false , 
      message : "something went wrong"
    }
  } catch (error) {
    console.log(error);
    return {
      success : false ,
      message : "network error"
    }
  }
  
}

export async function updateUserAdmin(userData : AdminUsersResponse) {
    try {
      const session = await getServerSession(authOptions)

      if(!session?.token){
        return {
          success : false ,
          message : "Unauthorized"
        }
      }

      const res = await fetch(`${process.env.BASE_URL}api/admin/users/${userData.id}/role` , {
        method : "PUT" ,
        headers : {
          "Content-Type" : "application/json" ,
          Authorization : `Bearer ${session.token}`
        },
        body : JSON.stringify(userData)
      })

      if(res.ok){
        const data = await res.json()
        return {
          success : true ,
          message : "user updated successfully" ,
          data
        }
      }

  
    if (res.status === 400) {
      return { success: false, message: "Invalid data or ID mismatch" }
    }

    if (res.status === 401) {
      return { success: false, message: "Unauthorized" }
    }

    if (res.status === 403) {
      return { success: false, message: "Forbidden (not owner)" }
    }

    if (res.status === 404) {
      return { success: false, message: "user not found" }
    }

    return {
      success : false , 
      message : "Faild to update user"
    }

      
    } catch (error) {
      console.log(error);
      
      return {
        success : false , 
        message : "Something went wrong"
      }
    }
}

export async function deleteUser(userId: string) {
  try {
    const session = await getServerSession(authOptions)

    if (!session?.token) {
      return {
        success: false,
        message: "Unauthorized",
      }
    }

    const res = await fetch(
      `${process.env.BASE_URL}api/admin/users/${userId}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${session.token}`,
        },
      }
    ) 


    
    if (res.ok) {
      return {
        success: true,
        message: "User deleted successfully",
      }
    }

    
    return {
      success: false,
      message: "Failed to delete user",
    }

  } catch {
    
    return {
      success: false,
      message: "Something went wrong",
    }
  }
}


export async function deleteCarList(listId: string) {
  try {
    const session = await getServerSession(authOptions)

    if (!session?.token) {
      return {
        success: false,
        message: "Unauthorized",
      }
    }

    const res = await fetch(
      `${process.env.BASE_URL}api/admin/listings/${listId}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${session.token}`,
        },
      }
    ) 


   
    if (res.ok) {
      return {
        success: true,
        message: "Car deleted successfully",
      }
    }

    
    return {
      success: false,
      message: "Failed to delete Car",
    }

  } catch {
    
    return {
      success: false,
      message: "Something went wrong",
    }
  }
}