export function formatTimeAgo(dateString: string) {
  const now = new Date();
  const createdDate = new Date(dateString);

  if (isNaN(createdDate.getTime())) {
    return "";
  }

  const diffInMs = now.getTime() - createdDate.getTime();

  const diffInMinutes = diffInMs / (1000 * 60);
  const diffInHours = diffInMinutes / 60;
  const diffInDays = diffInHours / 24;

  // أقل من دقيقة
  if (diffInMinutes < 1) {
    return "Just now";
  }

  // أقل من ساعة
  if (diffInHours < 1) {
    return `${Math.floor(diffInMinutes)}m ago`;
  }

  // أقل من 24 ساعة
  if (diffInHours < 24) {
    return `${Math.floor(diffInHours)}h ago`;
  }

  // أقل من 7 أيام
  if (diffInDays < 7) {
    return `${Math.floor(diffInDays)}d ago`;
  }

  // أكتر من 7 أيام
  return createdDate.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}