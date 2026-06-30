export function formatTimeAgoMinus3Hours(date: string) {
  const now = new Date()
  const past = new Date(date)

  const diffInSeconds = Math.floor((now.getTime() - past.getTime()) / 1000)

  const OFFSET_HOURS = 3 // 👈 اللي انت عايزه
  const offsetInSeconds = OFFSET_HOURS * 3600

  const adjustedSeconds = diffInSeconds - offsetInSeconds

  if (adjustedSeconds <= 0) return "now"

  const minutes = Math.floor(adjustedSeconds / 60)
  const hours = Math.floor(adjustedSeconds / 3600)
  const days = Math.floor(adjustedSeconds / 86400)

  if (days > 0) return `${days}d ago`
  if (hours > 0) return `${hours}h ago`
  if (minutes > 0) return `${minutes}m ago`

  return "now"
}