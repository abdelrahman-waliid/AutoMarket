
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card" 
import ChangeAvatarLayout from "@/components/layout/ChangeAvatarLayout"
import ChangeProfileLayout from "@/components/layout/ChangeProfileLayout"
import { getProfile } from "@/actions/profileActions"
import { UserProfileInterface } from "@/Interface/ProfileInterface"
import ForceLogout from "@/components/ReuseComponents/ForceLogout"
import BackButton from "@/components/ReuseComponents/BackButton"
import ChangePasswordLayout from "@/components/layout/ChangePasswordLayout"

export default async function ProfilePage() {

    const res = await getProfile()
    if(!res.success){
      if(res.unauthorized) {
        return <ForceLogout/>
      }
    }
    const profile : UserProfileInterface = res.data
  return (
    <div className="min-h-screen bg-background px-4 py-8">
      <div className="max-w-4xl mx-auto mb-4">
        <BackButton />
      </div>
      {/* Page Title */}
      <div className="max-w-4xl mx-auto mb-6">
        <h1 className="text-2xl font-bold">My Profile</h1>
        <p className="text-gray-500">
          Manage your account settings and preferences.
        </p>
      </div>

      {/* Card */}
      <Card className="bg-background max-w-4xl mx-auto rounded-2xl shadow-sm">
        <CardHeader>
          <CardTitle>Personal Information</CardTitle>
          <CardDescription>
            Update your personal details here.
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-6">

          {/* Avatar Section */}
           <ChangeAvatarLayout avatarUrl={profile.avatarUrl} fullName={profile.fullName}/>

          {/* Divider */}
          <div className="border-t" />

           <ChangeProfileLayout profile={profile}/>

           {/* Divider */}
          <div className="border-t" />
          <ChangePasswordLayout/>

        </CardContent>
      </Card>
    </div>
  )
}