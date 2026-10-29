import { useRef, useState } from 'react';
import { Redirect, useLocalSearchParams } from 'expo-router';
import { ActivityIndicator, ScrollView, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { audio } from '@/audio';
import { AlertBanner } from '@/components/AlertBanner';
import { Banner } from '@/components/Banner';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { AnswerOption, type AnswerOptionState } from '@/components/game/AnswerOption';
import { FeedbackCard } from '@/components/game/FeedbackCard';
import { GameIntroCard } from '@/components/game/GameIntroCard';
import { MathQuestionCard } from '@/components/game/MathQuestionCard';
import { REWARD, generateQuestion } from '@/lib/mathQuestion';
import { useGameStore } from '@/store/gameStore';
import { useSession } from '@/hooks/useSession';
import { useStudentQuery } from '@/hooks/useStudents';
import { toApiError } from '@/services/apiError';
import type { MathQuestion, QuizStatus } from '@/types/game';

export default function ScreenMathQuiz() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const studentId = Number(id);

  const { session, isHydrated } = useSession();
  const { data: student, isPending, isError, error, refetch } = useStudentQuery(studentId);
  const addReward = useGameStore((state) => state.addReward);

  const [question, setQuestion] = useState<MathQuestion | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [status, setStatus] = useState<QuizStatus>('idle');
  const rewardedRef = useRef(false);

  const invalidId = !Number.isFinite(studentId);
  const loadError = isError ? toApiError(error) : null;

  if (!isHydrated) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={['bottom']}>
        <ActivityIndicator size="large" color="#4f46e5" />
      </SafeAreaView>
    );
  }

  if (!session) {
    return <Redirect href="/login" />;
  }

  if (invalidId || loadError || (isPending && !student)) {
    return (
      <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>
        <ScrollView contentContainerClassName="px-6 py-6">
          <ScreenHeader title="Toán vui" />

          {isPending && !invalidId ? (
            <View className="items-center py-10">
              <ActivityIndicator size="large" color="#4f46e5" />
            </View>
          ) : (
            <>
              <AlertBanner message={invalidId ? 'Mã học sinh không hợp lệ.' : (loadError?.message ?? 'Không tải được thông tin học sinh.')}
                tone="error" />
              <PrimaryButton label="Thử lại" onPress={() => { void refetch(); }} variant="ghost" /> </>
          )}
        </ScrollView>
      </SafeAreaView>
    );
  }

  if (!student) {
    return null;
  }

  function handleStart() {
    rewardedRef.current = false;
    setSelected(null);
    setQuestion(generateQuestion());
    setStatus('playing');
  }

  function handleSelect(index: number) {
    if (status !== 'playing' || !question) { return; }
    setSelected(index);
    if (index === question.correctIndex) {
      if (!rewardedRef.current) {
        rewardedRef.current = true;
        addReward(studentId);
      }
      audio.play('correct');
      setStatus('correct');
      return;
    }
    audio.play('wrong');
    setStatus('wrong');
  }

  function answerState(index: number): AnswerOptionState {
    if (status === 'correct' && index === question?.correctIndex) {
      return 'correct';
    }

    if (status === 'wrong') {
      if (index === selected) {
        return 'wrong';
      }
      if (index === question?.correctIndex) {
        return 'correct';
      }
    }
    return 'default';
  }
  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>

      <View className="relative overflow-hidden bg-indigo-600 px-5 pb-7 pt-14">
        <View className="absolute -right-12 -top-12 h-40 w-40 rounded-full bg-cyan-400/20 opacity-50" />
        <View className="absolute bottom-0 left-4 h-24 w-24 rounded-full bg-indigo-300/20 opacity-50" />
        <Banner student={student} />
      </View>

      <View className="flex-1 px-5 pt-6">

        {status === 'idle' ? (
          <GameIntroCard
            title="Sẵn sàng chưa?"
            description={`Bé sẽ thấy một phép tính cộng hoặc trừ trong phạm vi 20. Chọn đúng đáp án để nhận ${REWARD.points} điểm và ${REWARD.stars} sao nhé!`}
            onStart={handleStart}
          />
        ) : null}

        {status !== 'idle' && question ? (
          <>
            <MathQuestionCard expression={question.expression} />

            <View className="mt-4 flex-row flex-wrap gap-3">
              {question.options.map((option, index) => (
                <AnswerOption
                  state={answerState(index)}
                  value={option}
                  disabled={status !== 'playing'}
                  key={`${question.expression}-${index}`}
                  onPress={() => handleSelect(index)}
                >
                </AnswerOption>
              ))}
            </View>

            {status === 'correct' ? (
              <FeedbackCard variant="correct" points={REWARD.points} stars={REWARD.stars} onAction={handleStart} />) : null}
            {status === 'wrong' ? (
              <FeedbackCard variant="wrong" correctAnswer={question.options[question.correctIndex]} onAction={handleStart} />) : null}
          </>
        ) : null}
      </View>
    </SafeAreaView>
  );
}
