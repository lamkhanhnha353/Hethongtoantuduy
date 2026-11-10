import { useEffect, useState } from 'react';
import { router, useLocalSearchParams } from 'expo-router';
import * as ScreenOrientation from 'expo-screen-orientation';
import {
  ActivityIndicator,
  Platform,
  ScrollView,
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

function AnswerChoice({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  return (
    <TouchableOpacity
      accessibilityRole="button"
      accessibilityState={{ selected }}
      className={`rounded-xl border px-4 py-3 ${
        selected ? 'border-indigo-500 bg-indigo-50' : 'border-slate-200 bg-white'
      }`}
      onPress={onPress}
    >
      <Text className={`text-base ${selected ? 'font-semibold text-indigo-700' : 'text-slate-800'}`}>
        {label}
      </Text>
    </TouchableOpacity>
  );
}

function PracticeQuestion({
  question,
  value,
  onChange,
}: {
  question: StudentPracticeQuestionDto;
  value: string;
  onChange: (answer: string) => void;
}) {
  if (question.type === 'MULTIPLE_CHOICE' && question.options.length > 0) {
    return (
      <View className="mt-4 gap-2">
        {question.options.map((option) => (
          <AnswerChoice
            key={option.key}
            label={`${option.key}. ${option.text}`}
            selected={value === option.key}
            onPress={() => onChange(option.key)}
          />
        ))}
      </View>
    );
  }

  if (question.type === 'TRUE_FALSE') {
    return (
      <View className="mt-4 flex-row gap-3">
        {['Đúng', 'Sai'].map((answer) => (
          <View className="flex-1" key={answer}>
            <AnswerChoice
              label={answer}
              selected={value === answer}
              onPress={() => onChange(answer)}
            />
          </View>
        ))}
      </View>
    );
  }

  if (question.type === 'TIMED_SEQUENCE') {
    return (
      <TimedSequenceAnswer
        terms={question.sequenceTerms}
        value={value}
        onChange={onChange}
      />
    );
  }

  return (
    <TextInput
      accessibilityLabel="Câu trả lời"
      className="mt-4 rounded-xl border border-slate-200 bg-white px-4 py-3 text-base text-slate-900"
      onChangeText={onChange}
      placeholder="Nhập câu trả lời"
      value={value}
    />
  );
}

export default function StudentAssignmentScreen() {
  const { assignmentId: assignmentIdParam, studentId: studentIdParam } =
    useLocalSearchParams<{ assignmentId?: string; studentId?: string }>();
  const assignmentId = Number(assignmentIdParam);
  const studentId = Number(studentIdParam);
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const assignmentQuery = useStudentPracticeAssignmentQuery(studentId, assignmentId);
  const submitMutation = useSubmitStudentPracticeAssignmentMutation(studentId, assignmentId);
  const detail = assignmentQuery.data;
  const hasTimedSequence = detail?.questions.some((question) => question.type === 'TIMED_SEQUENCE') ?? false;
  const submitError = submitMutation.error ? toApiError(submitMutation.error) : null;
  const loadError = assignmentQuery.error ? toApiError(assignmentQuery.error) : null;
  const allAnswered = !!detail?.questions.length
    && detail.questions.every((question) => answers[question.questionAssignmentId]?.trim());

  useEffect(() => {
    if (!detail || Platform.OS === 'web') {
      return undefined;
    }

    const orientation = hasTimedSequence
      ? ScreenOrientation.OrientationLock.LANDSCAPE
      : ScreenOrientation.OrientationLock.PORTRAIT_UP;
    void ScreenOrientation.lockAsync(orientation).catch((error: unknown) => {
      console.error('Could not set orientation for the assignment screen.', error);
    });

    return () => {
      void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.PORTRAIT_UP).catch(
        (error: unknown) => {
          console.error('Could not restore portrait orientation after the assignment screen.', error);
        },
      );
    };
  }, [detail, hasTimedSequence]);

  function handleSubmit() {
    if (!detail || !allAnswered || submitMutation.isPending) {
      return;
    }

    submitMutation.mutate({
      answers: detail.questions.map((question) => ({
        questionAssignmentId: question.questionAssignmentId,
        answer: answers[question.questionAssignmentId].trim(),
      })),
    });
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['bottom']}>
      <ScrollView contentContainerClassName="px-5 pb-8 pt-4">
        <ScreenHeader title="Bài tập được giao" />

        {assignmentQuery.isPending ? (
          <View className="items-center py-12">
            <ActivityIndicator size="large" color="#4f46e5" />
            <Text className="mt-3 text-sm text-slate-500">Đang tải câu hỏi...</Text>
          </View>
        ) : null}

        {loadError ? (
          <View className="gap-3">
            <AlertBanner tone="error" message={loadError.message} />
            <PrimaryButton
              label="Quay lại"
              onPress={() => router.back()}
              variant="ghost"
            />
          </View>
        ) : null}

        {detail && !submitMutation.data ? (
          <>
            <View className="rounded-2xl border border-slate-200 bg-white p-4">
              <Text className="text-xl font-bold text-slate-900">{detail.title}</Text>
              {detail.lessonTitle ? (
                <Text className="mt-1 text-sm text-slate-500">{detail.lessonTitle}</Text>
              ) : null}
              <Text className="mt-3 text-sm text-slate-600">
                {detail.questions.length} câu · {detail.totalPoints} điểm
              </Text>
              <Text className="mt-1 text-sm text-slate-600">
                Hạn nộp: {new Date(detail.deadline).toLocaleString()}
              </Text>
            </View>

            {detail.questions.length === 0 ? (
              <View className="mt-4 gap-3">
                <AlertBanner
                  tone="error"
                  message="Bài tập này hiện chưa có câu hỏi. Vui lòng báo giáo viên kiểm tra và giao lại bài."
                />
                <PrimaryButton
                  label="Quay lại"
                  onPress={() => router.back()}
                  variant="ghost"
                />
              </View>
            ) : null}

            {detail.questions.map((question, index) => (
              <View
                className="mt-4 rounded-2xl border border-slate-200 bg-white p-4"
                key={question.questionAssignmentId}
              >
                <Text className="text-xs font-semibold uppercase text-indigo-600">
                  Câu {index + 1} · {question.pointValue} điểm
                </Text>
                {question.type === 'TIMED_SEQUENCE' ? (
                  <Text className="mt-2 text-sm font-medium text-slate-600">
                    Tính nhẩm theo nhịp · mỗi số hiển thị 1 giây
                  </Text>
                ) : (
                  <Text className="mt-2 text-base font-semibold leading-6 text-slate-900">
                    {question.content}
                  </Text>
                )}
                <PracticeQuestion
                  question={question}
                  value={answers[question.questionAssignmentId] ?? ''}
                  onChange={(answer) => {
                    setAnswers((current) => ({
                      ...current,
                      [question.questionAssignmentId]: answer,
                    }));
                  }}
                />
              </View>
            ))}

            {submitError ? <AlertBanner tone="error" message={submitError.message} /> : null}
            {detail.questions.length > 0 ? (
              <View className="mt-5">
                <PrimaryButton
                  disabled={!allAnswered}
                  label="Nộp bài"
                  loading={submitMutation.isPending}
                  onPress={() => {
                    void handleSubmit();
                  }}
                />
              </View>
            ) : null}
          </>
        ) : null}

        {submitMutation.data ? (
          <View className="mt-4 rounded-2xl border border-emerald-200 bg-emerald-50 p-5">
            <Text className="text-xl font-bold text-emerald-900">Đã nộp bài!</Text>
            <Text className="mt-3 text-base text-emerald-800">
              Kết quả: {submitMutation.data.correctCount}/{submitMutation.data.totalQuestions} câu đúng
            </Text>
            <Text className="mt-1 text-base text-emerald-800">
              Điểm: {submitMutation.data.score}/{submitMutation.data.totalPoints}
            </Text>
            <View className="mt-5">
              <PrimaryButton label="Quay lại" onPress={() => router.back()} />
            </View>
          </View>
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}
