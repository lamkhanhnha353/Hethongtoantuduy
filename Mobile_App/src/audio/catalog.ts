import type { AudioSource } from 'expo-audio';

type SoundDefinition = {
  source: AudioSource;
  volume: number;
};

export const SOUND_CATALOG = {
  correct: {
    source: require('../../assets/audio/correct.mp3'),
    volume: 0.9,
  },
  wrong: {
    source: require('../../assets/audio/error.mp3'),
    volume: 0.8,
  },
} satisfies Record<string, SoundDefinition>;

export type SoundKey = keyof typeof SOUND_CATALOG;
