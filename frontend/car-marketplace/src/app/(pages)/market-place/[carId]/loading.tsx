export default function Loading() {
  return (
    <div className="container mx-auto px-6 py-10 animate-pulse">

      <div className="grid lg:grid-cols-[2fr_1fr] gap-8">

        {/* Left */}
        <div className="space-y-6">

          <div className="h-87.5 bg-muted rounded-2xl"></div>

          <div className="flex justify-between">
            <div className="space-y-2">
              <div className="h-6 w-40 bg-muted rounded"></div>
              <div className="h-4 w-32 bg-muted rounded"></div>
            </div>
            <div className="h-6 w-24 bg-muted rounded"></div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="h-16 bg-muted rounded"></div>
            <div className="h-16 bg-muted rounded"></div>
            <div className="h-16 bg-muted rounded"></div>
          </div>

          <div className="h-20 bg-muted rounded"></div>
          <div className="h-10 bg-muted rounded"></div>

        </div>

        {/* Right */}
        <div className="h-64 bg-muted rounded-2xl"></div>

      </div>
    </div>
  )
}