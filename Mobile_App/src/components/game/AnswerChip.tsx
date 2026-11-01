import { Text, View } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import Animated, {
  runOnJS,
  useAnimatedStyle,
  useSharedValue,
  withSpring,
} from 'react-native-reanimated';

export type AnswerChipState = 'default' | 'correct' | 'wrong';

type AnswerChipProps = {
  value: number;
  state?: AnswerChipState;
  disabled?: boolean;
  onSelect: (value: number) => void;
  onDrop?: (value: number, x: number, y: number) => void;
  onDragStart?: () => void;
};

const SPRING = {
  damping: 18,
  mass: 0.6,
  stiffness: 220,
};

const STATE_CLASS: Record<AnswerChipState, string> = {
  default: 'border-emerald-100/40 bg-emerald-800',
  correct: 'border-emerald-300 bg-emerald-500/40',
  wrong: 'border-rose-300 bg-rose-500/40',
};

export function AnswerChip({
  value,
  state = 'default',
  disabled = false,
  onSelect,
  onDrop,
  onDragStart,
}: AnswerChipProps) {
  const translateX = useSharedValue(0);
  const translateY = useSharedValue(0);
  const scale = useSharedValue(1);
  const isDragging = useSharedValue(false);

  const pan = Gesture.Pan()
    .enabled(!disabled)
    .minDistance(6)
    .onBegin(() => {
      isDragging.value = true;
      scale.value = withSpring(1.08, SPRING);
      if (onDragStart !== undefined) {
        runOnJS(onDragStart)();
      }
    })
    .onUpdate((event) => {
      translateX.value = event.translationX;
      translateY.value = event.translationY;
    })
    .onEnd((event) => {
      if (onDrop !== undefined) {
        runOnJS(onDrop)(value, event.absoluteX, event.absoluteY);
      }
    })
    .onFinalize(() => {
      isDragging.value = false;
      scale.value = withSpring(1, SPRING);
      translateX.value = withSpring(0, SPRING);
      translateY.value = withSpring(0, SPRING);
    });

  const tap = Gesture.Tap()
    .enabled(!disabled)
    .maxDistance(12)
    .maxDuration(350)
    .onEnd((_event, success) => {
      if (success) {
        runOnJS(onSelect)(value);
      }
    });

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [
      { translateX: translateX.value },
      { translateY: translateY.value },
      { scale: scale.value },
    ],
    zIndex: isDragging.value ? 20 : 0,
    elevation: isDragging.value ? 12 : 0,
  }));

  const dimmed = disabled && state === 'default';

  return (
    <GestureDetector gesture={Gesture.Exclusive(pan, tap)}>
      <Animated.View
        accessibilityHint="Kéo vào ô kết quả hoặc chạm để chọn"
        accessibilityLabel={`Đáp án ${value}`}
        accessibilityRole="button"
        accessibilityState={{ disabled }}
        onAccessibilityTap={() => {
          if (!disabled) {
            onSelect(value);
          }
        }}
        style={[{ width: 64, height: 48 }, animatedStyle]}
      >
        <View
          className={`h-full w-full items-center justify-center rounded-2xl border-2 ${STATE_CLASS[state]} ${
            dimmed ? 'opacity-60' : ''
          }`}
        >
          <Text
            className={`text-2xl font-black ${
              state === 'wrong' ? 'text-rose-50' : 'text-emerald-50'
            }`}
          >
            {value}
          </Text>
        </View>
      </Animated.View>
    </GestureDetector>
  );
}
