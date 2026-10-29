import { createAudioPlayer, preload, setAudioModeAsync, type AudioPlayer } from 'expo-audio';

import { useAudioStore } from '@/store/audioStore';

import { SOUND_CATALOG, type SoundKey } from './catalog';

const POOL_SIZE = 2;

type AudioEngine = {
  voices: Partial<Record<SoundKey, AudioPlayer[]>>;
  cursor: Partial<Record<SoundKey, number>>;
  ready: Promise<void>;
};

const globalScope = globalThis as typeof globalThis & {
  __toanTuDuyAudioEngine?: AudioEngine;
};

function pauseAll(engine: AudioEngine) {
  for (const voices of Object.values(engine.voices)) {
    voices?.forEach((player) => {
      player.pause();
    });
  }
}

function createEngine(): AudioEngine {
  const engine: AudioEngine = {
    voices: {},
    cursor: {},
    ready: setAudioModeAsync({
      playsInSilentMode: true,
      shouldPlayInBackground: false,
      interruptionMode: 'mixWithOthers',
    }).catch(() => undefined),
  };

  const entries = Object.entries(SOUND_CATALOG) as [
    SoundKey,
    (typeof SOUND_CATALOG)[SoundKey],
  ][];

  for (const [key, definition] of entries) {
    void preload(definition.source).catch(() => undefined);

    engine.voices[key] = Array.from({ length: POOL_SIZE }, () => {
      const player = createAudioPlayer(definition.source);
      player.volume = definition.volume;
      return player;
    });
  }

  useAudioStore.subscribe((state, previous) => {
    if (state.enabled === previous.enabled) {
      return;
    }

    if (!state.enabled) {
      pauseAll(engine);
    }
  });

  return engine;
}

function getEngine(): AudioEngine {
  if (!globalScope.__toanTuDuyAudioEngine) {
    globalScope.__toanTuDuyAudioEngine = createEngine();
  }

  return globalScope.__toanTuDuyAudioEngine;
}

export function playSound(key: SoundKey) {
  if (!useAudioStore.getState().enabled) {
    return;
  }

  const engine = getEngine();
  const voices = engine.voices[key];

  if (!voices || voices.length === 0) {
    return;
  }

  const cursor = engine.cursor[key] ?? 0;
  const player = voices[cursor % voices.length];
  engine.cursor[key] = (cursor + 1) % voices.length;

  void engine.ready.then(() => {
    if (!useAudioStore.getState().enabled) {
      return;
    }

    void player
      .seekTo(0)
      .catch(() => undefined)
      .then(() => {
        player.play();
      });
  });
}

// Khởi tạo sớm để preload nguồn và cấu hình audio session trước lần phát đầu tiên.
getEngine();

export function releaseAudioEngine() {
  const engine = globalScope.__toanTuDuyAudioEngine;

  if (!engine) {
    return;
  }

  for (const voices of Object.values(engine.voices)) {
    voices?.forEach((player) => {
      player.remove();
    });
  }

  delete globalScope.__toanTuDuyAudioEngine;
}
