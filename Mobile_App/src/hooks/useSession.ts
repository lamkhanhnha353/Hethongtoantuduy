import { useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { loadSession } from '@/services/tokenStorage';
import { useAuthStore } from '@/store/authStore';
import type { StoredSession } from '@/types/auth';

export const sessionQueryKey = ['session'] as const;

export function useSessionQuery() {
  return useQuery<StoredSession | null>({
    queryKey: sessionQueryKey,
    queryFn: loadSession,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}

export function useClearSessionMutation() {
  const queryClient = useQueryClient();
  const logout = useAuthStore((state) => state.logout);

  return async function clear() {
    await logout();
    queryClient.setQueryData(sessionQueryKey, null);
  };
}
export function useAuthHydration() {
  const hydrate = useAuthStore((state) => state.hydrate);
  const isHydrated = useAuthStore((state) => state.isHydrated);

  useEffect(() => {
    if (isHydrated) {
      return;
    }

    void hydrate();
  }, [hydrate, isHydrated]);
}
