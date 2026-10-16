import { useEffect } from 'react';

import { useAuthStore } from '@/store/authStore';
import type { StoredSession } from '@/types/auth';

export function useSession(): { session: StoredSession | null; isHydrated: boolean } {
  const session = useAuthStore((state) => state.session);
  const isHydrated = useAuthStore((state) => state.isHydrated);

  return { session, isHydrated };
}

export function useLogout(): () => Promise<void> {
  return useAuthStore((state) => state.logout);
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
