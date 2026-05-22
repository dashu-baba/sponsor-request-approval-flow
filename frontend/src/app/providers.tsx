import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { Toaster } from 'sonner'

import { router } from '@/app/router'
import { AuthProvider } from '@/features/auth/auth-provider'
import { queryClient } from '@/lib/query-client'

export function AppProviders() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
        <Toaster position="bottom-right" richColors closeButton duration={3000} />
      </AuthProvider>
    </QueryClientProvider>
  )
}
