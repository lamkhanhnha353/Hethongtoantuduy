import { create } from 'zustand';
import { clearSession, loadSession, saveSession } from '@/services/tokenStorage';
import type { StoredSession } from '@/types/auth';

type AuthState = {
  session: StoredSession | null;
  isHydrated: boolean;
  setSession: (session: StoredSession) => Promise<void>;
  logout: () => Promise<void>;
  hydrate: () => Promise<void>;
};

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  isHydrated: false,

  hydrate: async () => {
    const session = await loadSession();
    set({ session, isHydrated: true });
  },

  setSession: async (session) => {
    await saveSession(session);
    set({ session });
  },

  logout: async () => {
    await clearSession();
    set({ session: null });
  },
}));

export function getAccessToken(): string | null {
  return useAuthStore.getState().session?.accessToken ?? null;
}
