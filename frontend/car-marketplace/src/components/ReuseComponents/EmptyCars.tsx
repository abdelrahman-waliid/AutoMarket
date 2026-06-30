export default function EmptyCars() {
  return (
    <div className="flex flex-col items-center justify-center mt-16 gap-4 text-center">

      <div className="bg-blue-50 p-6 rounded-full">
        <svg
          className="w-10 h-10 text-blue-600"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M3 13h18M5 13l1-5h12l1 5M6 13v4a1 1 0 001 1h1m8-5v4a1 1 0 01-1 1h-1"
          />
        </svg>
      </div>

      <h2 className="text-xl font-semibold text-gray-800">
        No Cars Found
      </h2>

      <p className="text-gray-500 max-w-md">
        We couldn’t find any cars matching your filters.
        Try adjusting the filters or expanding your search.
      </p>

    </div>
  )
}