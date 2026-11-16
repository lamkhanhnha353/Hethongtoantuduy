import { useEffect, useRef, useState } from 'react';
import { Animated, StyleSheet, Text, TextInput, View } from 'react-native';

import type { StudentPracticeSequenceTermDto } from '@/types/assignment';

type TimedSequenceAnswerProps = {
  terms: StudentPracticeSequenceTermDto[];
  value: string;
  onChange: (answer: string) => void;
  onSubmit: () => void;
};

const C = {
  indigo: '#4f46e5',
  indigoDark: '#312e81',
  indigoSoft: '#eef2ff',
  indigoLine: '#c7d2fe',
  green: '#10b981',
  greenSoft: '#d1fae5',
  red: '#f43f5e',
  redSoft: '#ffe4e6',
  slate: '#475569',
  slateSoft: '#f1f5f9',
  white: '#ffffff',
  orangeSoft: '#ffedd5',
  orange: '#f97316',
};

function operatorStyle(operator?: string | null) {
  if (operator === '+') return { color: C.green, bg: C.greenSoft, label: 'Thêm' };
  if (operator === '-' || operator === '−') return { color: C.red, bg: C.redSoft, label: 'Bớt' };
  if (operator) return { color: C.slate, bg: C.slateSoft, label: '' };
  return { color: C.indigo, bg: C.indigoSoft, label: 'Số đầu tiên' };
}

type SequencePhase = 'PREPARING' | 'SHOWING' | 'ANSWERING';

