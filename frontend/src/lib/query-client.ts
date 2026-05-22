import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
})

export const queryKeys = {
  me: ['me'] as const,
  health: ['health'] as const,
  requests: {
    all: ['requests'] as const,
    summary: ['requests', 'summary'] as const,
    list: (page: number, pageSize: number) => ['requests', 'list', page, pageSize] as const,
    detail: (id: string) => ['requests', 'detail', id] as const,
    history: (id: string) => ['requests', 'history', id] as const,
    attachments: (id: string) => ['requests', 'attachments', id] as const,
  },
  sponsorshipTypes: {
    list: ['sponsorship-types', 'list'] as const,
  },
}
