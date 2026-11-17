import { useEffect, useRef, useState } from 'react';
import { router, useLocalSearchParams } from 'expo-router';
import * as ScreenOrientation from 'expo-screen-orientation';
import {
  ActivityIndicator,
  Alert,
  Animated,
  Easing,
  Keyboard,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AlertBanner } from '@/components/AlertBanner';
import { TimedSequenceAnswer } from '@/components/assignment/TimedSequenceAnswer';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import {
  useStudentPracticeAssignmentQuery,
  useSubmitStudentPracticeAssignmentMutation,
} from '@/hooks/useStudentPracticeAssignments';
import { toApiError } from '@/services/apiError';
import type { StudentPracticeQuestionDto } from '@/types/assignment';

const AUTO_NEXT_MS = 5000;

const C = {
  bg: '#f0f9ff',
  sky: '#0ea5e9',
  skyDark: '#0369a1',
  skySoft: '#e0f2fe',
  skyLine: '#bae6fd',
  green: '#10b981',
  greenDark: '#047857',
  greenSoft: '#d1fae5',
  indigo: '#4f46e5',
  indigoSoft: '#eef2ff',
  text: '#0f172a',
  slate: '#475569',
  slateLight: '#94a3b8',
  line: '#e2e8f0',
  white: '#ffffff',
};

/* ---------- Các kiểu đáp án ---------- */

function AnswerChoice({
  label,
  badge,
  selected,
  onPress,
  tone = 'sky',
}: {
  label: string;
  badge?: string;
  selected: boolean;
  onPress: () => void;
  tone?: 'sky' | 'green' | 'red';
}) {
  const accent = tone === 'green' ? C.green : tone === 'red' ? '#f43f5e' : C.sky;
  const soft = tone === 'green' ? C.greenSoft : tone === 'red' ? '#ffe4e6' : C.skySoft;

  return (
    <TouchableOpacity
      accessibilityRole="button"
      accessibilityState={{ selected }}
      activeOpacity={0.85}
      onPress={onPress}
      style={[
        styles.choice,
        { borderColor: selected ? accent : C.line, backgroundColor: selected ? soft : C.white },
      ]}
    >
      {badge ? (
        <View style={[styles.choiceBadge, { backgroundColor: selected ? accent : C.skySoft }]}>
          <Text style={[styles.choiceBadgeText, { color: selected ? C.white : C.skyDark }]}>
            {badge}
          </Text>
        </View>
      ) : null}
      <Text style={[styles.choiceText, { color: selected ? C.text : C.slate }]}>{label}</Text>
      {selected ? <Text style={[styles.check, { color: accent }]}>✓</Text> : null}
    </TouchableOpacity>
  );
}

function PracticeQuestion({
  question,
  value,
  onChange,
  onSubmit, // <-- Thêm onSubmit vào đây
}: {
  question: StudentPracticeQuestionDto;
  value: string;
  onChange: (answer: string) => void;
  onSubmit: () => void; // <-- Định nghĩa kiểu cho onSubmit
}) {
  if (question.type === 'MULTIPLE_CHOICE' && question.options.length > 0) {
    return (
      <View style={styles.answerGap}>
        {question.options.map((option) => (
          <AnswerChoice
            key={option.key}
            badge={option.key}
            label={option.text}
            selected={value === option.key}
            onPress={() => onChange(option.key)}
          />
        ))}
      </View>
    );
  }

  if (question.type === 'TRUE_FALSE') {
    return (
      <View style={styles.trueFalseRow}>
        <View style={styles.flex1}>
          <AnswerChoice
            label="Đúng"
            tone="green"
            selected={value === 'Đúng'}
            onPress={() => onChange('Đúng')}
          />
        </View>
        <View style={styles.flex1}>
          <AnswerChoice
            label="Sai"
            tone="red"
            selected={value === 'Sai'}
            onPress={() => onChange('Sai')}
          />
        </View>
      </View>
    );
  }

  if (question.type === 'TIMED_SEQUENCE') {
    return (
      <TimedSequenceAnswer 
        terms={question.sequenceTerms ?? []} 
        value={value} 
        onChange={onChange} 
        onSubmit={onSubmit} // <-- Truyền onSubmit xuống TimedSequenceAnswer
      />
    );
  }

  return (
    <TextInput
      accessibilityLabel="Câu trả lời"
      onChangeText={onChange}
      placeholder="Bé nhập đáp án vào đây"
      placeholderTextColor={C.slateLight}
      style={styles.textInput}
      value={value}
    />
  );
}

