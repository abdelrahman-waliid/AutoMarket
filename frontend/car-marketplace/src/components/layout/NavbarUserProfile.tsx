"use client"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { LogOutIcon, ShieldCheck, User } from "lucide-react"
import Logout from "../auth/Logout"
import Link from "next/link"

export default function NavbarUserProfile({role , avatar} : {role : string , avatar : string}) {
  return  <>
  
  <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="rounded-full">
          <Avatar>
            {avatar && avatar.trim() !== "" && (
              <AvatarImage
                src={avatar}
                alt="user avatar"
                onError={(e) => {
                  e.currentTarget.style.display = "none";
                }}
              />
            )}

            <AvatarFallback>
              <User className="w-4 h-4" />
            </AvatarFallback>
          </Avatar>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-32">
        <DropdownMenuGroup>
          <DropdownMenuItem asChild>  
              <Link href={"/profile"}>  
                  <span className="flex items-center gap-2 hover:text-primary">
                    <User className="w-4 h-4"/> 
                    Profile
                  </span> 
              </Link>  
          </DropdownMenuItem>
          {role === "Admin" && <DropdownMenuItem asChild>  
                                  <Link href={"/admin/admin-panel"}> 
                                    <span className="flex items-center gap-2 hover:text-primary">
                                      <ShieldCheck className="w-4 h-4"/> 
                                      Admin Panel 
                                    </span> 
                                  </Link> 
                                </DropdownMenuItem>} 
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
           <Logout/>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
    
  
  </>
}
