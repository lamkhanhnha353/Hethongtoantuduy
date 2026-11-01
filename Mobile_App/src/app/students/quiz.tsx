import { useEffect, useRef, useState } from 'react';
import { Redirect, useLocalSearchParams } from 'expo-router';
import * as ScreenOrientation from 'expo-screen-orientation';
import { ActivityIndicator, ScrollView, useWindowDimensions, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { audio } from '@/audio';
// import Mascot from '@/components/Mascot/mascot';
import { AlertBanner } from '@/components/AlertBanner';
import { Banner } from '@/components/Banner';
import { PrimaryButton } from '@/components/PrimaryButton';
import { ScreenHeader } from '@/components/ScreenHeader';
import { ClassroomBoard } from '@/components/game/ClassroomBoard';
import { GameIntroCard } from '@/components/game/GameIntroCard';
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
  const { width, height } = useWindowDimensions();
  const isLandscape = width > height;

  useEffect(() => {
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

  function handleStart() {
    rewardedRef.current = false;
    setSelected(null);
    setQuestion(generateQuestion());
    audio.pop();
    setStatus('playing');
  }

  function handleSelect(index: number) {
    if (status !== 'playing' || !question) { return; }
    audio.pop();
    setSelected(index);
    if (index === question.correctIndex) {
      if (!rewardedRef.current) {
        rewardedRef.current = true;
        addReward(studentId);
      }
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
            <View className="w-full max-w-xl self-center">
              <GameIntroCard
                title="Sẵn sàng chưa?"
                description={`Bé sẽ thấy phép tính cộng hoặc trừ trong phạm vi 20 qua những hình ảnh ngộ nghĩnh. Kéo số vào ô trống để nhận ${REWARD.points} điểm và ${REWARD.stars} sao nhé!`}
                onStart={handleStart}
              />
            </View>
          </View>
        ) : question ? (
          <ClassroomBoard
            onNext={handleStart}
            onRetry={handleRetry}
            onSelect={handleSelect}
            question={question}
            selectedIndex={selected}
            status={status}
          />
        ) : null}
      </View>
    </SafeAreaView>
  );
}
