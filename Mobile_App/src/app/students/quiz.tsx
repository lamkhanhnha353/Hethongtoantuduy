import { useEffect, useRef, useState } from 'react';
import { Redirect, useLocalSearchParams } from 'expo-router';
import * as ScreenOrientation from 'expo-screen-orientation';
import { ActivityIndicator, Platform, ScrollView, Text, TouchableOpacity, useWindowDimensions, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { audio } from '@/audio';
// import Mascot from '@/components/Mascot/mascot';
import { AlertBanner } from '@/components/AlertBanner';
import { Banner } from '@/components/Banner';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { ClassroomBoard } from '@/components/game/ClassroomBoard';
import { PracticeModePicker } from '@/components/game/PracticeModePicker';
import { TimedSequenceBoard } from '@/components/game/TimedSequenceBoard';
import { TrueFalseBoard } from '@/components/game/TrueFalseBoard';
import {
  generateQuestion,
  generateTimedSequenceQuestion,
  generateTrueFalseQuestion,
} from '@/lib/mathQuestion';
import { useGameStore } from '@/store/gameStore';
import { useSession } from '@/hooks/useSession';
import { useStudentQuery } from '@/hooks/useStudents';
import { toApiError } from '@/services/apiError';
import type {
  MathQuestion,
  QuizStatus,
  TimedSequenceQuestion,
  TrueFalseQuestion,
} from '@/types/game';

type PracticeMode = 'classic' | 'timed' | 'trueFalse' | null;

export default function ScreenMathQuiz() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  const studentId = Number(id);

  const { session, isHydrated } = useSession();
  const { data: student, isPending, isError, error, refetch } = useStudentQuery(studentId);
  const addReward = useGameStore((state) => state.addReward);

  const [question, setQuestion] = useState<MathQuestion | null>(null);
  const [timedQuestion, setTimedQuestion] = useState<TimedSequenceQuestion | null>(null);
  const [trueFalseQuestion, setTrueFalseQuestion] = useState<TrueFalseQuestion | null>(null);
  const [practiceMode, setPracticeMode] = useState<PracticeMode>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [status, setStatus] = useState<QuizStatus>('idle');
  const rewardedRef = useRef(false);
  const { width, height } = useWindowDimensions();
  const isLandscape = width > height;

  useEffect(() => {
    if (Platform.OS === 'web') {
      return;
    }

    void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.LANDSCAPE).catch(
      () => undefined,
    );
    return () => {
      void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.PORTRAIT_UP).catch(
        () => undefined,
      );
    };
  }, []);

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

  function prepareQuestion() {
    rewardedRef.current = false;
    setSelected(null);
    setTimedQuestion(null);
    setTrueFalseQuestion(null);
    setStatus('playing');
    audio.pop();
  }

  function handleStartClassic() {
    prepareQuestion();
    setPracticeMode('classic');
    setQuestion(generateQuestion());
  }

  function handleStartTimed() {
    prepareQuestion();
    setPracticeMode('timed');
    setTimedQuestion(generateTimedSequenceQuestion());
  }

  function handleStartTrueFalse() {
    prepareQuestion();
    setPracticeMode('trueFalse');
    setTrueFalseQuestion(generateTrueFalseQuestion());
  }

  function handleNextTimed() {
    rewardedRef.current = false;
    setTimedQuestion(generateTimedSequenceQuestion());
    audio.pop();
  }

  function handleNextTrueFalse() {
    rewardedRef.current = false;
    setTrueFalseQuestion(generateTrueFalseQuestion());
    audio.pop();
  }

  function handleCorrectAnswer() {
    if (!rewardedRef.current) {
      rewardedRef.current = true;
      addReward(studentId);
    }
  }

  function handleChangeMode() {
    setPracticeMode(null);
    setQuestion(null);
    setTimedQuestion(null);
    setTrueFalseQuestion(null);
    setSelected(null);
    setStatus('idle');
    audio.pop();
  }

  function handleSelect(index: number) {
    if (status !== 'playing' || !question) { return; }
    audio.pop();
    setSelected(index);
    if (index === question.correctIndex) {
      handleCorrectAnswer();
      audio.correct();
      setStatus('correct');
      return;
    }
    audio.wrong();
    setStatus('wrong');
  }

  function handleRetry() {
    setSelected(null);
    setStatus('playing');
    audio.pop();
  }

  return (
    <SafeAreaView
      className="flex-1 bg-slate-50"
      edges={isLandscape ? ['left', 'right', 'bottom'] : ['bottom']}
    >
      <View
        className={`relative overflow-hidden bg-indigo-600 px-5 ${isLandscape ? 'pb-3 pt-4' : 'pb-7 pt-14'
          }`}
      >
        <View className="absolute -right-12 -top-12 h-40 w-40 rounded-full bg-cyan-400/20 opacity-50" />
        <View className="absolute bottom-0 left-4 h-24 w-24 rounded-full bg-indigo-300/20 opacity-50" />
        <Banner student={student} />
        {/* <Mascot /> */}
      </View>

      <View
        className={`flex-1 px-4 ${isLandscape ? 'pb-3 pt-2' : 'pb-6 pt-4'}`}
      >
        {status === 'idle' ? (
          <View className="flex-1 justify-center">
            <PracticeModePicker
              onSelectClassic={handleStartClassic}
              onSelectTimed={handleStartTimed}
              onSelectTrueFalse={handleStartTrueFalse}
            />
          </View>
        ) : (
          <>
            <View className="mb-2 items-end">
              <TouchableOpacity
                accessibilityRole="button"
                className="rounded-full bg-indigo-100 px-4 py-2"
                onPress={handleChangeMode}
              >
                <Text className="font-bold text-indigo-800">Đổi dạng luyện tập</Text>
              </TouchableOpacity>
            </View>
            {practiceMode === 'classic' && question ? (
              <ClassroomBoard
                onNext={handleStartClassic}
                onRetry={handleRetry}
                onSelect={handleSelect}
                question={question}
                selectedIndex={selected}
                status={status}
              />
            ) : practiceMode === 'timed' && timedQuestion ? (
              <TimedSequenceBoard
                key={timedQuestion.id}
                onCorrect={handleCorrectAnswer}
                onNext={handleNextTimed}
                question={timedQuestion}
              />
            ) : practiceMode === 'trueFalse' && trueFalseQuestion ? (
              <TrueFalseBoard
                key={trueFalseQuestion.id}
                onCorrect={handleCorrectAnswer}
                onNext={handleNextTrueFalse}
                question={trueFalseQuestion}
              />
            ) : null}
          </>
        )}
      </View>
    </SafeAreaView>
  );
}
