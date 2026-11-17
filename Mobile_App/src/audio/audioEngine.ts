import { createAudioPlayer, preload, setAudioModeAsync, type AudioPlayer } from 'expo-audio';
import { Platform } from 'react-native';

import { useAudioStore } from '@/store/audioStore';

import { getSoundDefinition, SOUND_CATALOG, type SoundInterrupt, type SoundKey,} from './catalog';

const POOL_SIZE = 2;
const DEFAULT_INTERRUPT: SoundInterrupt = 'duck';
const DEFAULT_DUCK_FACTOR = 0.25;

type ActiveSound = {
  player: AudioPlayer;
  priority: number;
  baseVolume: number;
  interrupt: SoundInterrupt;
  duckOthers: number;
};

type AudioEngine = {
  voices: Partial<Record<SoundKey, AudioPlayer[]>>;
  cursor: Partial<Record<SoundKey, number>>;
  active: Map<AudioPlayer, ActiveSound>;
  ready: Promise<void>;
};

const globalScope = globalThis as typeof globalThis & {
  __toanTuDuyAudioEngine?: AudioEngine;
};

/**
 * Tính lại volume cho các voice đang phát: voice priority thấp hơn sẽ bị duck
 * bởi voice priority cao hơn nếu voice đó dùng interrupt 'duck'.
 */
function applyVolumes(engine: AudioEngine) {
  const sounds = [...engine.active.values()];

  for (const sound of sounds) {
    let factor = 1;

    for (const other of sounds) {
      if (other === sound || other.priority <= sound.priority) {
        continue;
      }

      if (other.interrupt === 'duck') {
        factor = Math.min(factor, other.duckOthers);
      }
    }

    sound.player.volume = sound.baseVolume * factor;
  }
}

/**
 * Voice priority thấp hơn bị pause ngay khi voice mới dùng interrupt 'stop'.
 */
function stopLowerPriority(engine: AudioEngine, incoming: ActiveSound) {
  if (incoming.interrupt !== 'stop') {
    return;
  }

  for (const [player, sound] of engine.active) {
    if (sound.priority >= incoming.priority) {
      continue;
    }

    player.pause();
    player.volume = sound.baseVolume;
    engine.active.delete(player);
  }
}

function pauseAll(engine: AudioEngine) {
  for (const voices of Object.values(engine.voices)) {
    voices?.forEach((player) => {
      player.pause();
    });
  }

  for (const sound of engine.active.values()) {
    sound.player.volume = sound.baseVolume;
  }

  engine.active.clear();
}

function createEngine(): AudioEngine {
  const engine: AudioEngine = {
    voices: {},
    cursor: {},
    active: new Map(),
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

      player.addListener('playbackStatusUpdate', (status) => {
        if (!status.didJustFinish) {
          return;
        }

        engine.active.delete(player);
        applyVolumes(engine);
      });

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

if (Platform.OS !== 'web' || typeof window !== 'undefined') {
  getEngine();
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

  const definition = getSoundDefinition(key);
  const interrupt = definition.interrupt ?? DEFAULT_INTERRUPT;
  const duckOthers = definition.duckOthers ?? DEFAULT_DUCK_FACTOR;

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
        if (!useAudioStore.getState().enabled) {
          return;
        }

        const sound: ActiveSound = {
          player,
          priority: definition.priority,
          baseVolume: definition.volume,
          interrupt,
          duckOthers,
        };

        stopLowerPriority(engine, sound);
        engine.active.set(player, sound);
        player.volume = sound.baseVolume;
        applyVolumes(engine);
        player.play();
      });
  });
}

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

  engine.active.clear();
  delete globalScope.__toanTuDuyAudioEngine;
}
