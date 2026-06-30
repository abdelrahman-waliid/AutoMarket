'use client'
import React from 'react'
import { NavigationMenu, NavigationMenuItem, NavigationMenuLink, NavigationMenuList } from '../ui/navigation-menu'
import Link from 'next/link'
import { usePathname } from 'next/navigation'

export default function NavbarLinks({role}:{role : string}) {

    const pathName = usePathname()
    const isActive = (path:string) => pathName.startsWith(path)   // boolean

  return <>
  
  
    <NavigationMenu>
                    <NavigationMenuList>
                     
                        <NavigationMenuItem className={`cursor-pointer font-medium text-sm p-2 rounded-xl font-heading
                            ${isActive('/market-place')
                                ? 'text-primary'
                                : 'text-muted-foreground hover:text-primary'
                            }  `}>
                            <NavigationMenuLink asChild>
                                <Link href={'/market-place'}>MarketPlace</Link>
                            </NavigationMenuLink>
                        </NavigationMenuItem>
                     {/* user links */}
                       {role === "User" && (
                            <> 
                                <NavigationMenuItem className={`cursor-pointer font-medium text-sm p-2 rounded-xl font-heading
                                    ${isActive('/user/dashboard')
                                        ? 'text-primary'
                                        : 'text-muted-foreground hover:text-primary'
                                    }  `}>
                                    <NavigationMenuLink asChild>
                                        <Link href={'/user/dashboard'}>Dashboard</Link>
                                    </NavigationMenuLink>
                                </NavigationMenuItem> 
                                <NavigationMenuItem className={`cursor-pointer font-medium text-sm p-2 rounded-xl font-heading
                                    ${isActive('/user/my-cars')
                                        ? 'text-primary'
                                        : 'text-muted-foreground hover:text-primary'
                                    }  `}>
                                    <NavigationMenuLink asChild>
                                        <Link href={'/user/my-cars'}>My Cars</Link>
                                    </NavigationMenuLink>
                                </NavigationMenuItem>
                                <NavigationMenuItem className={`cursor-pointer font-medium text-sm p-2 rounded-xl font-heading
                                    ${isActive('/user/messages')
                                        ? 'text-primary'
                                        : 'text-muted-foreground hover:text-primary'
                                    }  `}>
                                    <NavigationMenuLink asChild>
                                        <Link href={'/user/messages'}>Messages</Link>
                                    </NavigationMenuLink>
                                </NavigationMenuItem>
                            </>
                        )}
                        {/* admin links */} 
                          {role === "Admin" && (
 
                                <NavigationMenuItem className={`cursor-pointer font-medium text-sm p-2 rounded-xl font-heading
                                    ${isActive('/admin/admin-panel')
                                        ? 'text-primary'
                                        : 'text-muted-foreground hover:text-primary'
                                    }  `}>
                                    <NavigationMenuLink asChild>
                                        <Link href={'/admin/admin-panel'}>Admin Panel</Link>
                                    </NavigationMenuLink>
                                </NavigationMenuItem>
                         )}

                    </NavigationMenuList>
                    </NavigationMenu>
  
  </>
}
