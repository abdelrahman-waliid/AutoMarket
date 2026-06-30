"use server"

import { authOptions } from "@/auth"
import { AdminUsersResponse } from "@/Interface/AdminUsers"
import { CarResponse, DetailsOfNewCar } from "@/Interface/CarInterface"
import { getServerSession } from "next-auth"
import { unauthorized } from "next/navigation"

export async function getAllCars(params:any) {

  try {
    
      const query = new URLSearchParams()
    
      if(params.page) query.append("PageNumber", params.page)
    
      if(params.brand) query.append("Brand", params.brand)

      if (params.model) query.append("Model", params.model)
    
      if(params.minPrice) query.append("MinPrice", params.minPrice)
    
      if(params.maxPrice) query.append("MaxPrice", params.maxPrice)
    
      if(params.minYear) query.append("MinYear", params.minYear)
    
      if(params.maxYear) query.append("MaxYear", params.maxYear)  

      if (params.search) query.append("Search", params.search)
        
      const res = await fetch(
        `${process.env.BASE_URL}api/Cars?${query.toString()}`,
        { cache: "no-store"  , headers : { "Content-Type": "application/json", Accept: "application/json" } }
      )
      if(!res.ok){
        throw new Error(`API error: ${res.status} ${res.statusText}`)
      }
      const data = await res.json()
      
      return data
  } catch (error) {
    console.log("getAllCars error:"  ,error); 
    return { items: [], total: 0 }
  }  

}

export async function getCarById(carId : string) {
  try {
    const response = await fetch(`${process.env.BASE_URL}api/cars/`+carId ,{
      cache:"no-store"
    })

    if(!response.ok){
      console.log("Failed to fetch car:", response.status); // law API rga3 error
      return null;
    }
    const data = await response.json()

    return data ;
    
  } catch (error) {
    console.log("Error in getCarById:", error);
    return null;
  }


}

export async function getMyCars(page: number) {
  try {
    const session = await getServerSession(authOptions)

    if (!session) {
      return {
        success: false, 
        message: "Unauthorized", 
      }
    }

    const res = await fetch(
      `${process.env.BASE_URL}api/my-cars?PageNumber=${page}&PageSize=10`,
      {
        headers: {
          Authorization: `Bearer ${session.token}`,
        },
        cache: "no-store",
      }
    )

    if(res.status === 401) {
      return {
        success : false ,
        message : "session expired" ,
        unauthorized : true
      }
    }
 
    if (!res.ok) { 

      return {
        success: false, 
        message: "Something went wrong", 
      }
    }

    // ✅ success response
    const data = await res.json()

    return {
      success: true, 
      data,
    }
  } catch (error: any) {
    console.log("SERVER ACTION ERROR:", error)

    return {
      success: false, 
      message: "Internal Server Error", 
  }
}
}
 

export async function deleteCar(carId: string) {
  try {
    const session = await getServerSession(authOptions)

    if (!session?.token) {
      return {
        success: false,
        message: "Unauthorized",
      }
    }

    const res = await fetch(
      `${process.env.BASE_URL}api/Cars/${carId}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${session.token}`,
        },
      }
    ) 


    // ✅ success (أي status ناجح)
    if (res.ok) {
      return {
        success: true,
        message: "Car deleted successfully",
      }
    }

    // ❌ API failed
    return {
      success: false,
      message: "Failed to delete car",
    }

  } catch {
    // ❌ network / server action error
    return {
      success: false,
      message: "Something went wrong",
    }
  }
}
 

export async function CreateNewCar(formData: FormData) {
  try {
    const session = await getServerSession(authOptions)

    if (!session?.token) {
      return {
        success: false,
        message: "Unauthorized",
      }
    }

    const res = await fetch(
      `${process.env.BASE_URL}api/Cars/with-images`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${session.token}`, 
        },
        body: formData,
      }
    )

    if (res.ok) {
      const data = await res.json()

      return {
        success: true,
        message: "Car created successfully",
        data,
      }
     
    }

    return {
      success: false,
      message: "Failed to create car",
    }

  } catch (error) {
    console.log(error)

    return {
      success: false,
      message: "Something went wrong",
    }
  }
}

export async function updateCar(carData : CarResponse) {
    try {
      const session = await getServerSession(authOptions)

      if(!session?.token){
        return {
          success : false ,
          message : "Unauthorized"
        }
      }

      const res = await fetch(`${process.env.BASE_URL}api/Cars/${carData.id}` , {
        method : "PUT" ,
        headers : {
          "Content-Type" : "application/json" ,
          Authorization : `Bearer ${session.token}`
        },
        body : JSON.stringify(carData)
      })

      if(res.ok){
        const data = await res.json()
        return {
          success : true ,
          message : "Car updated successfully" ,
          data
        }
      }

  //handling errors based on status
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
      return { success: false, message: "Car not found" }
    }

    return {
      success : false , 
      message : "Faild to update car"
    }

      
    } catch (error) {
      console.log(error);
      
      return {
        success : false , 
        message : "Something went wrong"
      }
    }
}
