"use server"

import { authOptions } from "@/auth"
import { changedPasswordData, UpdatedData } from "@/Interface/ProfileInterface"
import { getServerSession } from "next-auth"
import { unauthorized } from "next/navigation"

export async function getProfile(){
    try{
        const session = await getServerSession(authOptions)

        if(!session){
            return {
                success : false ,
                message : "Unauthorized"  
            }
        }
        const res = await fetch(`${process.env.BASE_URL}api/Profile` , {
            headers : {
                Authorization : `Bearer ${session.token}` 
            } ,
            cache : "no-store"
        })
        if(res.status === 401){
            return {
                success : false ,
                message : "session expired" ,
                unauthorized : true
            }
        }
        if(!res.ok){
            return {
                success : false , 
                message : "Faild to get your profile"
            }
        }
        const data = await res.json()

        return {
            success : true ,
            data
        }
        
    }catch(error){
        console.log(error);
        return {
            success : false , 
            message : "Something went wrong"
        }
    }
}

export async function updateProfile(Changeddata : UpdatedData) {
    try {
        
        const session = await getServerSession(authOptions)
        if(!session){
            return{
                success : false ,
                message : "Unauthorized"
            }
        } 
        
        const res = await fetch(`${process.env.BASE_URL}api/Profile` , {
            headers : {
                Authorization : `Bearer ${session.token}` ,
                "Content-Type" : "application/json"
            } ,
            cache : "no-store" ,
            method : "PUT" ,
            body : JSON.stringify(Changeddata)
        })
        if(!res.ok){
            return{
                success : false ,
                message : "Faild to update your profile"
            }
        }
        const data = await res.json()
        return{
            success : true ,
            data
        }
    } catch (error) {
        console.log(error);
        return{
            success : false ,
            message : "Something went wrong"
        }
    }
}

export async function updateAvatar(formdata : FormData) {
    try {
        const session = await getServerSession(authOptions)

        if(!session){
            return {
                success : false ,
                message : "Unauthorized"
            }
        }
        const res = await fetch(`${process.env.BASE_URL}api/Profile/avatar` , {
            method : "POST" ,
            headers :{
                Authorization : `Bearer ${session.token}` ,
            } ,
            body : formdata
        })

        if (res.ok) {
        const data = await res.json()

        return {
            success: true,
            message: "profile picture added successfully",
            data,
        }
        }

        return{
            success : false ,
            message : "Faild to create avatar"
        }
    } catch (error) {
        console.log(error);
        return{
            success : false ,
            message : "Something went wrong"
        }
    }
}

export async function changePassword(changedData : changedPasswordData) {
    try {
        const session = await getServerSession(authOptions)
        if(!session){
            return{
                success : false ,
                message : "Unauthorized"
            }
        } 
        const res = await fetch(`${process.env.BASE_URL}api/Auth/change-password` , {
            headers : {
                Authorization : `Bearer ${session.token}` ,
                "Content-Type" : "application/json"
            } ,
            cache : "no-store" ,
            method : "POST" ,
            body : JSON.stringify(changedData)
        })

        if(!res.ok){
            return{
                success : false ,
                message : "Current password is wrong"
            }
        }
        const data = await res.json()
        return{
            success : true ,
            data
        }
    } catch (error) {
        console.log(error); 
        return {
            success : false ,
            message : "Something went wrong"
        }
    }
    
}