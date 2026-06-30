export function formatCurrency(num:number){   // de 3alashan shakl w see8et el arkam 
    return new Intl.NumberFormat("en-us" , {
        style : "currency" ,         
        currency: "EGP"
    }).format(num)
}