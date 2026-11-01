import { useCallback, useEffect, useRef } from 'react';
import { Text, TouchableOpacity, useWindowDimensions, View } from 'react-native';

import { AnswerChip, type AnswerChipState } from '@/components/game/AnswerChip';
import {
  MathExpression,
  type MathDisplayMode,
  type SlotPosition,
} from '@/components/game/MathExpression';
import { MathSlot } from '@/components/game/MathSlot';
import { REWARD } from '@/lib/mathQuestion';
import type { MathQuestion, QuizStatus } from '@/types/game';

export type ClassroomBoardProps = {
  question: MathQuestion;
  status: QuizStatus;
  selectedIndex: number | null;
  onSelect: (index: number) => void;
  onNext: () => void;
  onRetry: () => void;
  displayMode?: MathDisplayMode;
  slotPosition?: SlotPosition;
  showNumbers?: boolean;
};

type SlotRect = {
  x: number;
  y: number;
  width: number;
  height: number;
};

function BoardAction({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <TouchableOpacity
      accessibilityRole="button"
      activeOpacity={0.8}
      className="rounded-full bg-emerald-100 px-3 py-1.5"
      onPress={onPress}
    >
      <Text className="text-xs font-bold text-emerald-900">{label}</Text>
    </TouchableOpacity>
  );
}

export function ClassroomBoard({ question, status, selectedIndex, onSelect, onNext, onRetry, displayMode = 'standard', slotPosition = 'answer', showNumbers = false,
}: ClassroomBoardProps) {
  const slotRef = useRef<View>(null);
  const slotRect = useRef<SlotRect | null>(null);
  const { width, height } = useWindowDimensions();

  const isPlaying = status === 'playing';
  const selectedValue = selectedIndex === null ? null : question.options[selectedIndex];
  const correctValue = question.options[question.correctIndex];
  const scale = Math.min(1, Math.max(0.78, Math.min(width / 760, height / 380)));

  const measureSlot = useCallback(() => {
    slotRef.current?.measureInWindow((x, y, slotWidth, slotHeight) => {
      slotRect.current = { x, y, width: slotWidth, height: slotHeight };
    });
  }, []);

  useEffect(() => {
    const frame = requestAnimationFrame(measureSlot);
    return () => cancelAnimationFrame(frame);
  }, [measureSlot, question.id, width, height]);

  function selectValue(value: number) {
    if (!isPlaying) {
      return;
    }
    const index = question.options.indexOf(value);
    if (index >= 0) {
      onSelect(index);
    }
  }

  function handleDrop(value: number, x: number, y: number) {
    if (!isPlaying) {
      return;
    }

    const rect = slotRect.current;
    if (!rect) {
      return;
    }

    const padding = 14;
    const inside =
      x >= rect.x - padding &&
      x <= rect.x + rect.width + padding &&
      y >= rect.y - padding &&
      y <= rect.y + rect.height + padding;

    if (inside) {
      selectValue(value);
    }
  }

  function chipState(index: number): AnswerChipState {
    if (status === 'correct' && index === question.correctIndex) {
      return 'correct';
    }

    if (status === 'wrong') {
      if (index === selectedIndex) {
        return 'wrong';
      }
      if (index === question.correctIndex) {
        return 'correct';
      }
    }

    return 'default';
  }

  return (
    <View className="flex-1 rounded-[30px] bg-amber-800 p-2 shadow-2xl">
      <View className="flex-1 overflow-hidden rounded-[24px] border-2 border-amber-950/30 bg-emerald-900 px-4 py-3">
        <MathExpression
          displayMode={displayMode}
          emojiScale={scale}
          question={question}
          showNumbers={showNumbers}
          slot={
            <MathSlot
              ref={slotRef}
              onLayout={measureSlot}
              status={status}
              value={selectedValue}
            />
          }
          slotPosition={slotPosition}
        />

        {/* Các thẻ lựa chọn đáp án */}
        <View className="mt-2 flex-row flex-wrap justify-center gap-6">
          {question.options.map((option, index) => (
            <AnswerChip
              disabled={!isPlaying}
              key={`${question.id}-${index}`}
              onDragStart={measureSlot}
              onDrop={handleDrop}
              onSelect={selectValue}
              state={chipState(index)}
              value={option}
            />
          ))}
        </View>

        {/* Thông báo kết quả và nút điều hướng */}
        <View className="mt-2 min-h-[40px] items-center justify-center">
          {status === 'correct' ? (
            <View className="flex-row flex-wrap items-center justify-center gap-2">
              <Text className="text-sm font-bold text-emerald-200">
                Chính xác! +{REWARD.points} điểm · +{REWARD.stars} sao
              </Text>
              <BoardAction label="Câu tiếp theo" onPress={onNext} />
            </View>
          ) : status === 'wrong' ? (
            <View className="flex-row flex-wrap items-center justify-center gap-2">
              <Text className="text-sm font-bold text-rose-200">
                Chưa đúng. Đáp án đúng là {correctValue}.
              </Text>
              <BoardAction label="Thử lại" onPress={onRetry} />
            </View>
          ) : (
            <Text className="text-center text-sm font-semibold text-emerald-100/70">
              Kéo số vào ô trống hoặc chạm để chọn
            </Text>
          )}
        </View>
      </View>
    </View>
  );
}
