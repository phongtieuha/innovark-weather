import { useState, type ReactNode } from "react"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"

interface IAppLayoutProps {
  readonly children: ReactNode
}

export function AppLayout({ children }: IAppLayoutProps) {
  const [queryClient] = useState(() => new QueryClient())

  return (
    <QueryClientProvider client={queryClient}>
      <main className="mx-auto flex min-h-svh w-full max-w-lg flex-col justify-center px-4 py-10">
        {children}
      </main>
    </QueryClientProvider>
  )
}
