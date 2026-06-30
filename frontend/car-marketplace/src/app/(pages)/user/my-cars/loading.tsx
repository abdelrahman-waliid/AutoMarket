export default function Loading() {
  return (
    <div className="container mx-auto px-4 py-8">

      {/* HEADER */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <div className="h-6 w-32 bg-muted rounded animate-pulse mb-2" />
          <div className="h-4 w-48 bg-muted rounded animate-pulse" />
        </div>

        <div className="h-10 w-32 bg-muted rounded animate-pulse" />
      </div>

      {/* TABLE */}
      <div className="bg-card rounded-xl border shadow-sm p-6">

        <div className="h-5 w-40 bg-muted rounded animate-pulse mb-4" />

        {[...Array(5)].map((_, i) => (
          <div
            key={i}
            className="flex items-center justify-between border-b py-4"
          >
            {/* LEFT */}
            <div className="flex items-center gap-4">
              <div className="w-20 h-16 bg-muted rounded-lg animate-pulse" />

              <div>
                <div className="h-4 w-32 bg-muted rounded animate-pulse mb-2" />
                <div className="h-3 w-24 bg-muted rounded animate-pulse" />
              </div>
            </div>

            {/* PRICE */}
            <div className="h-4 w-20 bg-muted rounded animate-pulse" />

            {/* STATUS */}
            <div className="h-6 w-16 bg-muted rounded-full animate-pulse" />

            {/* ACTIONS */}
            <div className="h-4 w-16 bg-muted rounded animate-pulse" />
          </div>
        ))}

      </div>

    </div>
  )
}