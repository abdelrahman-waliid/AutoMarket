import { AuthOptions } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials" 
import toast from "react-hot-toast";

// da el object elly bndeeh le function NextAuth elly fe route.ts w kaman bndeeh le getServerSessiion fe ay makan 3ayzen ngeb feeh el token aw el session 3moman

export const authOptions : AuthOptions ={

    providers : [
        CredentialsProvider({
            name: "USER" , // elly byezhar fel form
            credentials: { // el inputs elly 3ayz el user yeda5alha
                email:{placeholder:'example@gmail.com' , type: 'email'},
                password: {placeholder: 'Enter Your Password', type: 'password'}
            },
            async authorize(data){  // function elly bt call API 3adi gdn bs lazem te return haga w hata5od data elly el user da5alha
               try{

                   const response = await fetch(`${process.env.BASE_URL}api/Auth/login` , {
                       method:'POST',
                       body: JSON.stringify({email: data?.email , password: data?.password}), // data elly gaya mn el user fel body
                       headers: {'content-type' : 'application/json'} // a7dd type el data
                    })
                    console.log(response);
                    
                    const payload  = await response.json() ;  
                    
                    
                    if('token' in payload){
                        return {  // el object da elly nextAuth hate7tfez beeh mn demn kol elly rage3 elly howa el user elly kont 3amlo fel next-auth.d.ts
                            id:payload.user.id, // egbary nextAuth 3Ayzah yerga3 w mafesh id fa e5tarna ay haga unique zy el email
                            user: payload.user,
                            token:payload.token 
                        }
                    }else{ 
                        throw new Error(payload.title)   // hayerga3 fel searchParams fel url
                    }
                }catch(err){
                    console.log(err);
                    
                } 
                return null
            } 
        })
    ] , 
    pages: {
        signIn: '/login',  // lama akteb fel path api/auth/signin yro7 lel login bta3ty msh el form bta3to
        error: '/login'    // w law feeh error fel login ya3ny rl path feeh error yro7 lel login bta3ty bardo 3lashan law 3ayez a3red error hnak aw haga
    } ,
    secret:process.env.NEXTAUTH_SECRET ,
    callbacks: {  // object feeh methods byet3melaha call after login
        async jwt({token , user}){  // el token da fady ka2eno kees fady hamla feeh hagat hia elly hait3mlha tashfeeeeeer
                if(user){
                    token.user = user.user  // ha7ot feeh fe makan el user elly m3roof mn el interface elly 3mlto fe next-auth.d.ts el user elly fel user elly next e7tafazet beeh foo2 b3d ma 3mlna call lel api
                    token.token = user.token // ha7ot feeh fe makan el token elly bardo ma3roof mn nafs el interface el token elly fel user elly next e7tafazet beeh foo2 b3d ma 3mlna call lel api
                } 

            return token // da kda token mo4afar
        } ,

        async session ({session , token}){
            if(token){
                session.user = token.user       //el 7al fe session.user el module elly et3amal zeyada fel next-auth.d.ts elly howa interface JWT msh 3aref el sbb ????!
                session.token = token.token    // da el token el 3ady bta3y elly byerga3 b3d el login el so8ayar msh elly metshafar
            }
            return session
        }
    }
}