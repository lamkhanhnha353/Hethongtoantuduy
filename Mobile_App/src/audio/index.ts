import { useAudioStore } from '@/store/audioStore';

import { playSound } from './audioEngine';
import { SOUND_CATALOG, type SoundKey } from './catalog';

export type { SoundKey } from './catalog';

type SoundShortcuts = {
  [Key in SoundKey]: () => void;
};

const shortcuts = (Object.keys(SOUND_CATALOG) as SoundKey[]).reduce(
  (list, key) => {
    list[key] = () => playSound(key);
    return list;
  },
  {} as SoundShortcuts,
);

export const audio = {
  ...shortcuts,
  play: playSound,
  isEnabled: () => useAudioStore.getState().enabled,
  setEnabled: (enabled: boolean) => useAudioStore.getState().setEnabled(enabled),
};
