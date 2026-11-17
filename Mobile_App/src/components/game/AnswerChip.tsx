import { useEffect } from 'react';
import { Text, View } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import Animated, {
  runOnJS,
  useAnimatedStyle,
  useSharedValue,
  withDelay,
  withSequence,
  withSpring,
  withTiming,
} from 'react-native-reanimated';

export type AnswerChipState = 'default' | 'correct' | 'wrong';

type AnswerChipProps = {
  value: number;
  index?: number;
  state?: AnswerChipState;
  disabled?: boolean;
  onSelect: (value: number) => void;
  onDrop?: (value: number, x: number, y: number) => void;
  onDragStart?: () => void;
};

const SPRING = {
  damping: 16,
  mass: 0.6,
  stiffness: 240,
};

// Bảng màu 3D nổi bật, vui nhộn cho trẻ em
const THEMES = [
  // 0: Vàng hổ phách / Cam mật ong (ấm áp, mời gọi)
  {
    bg: 'bg-amber-400',
    border: 'border-amber-300',
    borderBottom: 'border-b-amber-600',
    text: 'text-amber-950',
    shadow: '#f59e0b',
  },
  // 1: Xanh da trời / Sky (tươi sáng)
  {
    bg: 'bg-sky-400',
    border: 'border-sky-300',
    borderBottom: 'border-b-sky-600',
    text: 'text-sky-950',
    shadow: '#0ea5e9',
  },
  // 2: Tím kẹo ngọt (ngọt ngào)
  {
    bg: 'bg-purple-400',
    border: 'border-purple-300',
    borderBottom: 'border-b-purple-600',
    text: 'text-purple-950',
    shadow: '#a855f7',
  },
  // 3: Hồng dâu tây
  {
    bg: 'bg-rose-400',
    border: 'border-rose-300',
    borderBottom: 'border-b-rose-600',
    text: 'text-rose-950',
    shadow: '#f43f5e',
  },
];

const CORRECT_THEME = {
  bg: 'bg-emerald-400',
  border: 'border-emerald-300',
  borderBottom: 'border-b-emerald-600',
  text: 'text-emerald-950',
  shadow: '#10b981',
};

const WRONG_THEME = {
  bg: 'bg-rose-500',
  border: 'border-rose-400',
  borderBottom: 'border-b-rose-700',
  text: 'text-white',
  shadow: '#ef4444',
};

export function AnswerChip({
  value,
  index = 0,
  state = 'default',
  disabled = false,
  onSelect,
  onDrop,
  onDragStart,
}: AnswerChipProps) {
  const translateX = useSharedValue(0);
  const translateY = useSharedValue(0);
  const scale = useSharedValue(0);
  const rotate = useSharedValue(0);
  const isDragging = useSharedValue(false);

  const pan = Gesture.Pan()
    .enabled(!disabled)
    .minDistance(6)
    .onBegin(() => {
      isDragging.value = true;
      scale.value = withSpring(1.18, SPRING);
      rotate.value = withSpring(index % 2 === 0 ? 6 : -6, SPRING);
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
      rotate.value = withSpring(0, SPRING);
      translateX.value = withSpring(0, SPRING);
      translateY.value = withSpring(0, SPRING);
    });

  const tap = Gesture.Tap()
    .enabled(!disabled)
    .maxDistance(12)
    .maxDuration(350)
    .onBegin(() => {
      scale.value = withTiming(0.92, { duration: 70 });
    })
    .onEnd((_event, success) => {
      scale.value = withSpring(1, SPRING);
      if (success) {
        runOnJS(onSelect)(value);
      }
    })
    .onFinalize(() => {
      scale.value = withSpring(1, SPRING);
    });

  // Hiệu ứng nảy xuất hiện và phản hồi trạng thái
  useEffect(() => {
    if (state === 'correct') {
      scale.value = withSequence(
        withTiming(1.25, { duration: 150 }),
        withTiming(0.95, { duration: 100 }),
        withSpring(1.15, { damping: 8, stiffness: 220 }),
        withSpring(1, SPRING),
      );
      rotate.value = withSequence(
        withTiming(-8, { duration: 90 }),
        withTiming(8, { duration: 90 }),
        withTiming(-4, { duration: 70 }),
        withTiming(0, { duration: 70 }),
      );
    } else if (state === 'wrong') {
      translateX.value = withSequence(
        withTiming(-12, { duration: 50 }),
        withTiming(12, { duration: 50 }),
        withTiming(-10, { duration: 50 }),
        withTiming(10, { duration: 50 }),
        withTiming(-5, { duration: 50 }),
        withTiming(0, { duration: 50 }),
      );
      rotate.value = withSequence(
        withTiming(-6, { duration: 60 }),
        withTiming(6, { duration: 60 }),
        withTiming(-3, { duration: 60 }),
        withTiming(0, { duration: 60 }),
      );
    } else {
      scale.value = 0;
      rotate.value = 0;
      translateX.value = 0;
      translateY.value = 0;

      scale.value = withDelay(
        index * 70,
        withSpring(1, {
          damping: 12,
          stiffness: 200,
        }),
      );
    }
  }, [index, rotate, scale, state, translateX, translateY, value]);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [
      { translateX: translateX.value },
      { translateY: translateY.value },
      { scale: scale.value },
      { rotate: `${rotate.value}deg` },
    ],
    zIndex: isDragging.value ? 50 : 1,
    elevation: isDragging.value ? 16 : 4,
  }));

  const theme =
    state === 'correct'
      ? CORRECT_THEME
      : state === 'wrong'
        ? WRONG_THEME
        : THEMES[Math.abs(index) % THEMES.length];

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
        style={[
          {
            width: 74,
            height: 56,
            shadowColor: theme.shadow,
            shadowOffset: { width: 0, height: 4 },
            shadowOpacity: 0.35,
            shadowRadius: 6,
          },
          animatedStyle,
        ]}
      >
        <View
          className={`relative h-full w-full items-center justify-center rounded-2xl border-t-2 border-l-2 border-r-2 border-b-4 ${theme.bg} ${theme.border} ${theme.borderBottom} ${dimmed ? 'opacity-50' : 'opacity-100'
            }`}
        >
          {/* Lớp phản chiếu ánh sáng bóng bẩy trên đỉnh chip */}
          {/* <View
            pointerEvents="none"
            className="absolute top-1.5 left-2 right-2 h-2 rounded-full bg-white/35"
          /> */}

          {/* Số đáp án */}
          <Text className={`text-2xl font-black tracking-wider ${theme.text}`}>
            {value}
          </Text>
        </View>
      </Animated.View>
    </GestureDetector>
  );
}
