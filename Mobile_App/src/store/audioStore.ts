import { create } from 'zustand';

import { loadAudioSettings, saveAudioSettings } from '@/services/audioSettingsStorage';

type AudioState = {
  enabled: boolean;
  isHydrated: boolean;
  hydrate: () => Promise<void>;
  setEnabled: (enabled: boolean) => Promise<void>;
};

export const useAudioStore = create<AudioState>(
  (set) => ({
  enabled: true,
  isHydrated: false,

  hydrate: async () => {
    const settings = await loadAudioSettings();
    set({ enabled: settings?.enabled ?? true, isHydrated: true });
  },

  setEnabled: async (enabled) => {
    set({ enabled });
    await saveAudioSettings({ enabled });
  },
}));
