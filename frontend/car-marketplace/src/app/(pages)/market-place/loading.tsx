export default function Loading() {
  return (
    <div className="container mx-auto px-4 py-6 animate-pulse">

      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4 mb-6">

        <div className="space-y-2">
          <div className="h-7 w-48 bg-muted rounded"></div>
          <div className="h-4 w-72 bg-muted rounded"></div>
        </div>

        <div className="flex gap-2">
          <div className="h-10 w-64 bg-muted rounded-lg"></div>
          <div className="h-10 w-10 bg-muted rounded-lg"></div>
        </div>

      </div>

      <div className="flex gap-6">

        {/* Filters Sidebar */}
        <aside className="hidden lg:block w-64 bg-card rounded-xl shadow-sm p-5 space-y-4">

          <div className="h-5 w-20 bg-muted rounded"></div>

          <div className="space-y-3">
            <div className="h-4 w-16 bg-muted rounded"></div>
            <div className="h-9 bg-muted rounded"></div>
          </div>

          <div className="space-y-3">
            <div className="h-4 w-24 bg-muted rounded"></div>
            <div className="flex gap-2">
              <div className="h-9 w-full bg-muted rounded"></div>
              <div className="h-9 w-full bg-muted rounded"></div>
            </div>
          </div>

          <div className="space-y-3">
            <div className="h-4 w-24 bg-muted rounded"></div>
            <div className="flex gap-2">
              <div className="h-9 w-full bg-muted rounded"></div>
              <div className="h-9 w-full bg-muted rounded"></div>
            </div>
          </div>

          <div className="h-10 bg-muted rounded-lg"></div>

          <div className="h-4 w-16 bg-muted rounded mx-auto"></div>

        </aside>

        {/* Cars Grid */}
        <main className="flex-1 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">

          {[1,2,3,4,5,6].map((i)=>(
            <div
              key={i}
              className="bg-card border border-border rounded-2xl shadow-sm p-4 space-y-4"
            >

              {/* Image */}
              <div className="h-48 bg-muted rounded-xl"></div>

              {/* Title */}
              <div className="h-4 bg-muted rounded w-3/4"></div>

              {/* Subtitle */}
              <div className="h-3 bg-muted rounded w-1/2"></div>

              {/* Price */}
              <div className="h-5 bg-muted rounded w-1/3"></div>

              {/* Button */}
              <div className="h-10 bg-muted rounded-xl"></div>

            </div>
          ))}

        </main>

      </div>

    </div>
  )
}