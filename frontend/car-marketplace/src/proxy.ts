import { getToken } from 'next-auth/jwt'
import { NextRequest, NextResponse } from 'next/server'
import React from 'react'



const authRoutes = [
    "/login" , 
    "/register"
]

const protectedRoutes = [
    "/user/dashboard",
    "/user/my-cars",
    "/user/messages",
]



export default async function proxy(req : NextRequest) {
    const token = await getToken({req})
    const path = req.nextUrl.pathname
    const role = token?.user?.role


// helper function for login redirect
  const redirectToLogin = () => {
    const redirectUrl = new URL("/login", req.url)

    redirectUrl.searchParams.set(
      "callbackUrl",
      req.nextUrl.pathname + req.nextUrl.search
    )

    return NextResponse.redirect(redirectUrl)
  }

    //auth pages

    if(authRoutes.includes(path)){
        if(token){
            return NextResponse.redirect(new URL("/market-place" , req.url))
        }
        return NextResponse.next()
    }

    // protected routes

    if(protectedRoutes.some(route => path.startsWith(route))){
        if(!token) {
            return redirectToLogin()
        }

        if(role !== "User"){
            return NextResponse.redirect(new URL("/market-place" , req.url))
        }
        
        return NextResponse.next()
    }

    // Admin panel

    if(path.startsWith("/admin")) {
        if(!token) {
            return redirectToLogin()
        }

        if(role !== "Admin") {
            return NextResponse.redirect(new URL("/market-place" , req.url))
        }
        return NextResponse.next()
    }

    return NextResponse.next()
   
}