/* ---------- Màn hình chính ---------- */

export default function StudentAssignmentScreen() {
  const { assignmentId: assignmentIdParam, studentId: studentIdParam } =
    useLocalSearchParams<{ assignmentId?: string; studentId?: string }>();
  const assignmentId = Number(assignmentIdParam);
  const studentId = Number(studentIdParam);
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const [currentIndex, setCurrentIndex] = useState(0);

  const assignmentQuery = useStudentPracticeAssignmentQuery(studentId, assignmentId);
  const submitMutation = useSubmitStudentPracticeAssignmentMutation(studentId, assignmentId);
  const detail = assignmentQuery.data;
  const submitError = submitMutation.error ? toApiError(submitMutation.error) : null;
  const loadError = assignmentQuery.error ? toApiError(assignmentQuery.error) : null;

  const questions = detail?.questions ?? [];
  const index = Math.min(currentIndex, Math.max(questions.length - 1, 0));
  const question = questions[index];
  const isLast = questions.length > 0 && index === questions.length - 1;

  const isAnswered = (q: StudentPracticeQuestionDto) =>
    !!answers[q.questionAssignmentId]?.trim();
  const missingCount = questions.filter((q) => !isAnswered(q)).length;
  const allAnswered = questions.length > 0 && missingCount === 0;
  const currentAnswered = question ? isAnswered(question) : false;
  const progress = questions.length > 0 ? (index + 1) / questions.length : 0;

  // Luôn khóa màn hình dọc cho dễ nhìn
  useEffect(() => {
    if (!detail || Platform.OS === 'web') {
      return undefined;
    }

    void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.PORTRAIT_UP).catch(
      (error: unknown) => {
        console.error('Could not lock portrait orientation for the assignment screen.', error);
      },
    );

    return () => {
      void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.PORTRAIT_UP).catch(
        () => undefined,
      );
    };
  }, [detail]);

  // Tự động chuyển câu sau 5 giây kể từ lần chọn/nhập đáp án cuối cùng
  const autoTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [autoBar] = useState(() => new Animated.Value(0));
  const [autoNextForIndex, setAutoNextForIndex] = useState<number | null>(null);
  const autoNextActive = autoNextForIndex === index;

  function cancelAutoNext() {
    if (autoTimerRef.current !== null) {
      clearTimeout(autoTimerRef.current);
      autoTimerRef.current = null;
    }
    autoBar.stopAnimation();
    autoBar.setValue(0);
    setAutoNextForIndex(null);
  }

  function scheduleAutoNext(fromIndex: number) {
    cancelAutoNext();
    autoBar.setValue(0);
    setAutoNextForIndex(fromIndex);
    Animated.timing(autoBar, {
      toValue: 1,
      duration: AUTO_NEXT_MS,
      easing: Easing.linear,
      useNativeDriver: false,
    }).start();
    autoTimerRef.current = setTimeout(() => {
      autoTimerRef.current = null;
      setAutoNextForIndex(null);
      Keyboard.dismiss();
      setCurrentIndex((current) => (current === fromIndex ? fromIndex + 1 : current));
    }, AUTO_NEXT_MS);
  }

  // Đổi câu (bấm ⬅, tự chuyển...) hoặc rời màn hình thì hủy đếm ngược
  useEffect(() => {
    if (autoTimerRef.current !== null) {
      clearTimeout(autoTimerRef.current);
      autoTimerRef.current = null;
    }
    autoBar.stopAnimation();
    autoBar.setValue(0);
    return () => {
      if (autoTimerRef.current !== null) {
        clearTimeout(autoTimerRef.current);
        autoTimerRef.current = null;
      }
      autoBar.stopAnimation();
    };
  }, [index, autoBar]);

  function handleSubmit() {
    if (!detail || !allAnswered || submitMutation.isPending) {
      return;
    }

    submitMutation.mutate({
      answers: detail.questions.map((q) => ({
        questionAssignmentId: q.questionAssignmentId,
        answer: answers[q.questionAssignmentId].trim(),
      })),
    });
  }

  // Chỉ dùng ở câu cuối: nộp bài (hoặc quay lại câu còn thiếu)
  function handleFinish() {
    if (!question || !currentAnswered || submitMutation.isPending) return;

    if (allAnswered) {
      handleSubmit();
      return;
    }
    const firstMissing = questions.findIndex((q) => !isAnswered(q));
    if (firstMissing >= 0) {
      cancelAutoNext();
      setCurrentIndex(firstMissing);
    }
  }

  function handlePrev() {
    if (index > 0 && !submitMutation.isPending) {
      cancelAutoNext();
      setCurrentIndex(index - 1);
    }
  }

  function handleExit() {
    cancelAutoNext();
    Alert.alert('Thoát bài tập?', 'Các câu bé đã làm sẽ không được lưu.', [
      { text: 'Làm tiếp', style: 'cancel' },
      { text: 'Thoát', style: 'destructive', onPress: () => router.back() },
    ]);
  }

  /* --- Đang tải / lỗi / không có câu hỏi --- */
  if (assignmentQuery.isPending || loadError || (detail && questions.length === 0)) {
    return (
      <SafeAreaView style={styles.screen} edges={['bottom']}>
        <ScrollView contentContainerStyle={styles.padded}>
          <ScreenHeader title="Bài tập được giao" />
          {assignmentQuery.isPending ? (
            <View style={styles.center}>
              <ActivityIndicator size="large" color={C.sky} />
              <Text style={styles.loadingText}>Đang chuẩn bị bài tập...</Text>
            </View>
          ) : null}
          {loadError ? (
            <View style={styles.errorBox}>
              <AlertBanner tone="error" message={loadError.message} />
              <PrimaryButton label="Quay lại" onPress={() => router.back()} variant="ghost" />
            </View>
          ) : null}
          {detail && questions.length === 0 ? (
            <View style={styles.errorBox}>
              <AlertBanner
                tone="error"
                message="Bài tập này hiện chưa có câu hỏi. Vui lòng báo giáo viên kiểm tra và giao lại bài."
              />
              <PrimaryButton label="Quay lại" onPress={() => router.back()} variant="ghost" />
            </View>
          ) : null}
        </ScrollView>
      </SafeAreaView>
    );
  }

  /* --- Đã nộp bài: màn hình kết quả --- */
  if (submitMutation.data) {
    const { correctCount, totalQuestions, score, totalPoints } = submitMutation.data;
    const ratio = totalQuestions > 0 ? correctCount / totalQuestions : 0;
    const emoji = ratio >= 0.8 ? '🎉' : ratio >= 0.5 ? '👏' : '💪';
    const message =
      ratio >= 0.8 ? 'Tuyệt vời!' : ratio >= 0.5 ? 'Bé làm tốt lắm!' : 'Cố gắng lên nhé!';

    return (
      <SafeAreaView style={styles.screen} edges={['bottom']}>
        <ScrollView contentContainerStyle={styles.resultWrap}>
          <View style={styles.resultCard}>
            <Text style={styles.resultEmoji}>{emoji}</Text>
            <Text style={styles.resultTitle}>{message}</Text>
            <Text style={styles.resultLine}>
              Bé đúng {correctCount} / {totalQuestions} câu
            </Text>
            <View style={styles.scorePill}>
              <Text style={styles.scoreText}>
                ⭐ {score} / {totalPoints} điểm
              </Text>
            </View>
            <View style={styles.fullWidth}>
              <PrimaryButton label="XONG BÀI TẬP" onPress={() => router.back()} />
            </View>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  /* --- Màn hình làm bài: MỖI MÀN HÌNH MỘT CÂU --- */
  const finishLabel = allAnswered ? 'NỘP BÀI 🏆' : `Còn ${missingCount} câu chưa làm`;
  const finishDisabled = !currentAnswered || submitMutation.isPending;
  const finishColor = allAnswered ? C.green : C.sky;

  return (
    <SafeAreaView style={styles.screen} edges={['bottom']}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.flex1}
      >
        <View style={styles.padded}>
          {/* Thanh trên: thoát + tiến độ */}
          <View style={styles.topRow}>
            <TouchableOpacity accessibilityRole="button" onPress={handleExit} style={styles.exitBtn}>
              <Text style={styles.exitText}>✕</Text>
            </TouchableOpacity>
            <View style={styles.progressWrap}>
              <View style={styles.progressTrack}>
                <View style={[styles.progressFill, { width: `${progress * 100}%` }]} />
              </View>
            </View>
            <View style={styles.counterPill}>
              <Text style={styles.counterText}>
                {index + 1}/{questions.length}
              </Text>
            </View>
          </View>
          <Text numberOfLines={1} style={styles.courseTag}>
            🖐️ {detail?.title ?? 'FingerMath'}
          </Text>
        </View>

        {/* Thẻ câu hỏi duy nhất */}
        <ScrollView
          contentContainerStyle={styles.cardScroll}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.card}>
            {question.type === 'TIMED_SEQUENCE' ? (
              <View style={styles.typeChip}>
                <Text style={styles.typeChipText}>TÍNH NHẨM THEO NHỊP</Text>
              </View>
            ) : (
              <Text style={styles.questionText}>{question.content}</Text>
            )}

            <PracticeQuestion
              key={`pq-${question.questionAssignmentId}`}
              question={question}
              value={answers[question.questionAssignmentId] ?? ''}
              onChange={(answer) => {
                setAnswers((current) => ({
                  ...current,
                  [question.questionAssignmentId]: answer,
                }));
                if (isLast) return;
                if (answer.trim()) scheduleAutoNext(index);
                else cancelAutoNext();
              }}
              onSubmit={() => {
                if (!isLast) {
                  scheduleAutoNext(index);
                }
              }}
            />
          </View>
        </ScrollView>

        {/* Thanh điều hướng */}
        <View style={styles.bottom}>
          {submitError ? <AlertBanner tone="error" message={submitError.message} /> : null}
          <View style={styles.navRow}>
            <TouchableOpacity
              accessibilityRole="button"
              activeOpacity={0.85}
              disabled={index === 0 || submitMutation.isPending}
              onPress={handlePrev}
              style={[styles.prevBtn, { opacity: index === 0 ? 0.35 : 1 }]}
            >
              <Text style={styles.prevText}>⬅</Text>
            </TouchableOpacity>

            {isLast ? (
              <TouchableOpacity
                accessibilityRole="button"
                activeOpacity={0.85}
                disabled={finishDisabled}
                onPress={handleFinish}
                style={[
                  styles.nextBtn,
                  {
                    backgroundColor: finishDisabled ? C.slateLight : finishColor,
                    opacity: finishDisabled ? 0.55 : 1,
                  },
                ]}
              >
                {submitMutation.isPending ? (
                  <ActivityIndicator color={C.white} />
                ) : (
                  <Text style={styles.nextText}>
                    {submitError && allAnswered ? 'THỬ NỘP LẠI' : finishLabel}
                  </Text>
                )}
              </TouchableOpacity>
            ) : (
              <View style={styles.autoBox}>
                <Text style={styles.autoText}>
                  {autoNextActive
                    ? 'Giỏi lắm! Sắp sang câu tiếp theo...'
                    : 'Bé chọn đáp án nhé 👆'}
                </Text>
                <View style={styles.autoTrack}>
                  <Animated.View
                    style={[
                      styles.autoFill,
                      { width: autoBar.interpolate({ inputRange: [0, 1], outputRange: ['0%', '100%'] }) },
                    ]}
                  />
                </View>
              </View>
            )}
          </View>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const shadow = {
  shadowColor: '#0f172a',
  shadowOpacity: 0.08,
  shadowRadius: 10,
  shadowOffset: { width: 0, height: 4 },
  elevation: 3,
} as const;

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: C.bg },
  flex1: { flex: 1 },
  fullWidth: { width: '100%', marginTop: 24 },
  padded: { paddingHorizontal: 20, paddingTop: 32, paddingBottom: 8 }, // Tăng paddingTop để cách xa màn hình trên
  center: { alignItems: 'center', paddingVertical: 48 },
  loadingText: { marginTop: 16, fontSize: 20, fontWeight: '700', color: C.skyDark },
  errorBox: { gap: 12, marginTop: 8 },

  /* thanh trên */
  topRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  exitBtn: {
    width: 40,
    height: 40,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 20,
    backgroundColor: C.white,
    borderWidth: 1,
    borderColor: C.line,
  },
  exitText: { fontSize: 18, fontWeight: '800', color: C.slate },
  progressWrap: { flex: 1 },
  progressTrack: {
    height: 14,
    borderRadius: 7,
    backgroundColor: C.skyLine,
    overflow: 'hidden',
  },
  progressFill: { height: '100%', borderRadius: 7, backgroundColor: C.green },
  counterPill: {
    borderRadius: 999,
    backgroundColor: C.white,
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderWidth: 1,
    borderColor: C.skyLine,
  },
  counterText: { fontSize: 16, fontWeight: '900', color: C.skyDark },
  courseTag: {
    marginTop: 8,
    textAlign: 'center',
    fontSize: 14,
    fontWeight: '700',
    color: C.slate,
  },

  /* thẻ câu hỏi */
  cardScroll: { flexGrow: 1, justifyContent: 'center', paddingHorizontal: 20, paddingVertical: 8 },
  card: {
    width: '100%',
    gap: 20,
    borderRadius: 28,
    backgroundColor: C.white,
    padding: 22,
    borderWidth: 1,
    borderColor: C.skyLine,
    ...shadow,
  },
  questionText: {
    textAlign: 'center',
    fontSize: 26,
    fontWeight: '800',
    lineHeight: 36,
    color: C.text,
  },
  typeChip: {
    alignSelf: 'center',
    borderRadius: 999,
    backgroundColor: C.indigoSoft,
    paddingHorizontal: 16,
    paddingVertical: 6,
  },
  typeChipText: { fontSize: 13, fontWeight: '900', letterSpacing: 1.5, color: C.indigo },

  /* đáp án */
  answerGap: { gap: 12 },
  trueFalseRow: { flexDirection: 'row', gap: 12 },
  choice: {
    minHeight: 68,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 12,
    borderRadius: 20,
    borderWidth: 3,
    paddingHorizontal: 16,
    paddingVertical: 10,
  },
  choiceBadge: {
    width: 38,
    height: 38,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 19,
  },
  choiceBadgeText: { fontSize: 18, fontWeight: '900' },
  choiceText: { flexShrink: 1, fontSize: 22, fontWeight: '800', textAlign: 'center' },
  check: { fontSize: 24, fontWeight: '900' },
  textInput: {
    minHeight: 80,
    borderRadius: 20,
    borderWidth: 3,
    borderColor: C.skyLine,
    backgroundColor: C.bg,
    paddingHorizontal: 20,
    fontSize: 30,
    fontWeight: '800',
    textAlign: 'center',
    color: C.text,
  },

  /* thanh nút dưới */
  bottom: { paddingHorizontal: 20, paddingTop: 8, paddingBottom: 12, gap: 8 },
  navRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  prevBtn: {
    width: 64,
    minHeight: 64,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 20,
    borderWidth: 2,
    borderColor: C.skyLine,
    backgroundColor: C.white,
  },
  prevText: { fontSize: 24, color: C.skyDark },
  nextBtn: {
    flex: 1,
    minHeight: 64,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 20,
    paddingHorizontal: 12,
    ...shadow,
  },
  autoBox: {
    flex: 1,
    minHeight: 64,
    justifyContent: 'center',
    gap: 8,
    borderRadius: 20,
    backgroundColor: C.white,
    borderWidth: 2,
    borderColor: C.skyLine,
    paddingHorizontal: 16,
  },
  autoText: { fontSize: 17, fontWeight: '800', color: C.skyDark, textAlign: 'center' },
  autoTrack: { height: 8, borderRadius: 4, backgroundColor: C.skySoft, overflow: 'hidden' },
  autoFill: { height: '100%', borderRadius: 4, backgroundColor: C.green },
  nextText: { fontSize: 22, fontWeight: '900', color: C.white, letterSpacing: 0.5 },

  /* kết quả */
  resultWrap: { flexGrow: 1, justifyContent: 'center', padding: 20 },
  resultCard: {
    alignItems: 'center',
    borderRadius: 32,
    backgroundColor: C.white,
    padding: 28,
    borderWidth: 1,
    borderColor: C.greenSoft,
    ...shadow,
  },
  resultEmoji: { fontSize: 72 },
  resultTitle: { marginTop: 8, fontSize: 32, fontWeight: '900', color: C.greenDark },
  resultLine: { marginTop: 10, fontSize: 22, fontWeight: '700', color: C.slate },
  scorePill: {
    marginTop: 16,
    borderRadius: 999,
    backgroundColor: C.greenSoft,
    paddingHorizontal: 22,
    paddingVertical: 8,
  },
  scoreText: { fontSize: 20, fontWeight: '900', color: C.greenDark },
});