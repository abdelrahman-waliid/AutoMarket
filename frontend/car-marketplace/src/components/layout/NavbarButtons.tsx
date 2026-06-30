'use client'
import React from 'react'
import { Button } from '../ui/button'
import Link from 'next/link'
import { usePathname } from 'next/navigation'

export default function NavbarButtons() {


    const pathName = usePathname()
        const isActive = (path:string) => pathName.startsWith(path)   // boolean

  return  <>
   
            <Button className={`text-secondary font-heading ${isActive('/login') 
                                ?   'bg-primary text-secondary'
                                : 'bg-primary text-primary-foreground'
                            }`}>
                 <Link href={'/login'} > login </Link> 
            </Button>
            <Button className={`text-secondary font-heading ${isActive('/register') 
                                ?   'bg-primary text-secondary'
                                : 'bg-primary text-primary-foreground'
                            }`}>
                <Link href={'/register'} > register  </Link> 
            </Button> 
  
  </>
}
