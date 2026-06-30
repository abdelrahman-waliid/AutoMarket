'use server'

export interface registerBody {
    fullName:string,
    email:string,
    password:string,
    rePassword:string 
}

export async function signUpAction(values:registerBody) {
    try{

        const response = await fetch(`${process.env.BASE_URL}api/Auth/register` , {
            method:'POST' , 
            body:JSON.stringify(values) , 
            headers :{
                "Content-Type" : "application/json"
            }
        })
        const data = await response.json()  
        return data ; 
    }catch (err){
        console.log(err);
        throw err
    }
}