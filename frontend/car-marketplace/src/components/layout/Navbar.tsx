import Link from 'next/link' 
import { Button } from '../ui/button'
import { Car, LayoutDashboard, Menu, MessageSquare, ShieldCheck, ShoppingBag } from 'lucide-react'
import { NavigationMenu, NavigationMenuItem, NavigationMenuLink, NavigationMenuList } from '../ui/navigation-menu'
import NavbarLinks from './NavbarLinks'
import NavbarButtons from './NavbarButtons' 
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Sheet,
  SheetClose,
  SheetContent, 
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import { getServerSession } from 'next-auth'
import { authOptions } from '@/auth'
import NavbarUserProfile from './NavbarUserProfile'
import { getProfile } from '@/actions/profileActions'
import ThemeToggle from '../ReuseComponents/ThemeToggle'

export default async function Navbar() {

  const session = await getServerSession(authOptions)
  let avatar = ""
  const res = await getProfile()
  if(!res.success) {
    avatar = ""
  }
  avatar = res?.data?.avatarUrl
  var role = undefined 
  if(session){ 
    role = session.user.role 
  }else{
    role = "guest"
  }
   
  return <>

   <nav className='bg-background border-b border-border py-3 shadow sticky top-0 z-50'>

    <div className='container mx-auto px-4 flex items-center justify-between'>

      {/* LEFT SIDE */}
        <div className='flex items-center gap-4'>

      {/* MOBILE MENU */} 
          <div className='lg:hidden'> 
              <Sheet>
                <SheetTrigger asChild>
                  <Button variant="outline" size="icon">
                    <Menu className="h-5 w-5" />
                  </Button>
                </SheetTrigger>
                <SheetContent side='left'>
                  <SheetHeader>
                      <SheetTitle>

                        <div className="flex items-center gap-2 text-primary font-bold text-xl mb-8">
                          <Car className="h-6 w-6" />
                          AutoMarket
                        </div>

                      </SheetTitle>
                    <div className='flex flex-col justify-center items-start gap-7'>
                      <SheetClose asChild> 
                          <Link href={'/market-place'} className='flex gap-3 text-secondary text-md font-medium font-heading'> <ShoppingBag/>  MarketPlace </Link>
                      </SheetClose>
                    {/* user links */}
                     {role === "User" && (
                        <>
                          <SheetClose asChild>    
                              <Link href={'/user/dashboard'} className='flex gap-3 text-secondary text-md font-medium font-heading'> <LayoutDashboard/>  Dashboard </Link>
                          </SheetClose> 
                          <SheetClose asChild> 
                              <Link href={'/user/my-cars'} className='flex gap-3 text-secondary text-md font-medium font-heading'> <Car/>  My Cars </Link>
                          </SheetClose>
                          <SheetClose asChild> 
                              <Link href={'/user/messages'} className='flex gap-3 text-secondary text-md font-medium font-heading'> <MessageSquare/>  Messages </Link>
                          </SheetClose>
                        </>
                      )}
                    {/* admin link */}
                      {role === "Admin" && ( 
                        <SheetClose asChild> 
                          <Link href={'/admin/admin-panel'} className='flex gap-3 text-secondary text-md font-medium font-heading'> <ShieldCheck/>  Admin Panel </Link>
                        </SheetClose>
                      )}
                    </div>
                  </SheetHeader> 
                </SheetContent>
              </Sheet> 
          </div>

          {/* LOGO */}

          <div>
            <Link href={'/'} className='flex justify-between items-center text-primary gap-2 font-extrabold text-2xl font-heading'>
                <Car className="h-8 w-8" />
                <span className='hidden lg:block'>AutoMarket</span> 
            </Link>
          </div> 
        </div>
          
      {/* DESKTOP LINKS */}
        
          <div className='gap-3 hidden lg:flex'>
            <NavbarLinks role={role}/>   
          </div>

      {/* RIGHT BUTTONS */}

          <div className='flex items-center gap-4'>

             {session ? <NavbarUserProfile role={role} avatar={avatar}/> : <NavbarButtons/>}

             <ThemeToggle/>
             
          </div>
          


        

             
        
    </div>

  </nav>

  </>
}
