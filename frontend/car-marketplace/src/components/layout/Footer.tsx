import { Car } from 'lucide-react'
import Link from 'next/link'
import React from 'react'

export default function Footer() {
  return  <>
  
    <footer className="border-t border-border bg-background py-8 mt-auto">
        <div className="container mx-auto px-4 flex flex-col md:flex-row justify-between items-center gap-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2">
            <Car className="h-4 w-4" />
            <span>© 2026 AutoMarket. All rights reserved.</span>
          </div>
          <div className="flex gap-6">
            <Link href={'#'} className="hover:text-primary transition-colors">Terms</Link>
            <Link href={'#'} className="hover:text-primary transition-colors">Privacy</Link>
            <Link href={'#'} className="hover:text-primary transition-colors">Contact</Link>
          </div>
        </div>
      </footer>

  </>
}
