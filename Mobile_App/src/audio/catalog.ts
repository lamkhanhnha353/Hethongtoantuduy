import type { AudioSource } from 'expo-audio';

export type SoundChannel = 'ui' | 'feedback';
export type SoundInterrupt = 'duck' | 'stop';

export type SoundDefinition = {
  source: AudioSource;
  volume: number;
  channel: SoundChannel;
  priority: number;
  interrupt?: SoundInterrupt;
  duckOthers?: number;
};

export const SOUND_CATALOG = {
  pop: {
    source: require('../../assets/audio/pop.mp3'),
    volume: 0.5,
    channel: 'ui',
    priority: 10,
  },
  correct: {
    source: require('../../assets/audio/correct.mp3'),
    volume: 0.7,
    channel: 'feedback',
    priority: 100,
    interrupt: 'duck',
    duckOthers: 0.25,
  },
  wrong: {
    source: require('../../assets/audio/error.mp3'),
    volume: 0.6,
    channel: 'feedback',
    priority: 100,
    interrupt: 'duck',
    duckOthers: 0.25,
  },
} satisfies Record<string, SoundDefinition>;

export type SoundKey = keyof typeof SOUND_CATALOG;

export function getSoundDefinition(key: SoundKey): SoundDefinition {
  return SOUND_CATALOG[key];
}
