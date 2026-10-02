import * as SecureStore from 'expo-secure-store';

import { SESSION_STORAGE_MODE } from '@/config/env';
import type { StoredSession } from '@/types/auth';

const SESSION_KEY = 'toanTuDuy.session';

const localStorageAdapter = {
  async getItem(key: string): Promise<string | null> {
    try {
      return globalThis.localStorage?.getItem(key) ?? null;
    } catch {
      return null;
    }
  },
  async setItem(key: string, value: string): Promise<void> {
    try {
      globalThis.localStorage?.setItem(key, value);
    } catch {
      // Bỏ qua: phiên sẽ không được lưu qua lần app reload.
    }
  },
  async deleteItem(key: string): Promise<void> {
    try {
      globalThis.localStorage?.removeItem(key);
    } catch {
      // Bỏ qua.
    }
  },
};

const secureStoreAdapter = {
  async getItem(key: string): Promise<string | null> {
    return SecureStore.getItemAsync(key);
  },
  async setItem(key: string, value: string): Promise<void> {
    await SecureStore.setItemAsync(key, value, {
      keychainAccessible: SecureStore.WHEN_UNLOCKED,
    });
  },
  async deleteItem(key: string): Promise<void> {
    await SecureStore.deleteItemAsync(key);
  },
};

const adapter = SESSION_STORAGE_MODE === 'local' ? localStorageAdapter : secureStoreAdapter;

export async function saveSession(session: StoredSession) {
  await adapter.setItem(SESSION_KEY, JSON.stringify(session));
}

export async function loadSession(): Promise<StoredSession | null> {
  const raw = await adapter.getItem(SESSION_KEY);

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    await clearSession();
    return null;
  }
}

export async function clearSession() {
  await adapter.deleteItem(SESSION_KEY);
}
