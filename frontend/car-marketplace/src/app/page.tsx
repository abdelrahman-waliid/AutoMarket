import React from 'react'
import MarketplacePage from './(pages)/market-place/page'

export default  function HomePage({searchParams} : {searchParams : any}) { 
  return  <>

      <MarketplacePage searchParams={searchParams}/>
  
  </>
}
