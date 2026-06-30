import { getServerSession } from "next-auth";
import { authOptions } from "@/auth";
import AdminPanelClient from "@/components/layout/AdminPanelClient";

export default async function AdminPanel() {
  
  const session = await getServerSession(authOptions);

   
  return <>
  
  {session && <AdminPanelClient token={session.token}/>}
  </>
}