export function TimedSequenceAnswer({ terms = [], value, onChange, onSubmit }: TimedSequenceAnswerProps) {
  const [phase, setPhase] = useState<SequencePhase>('PREPARING');
  const [prepTime, setPrepTime] = useState(3);
  const [shownTermCount, setShownTermCount] = useState(0);
  const [answerTimeLeft, setAnswerTimeLeft] = useState(10);

  const [pop] = useState(() => new Animated.Value(1));
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Hiệu ứng "nảy" mỗi khi hiện số mới
  useEffect(() => {
    if (shownTermCount < 1 || phase !== 'SHOWING') return;
    pop.setValue(0.6);
    Animated.spring(pop, { toValue: 1, friction: 5, tension: 120, useNativeDriver: true }).start();
  }, [shownTermCount, pop, phase]);

  // 1. GIAI ĐOẠN ĐẾM NGƯỢC CHUẨN BỊ (3... 2... 1)
  useEffect(() => {
    if (phase === 'PREPARING') {
      if (prepTime > 0) {
        timerRef.current = setTimeout(() => {
          setPrepTime((prev) => prev - 1);
        }, 1000);
      } else {
        timerRef.current = setTimeout(() => {
          setPhase('SHOWING');
          setShownTermCount(1);
        }, 600);
      }
    }

    return () => {
      if (timerRef.current !== null) clearTimeout(timerRef.current);
    };
  }, [phase, prepTime]);

  // 2. GIAI ĐOẠN HIỂN THỊ SỐ (Chỉ 1 lần duy nhất)
  useEffect(() => {
    if (phase === 'SHOWING') {
      if (shownTermCount <= terms.length) {
        timerRef.current = setTimeout(() => {
          setShownTermCount((count) => count + 1);
        }, 1000);
      } else {
        // Hết dãy số -> Chuyển sang màn hình nhập đáp án
        timerRef.current = setTimeout(() => {
          setPhase('ANSWERING');
        }, 1000);
      }
    }

    return () => {
      if (timerRef.current !== null) clearTimeout(timerRef.current);
    };
  }, [phase, shownTermCount, terms.length]);

  // 3. GIAI ĐOẠN NHẬP ĐÁP ÁN (Đếm ngược 10s)
  useEffect(() => {
    if (phase === 'ANSWERING') {
      if (answerTimeLeft > 0) {
        timerRef.current = setTimeout(() => {
          setAnswerTimeLeft((time) => time - 1);
        }, 1000);
      } else if (answerTimeLeft === 0 && (!value || value.trim() === '')) {
        // Hết 10 giây mà chưa gõ gì thì tự chuyển câu
        timerRef.current = setTimeout(() => {
          onSubmit();
        }, 800);
      }
    }
    return () => {
      if (timerRef.current !== null) clearTimeout(timerRef.current);
    };
  }, [phase, answerTimeLeft, value, onSubmit]);

  // 4. TỰ ĐỘNG CHUYỂN CÂU KHI GÕ XONG ĐÁP ÁN (Game Loop)
  useEffect(() => {
    if (phase === 'ANSWERING' && value && value.trim().length > 0) {
      const autoSubmitTimer = setTimeout(() => {
        onSubmit();
      }, 500);
      return () => clearTimeout(autoSubmitTimer);
    }
  }, [value, phase, onSubmit]);

  const currentTerm = terms[shownTermCount - 1];
  const op = operatorStyle(currentTerm?.operator);

  // --- RENDER GIAO DIỆN THEO TỪNG GIAI ĐOẠN ---

  if (phase === 'PREPARING') {
    return (
      <View style={styles.box}>
        <Text style={styles.bigEmoji}>🖐️</Text>
        <Text style={styles.title}>Chuẩn bị giơ tay nhé!</Text>
        <View style={styles.prepCircle}>
          <Text style={styles.prepText}>{prepTime > 0 ? prepTime : 'Dô!'}</Text>
        </View>
        <Text style={styles.hint}>Mỗi số sẽ hiện 1 giây. Bé nhìn thật kỹ nhé!</Text>
      </View>
    );
  }

  if (phase === 'SHOWING') {
    return (
      <View style={styles.box}>
        <View style={styles.dotsRow}>
          {terms.map((term, i) => (
            <View
              key={`${i}-${term.value}`}
              style={[styles.dot, i < shownTermCount ? styles.dotOn : styles.dotOff]}
            />
          ))}
        </View>

        {shownTermCount <= terms.length ? (
          <>
            <View style={[styles.badge, { backgroundColor: op.bg }]}>
              <Text style={[styles.badgeText, { color: op.color }]}>
                {op.label ? op.label : `Số ${shownTermCount}`}
              </Text>
            </View>

            <Animated.View style={[styles.numberRow, { transform: [{ scale: pop }] }]}>
              {currentTerm?.operator ? (
                <Text style={[styles.operator, { color: op.color }]}>{currentTerm.operator}</Text>
              ) : null}
              <Text style={styles.number}>{currentTerm?.value}</Text>
            </Animated.View>
          </>
        ) : (
          <View style={styles.numberRow}>
            <Text style={[styles.title, { color: C.indigo }]}>Chuẩn bị...</Text>
          </View>
        )}

        <Text style={styles.counter}>
          Số {shownTermCount <= terms.length ? shownTermCount : terms.length} / {terms.length}
        </Text>
      </View>
    );
  }

  // Màn hình nhập kết quả
  return (
    <View style={styles.box}>
      <View style={styles.answerHeader}>
        <Text style={styles.title}>Bé tính ra bao nhiêu?</Text>
        <View style={[styles.timerCircle, answerTimeLeft <= 3 && styles.timerCircleAlert]}>
          <Text style={[styles.timerText, answerTimeLeft <= 3 && styles.timerTextAlert]}>
            {answerTimeLeft}
          </Text>
        </View>
      </View>

      <TextInput
        accessibilityLabel="Kết quả phép tính theo nhịp"
        autoFocus
        keyboardType="numeric"
        onChangeText={onChange}
        placeholder="?"
        placeholderTextColor="#a5b4fc"
        style={styles.input}
        value={value}
        editable={answerTimeLeft > 0}
      />

      {answerTimeLeft === 0 && (
        <Text style={styles.timeoutHint}>Hết 10 giây mất rồi!</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  box: {
    width: '100%',
    alignItems: 'center',
    borderRadius: 28,
    backgroundColor: C.indigoSoft,
    borderWidth: 2,
    borderColor: C.indigoLine,
    paddingVertical: 28,
    paddingHorizontal: 20,
  },
  bigEmoji: { fontSize: 64, marginBottom: 12 },
  title: {
    fontSize: 24,
    fontWeight: '800',
    color: C.indigoDark,
    textAlign: 'center',
  },
  hint: {
    marginTop: 20,
    fontSize: 16,
    lineHeight: 23,
    color: C.slate,
    textAlign: 'center',
  },
  prepCircle: {
    marginTop: 20,
    width: 100,
    height: 100,
    borderRadius: 50,
    backgroundColor: C.white,
    borderWidth: 4,
    borderColor: C.indigoLine,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: C.indigo,
    shadowOpacity: 0.1,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 3,
  },
  prepText: {
    fontSize: 54,
    fontWeight: '900',
    color: C.indigo,
  },
  dotsRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  dot: { width: 12, height: 12, borderRadius: 6 },
  dotOn: { backgroundColor: C.indigo },
  dotOff: { backgroundColor: C.indigoLine },

  badge: { borderRadius: 999, paddingHorizontal: 18, paddingVertical: 6 },
  badgeText: { fontSize: 16, fontWeight: '800' },
  numberRow: {
    marginTop: 8,
    minHeight: 140,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
  },
  operator: { fontSize: 72, fontWeight: '900' },
  number: { fontSize: 112, fontWeight: '900', color: C.indigoDark },
  counter: { marginTop: 4, fontSize: 15, fontWeight: '700', color: C.slate },

  answerHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    width: '100%',
    paddingHorizontal: 10,
  },
  timerCircle: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 2,
    borderColor: C.green,
    backgroundColor: C.greenSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  timerCircleAlert: {
    borderColor: C.red,
    backgroundColor: C.redSoft,
  },
  timerText: {
    fontSize: 18,
    fontWeight: '900',
    color: C.green,
  },
  timerTextAlert: {
    color: C.red,
  },
  input: {
    marginTop: 24,
    minHeight: 84,
    width: '100%',
    borderRadius: 22,
    borderWidth: 3,
    borderColor: C.indigo,
    backgroundColor: C.white,
    fontSize: 44,
    fontWeight: '900',
    color: C.indigoDark,
    textAlign: 'center',
  },
  timeoutHint: {
    marginTop: 12,
    fontSize: 16,
    fontWeight: '700',
    color: C.red,
  }
});