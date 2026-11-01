import { useEffect } from 'react';

import { useAudioStore } from '@/store/audioStore';

export function useAudioHydration() {
  const hydrate = useAudioStore((state) => state.hydrate);
  const isHydrated = useAudioStore((state) => state.isHydrated);

  useEffect(() => {
    if (isHydrated) {
      return;
    }

    void hydrate();
  }, [hydrate, isHydrated]);
}

export function useSoundEnabled(): {
  enabled: boolean;
  setEnabled: (enabled: boolean) => Promise<void>;
} {
  const enabled = useAudioStore((state) => state.enabled);
  const setEnabled = useAudioStore((state) => state.setEnabled);

  return { enabled, setEnabled };
}
