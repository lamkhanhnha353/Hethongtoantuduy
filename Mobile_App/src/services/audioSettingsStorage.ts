import * as SecureStore from 'expo-secure-store';

import { SESSION_STORAGE_MODE } from '@/config/env';

const AUDIO_SETTINGS_KEY = 'toanTuDuy.audio';

export type StoredAudioSettings = {
  enabled: boolean;
};

type KeyValueAdapter = {
  getItem(key: string): Promise<string | null>;
  setItem(key: string, value: string): Promise<void>;
};

const localStorageAdapter: KeyValueAdapter = {
  async getItem(key) {
    try {
      return globalThis.localStorage?.getItem(key) ?? null;
    } catch {
      return null;
    }
  },
  async setItem(key, value) {
    try {
      globalThis.localStorage?.setItem(key, value);
    } catch {
      // Bỏ qua: cài đặt sẽ không được lưu qua lần app reload.
    }
  },
};

const secureStoreAdapter: KeyValueAdapter = {
  async getItem(key) {
    return SecureStore.getItemAsync(key);
  },
  async setItem(key, value) {
    await SecureStore.setItemAsync(key, value, {
      keychainAccessible: SecureStore.WHEN_UNLOCKED,
    });
  },
};

const adapter = SESSION_STORAGE_MODE === 'local' ? localStorageAdapter : secureStoreAdapter;

export async function saveAudioSettings(settings: StoredAudioSettings): Promise<void> {
  await adapter.setItem(AUDIO_SETTINGS_KEY, JSON.stringify(settings));
}

export async function loadAudioSettings(): Promise<StoredAudioSettings | null> {
  const raw = await adapter.getItem(AUDIO_SETTINGS_KEY);

  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<StoredAudioSettings>;

    if (typeof parsed.enabled !== 'boolean') {
      return null;
    }

    return { enabled: parsed.enabled };
  } catch {
    return null;
  }
}
