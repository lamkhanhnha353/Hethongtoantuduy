import { useAudioStore } from '@/store/audioStore';

import { playSound } from './audioEngine';
import type { SoundKey } from './catalog';

export type { SoundKey } from './catalog';

export const audio = {
  play: (sound: SoundKey) => playSound(sound),
  isEnabled: () => useAudioStore.getState().enabled,
  setEnabled: (enabled: boolean) => useAudioStore.getState().setEnabled(enabled),
};
