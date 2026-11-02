import { create } from 'zustand';

import { REWARD } from '@/lib/mathQuestion';
import type { SessionBonus } from '@/types/game';

type GameState = {
  bonusByStudentId: Record<number, SessionBonus>;
  addReward: (studentId: number) => void;
};

export const useGameStore = create<GameState>((set) => ({
  bonusByStudentId: {},

  addReward: (studentId) =>
    set((state) => {
      const current = state.bonusByStudentId[studentId] ?? { points: 0, stars: 0 };
      return {
        bonusByStudentId: {
          ...state.bonusByStudentId,
          [studentId]: {
            points: current.points + REWARD.points,
            stars: current.stars + REWARD.stars,
          },
        },
      };
    }),
}));
