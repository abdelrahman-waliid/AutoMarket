"use client"

import { useRef, useState } from "react"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { User } from "lucide-react"
import { updateAvatar } from "@/actions/profileActions"
import { toast } from "react-hot-toast"
import { useRouter } from "next/navigation"

export default function ChangeAvatarLayout({
  avatarUrl,
  fullName,
}: {
  avatarUrl: string
  fullName: string
}) {

  const fileRef = useRef<HTMLInputElement | null>(null)
  const [preview, setPreview] = useState<string | null>(avatarUrl || null)
  const [loading, setLoading] = useState(false)
  const router = useRouter()
  const [file, setFile] = useState<File | null>(null)

  // 📌 لما المستخدم يختار صورة
  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]

    if (!file) return

    setFile(file)
    const url = URL.createObjectURL(file)
    setPreview(url)
  }

  // 📌 رفع الصورة
  async function handleUpload() {
    if (!fileRef.current?.files?.[0]) {
      toast.error("Please select an image")
      return
    }

    const formData = new FormData()
    formData.append("Avatar", fileRef.current.files[0])

    setLoading(true) 
    
    const res = await updateAvatar(formData)

    if (res.success) {
      toast.success("Avatar updated successfully")
      router.refresh()
      setFile(null)
    } else {
      toast.error(res.message)
    }

    setLoading(false)
  }

  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-4">

      {/* Avatar */}
      <Avatar className="w-20 h-20">

        {preview ? (
          <AvatarImage src={preview} />
        ) : null}

        <AvatarFallback>
          {preview ? (
            fullName?.slice(0, 2).toUpperCase()
          ) : (
            <User className="w-6 h-6 text-gray-500" />
          )}
        </AvatarFallback>

      </Avatar>

      {/* Controls */}
      <div className="flex flex-col gap-2">

        {/* hidden input */}
        <input
          type="file"
          accept="image/*"
          ref={fileRef}
          onChange={handleFileChange}
          className="hidden"
        />

        <div className="flex gap-2 flex-wrap">

          <Button
            type="button"
            variant="outline"
            onClick={() => fileRef.current?.click()}
          >
            Choose Image
          </Button>

          <Button
            type="button"
            onClick={handleUpload}
            disabled={loading || !file}
            className="bg-blue-600 hover:bg-blue-700"
          >
            {loading ? "Uploading..." : "Upload"}
          </Button>

        </div>

        <span className="text-xs text-gray-500">
          JPG, PNG. Max size 800KB
        </span>

      </div>
    </div>
  )
}