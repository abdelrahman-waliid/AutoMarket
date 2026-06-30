import { authOptions } from "@/auth";
import { DashBoardResponse } from "@/Interface/DashBoardInterface";
import { getServerSession } from "next-auth";
import { unauthorized } from "next/navigation";

export async function getDashboard() {
    try {
        const session = await getServerSession(authOptions)

        if(!session){
            return {
                success : false ,
                message : "Unauthorized"
            }
        }
        const res = await fetch(`${process.env.BASE_URL}api/Dashboard` , {
            headers : {
                Authorization : `Bearer ${session.token}` 
            } ,
            cache : "no-store"
        })

        if(res.status === 401){
            return {
                success : false ,
                message : "Session Expired" ,
                unauthorized : true
            }
        }
        
        if(!res.ok){
            return{
                success : false , 
                message : "faild to get dashboard data"
            }
        }
        const data : DashBoardResponse = await res.json()
        return{
            success : true ,
            data ,
            userName : session.user.fullName
        }
    } catch (error) {
        console.log(error);
        return {
            success : false , 
            message : "Something went wrong"
        }      
    }
}