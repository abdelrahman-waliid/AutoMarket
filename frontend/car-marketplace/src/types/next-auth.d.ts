import { UserInterface } from "@/Interface/LoginInterface"
import NextAuth from "next-auth"
import { JWT } from "next-auth/jwt"

declare module "next-auth" {
  /**
   * Returned by `useSession`, `getSession` and received as a prop on the `SessionProvider` React Context
   */
  interface Session { // da elly hayerga3 mn el API bta3 el login w da elly ha3raf meno da login wala la badal el token 3alashan security
    user: UserInterface
    token:string
  }
  interface User {   // da elly nextAuth hate7tfez beeh mn elly rage3 mn el API
    user: UserInterface
    token: string
}
}


declare module "next-auth/jwt" {  
  /** Returned by the `jwt` callback and `getToken`, when using JWT sessions */
  interface JWT extends User{}  // da elly e7tafazt beeh mn demn elly rage3 mn el API w howa da elly hayetshafar w hayt3emel meno token gded 
}
declare module "next-auth/jwt" {      // leeh mn 8eer el goz2 da kan fe error fe session.user ???!
  interface JWT {
    user: UserInterface
    token: string
  }
}