import { useState, useEffect } from 'react';
import { View, Text, Pressable, ScrollView, ActivityIndicator, StyleSheet, Alert } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Feather, Ionicons } from '@expo/vector-icons';
import { API_URL } from '@/config/env';
import { useSession } from '@/hooks/useSession';

const C = {
  indigo600: '#4f46e5',
  indigo50: '#eef2ff',
  indigo900: '#312e81',
  emerald500: '#10b981',
  emerald50: '#ecfdf5',
  emerald900: '#064e3b',
  emerald700: '#047857',
  slate200: '#e2e8f0',
  slate500: '#64748b',
  slate600: '#475569',
  slate700: '#334155',
  slate400: '#94a3b8',
  white: '#ffffff',
  rose500: '#f43f5e',
  rose50: '#fff1f2',
};

interface EnrollmentBottomSheetProps {
  visible: boolean;
  onClose: () => void;
  course: any;
  onConfirm: (studentId: number, classId: number) => void;
}

export function EnrollmentBottomSheet({ visible, onClose, course, onConfirm }: EnrollmentBottomSheetProps) {
  const insets = useSafeAreaInsets();
  const { session } = useSession(); 
  
  const [students, setStudents] = useState<any[]>([]);
  const [classes, setClasses] = useState<any[]>([]);
  const [isFetchingData, setIsFetchingData] = useState(false);

  const [selectedStudent, setSelectedStudent] = useState<number | null>(null);
  const [selectedClass, setSelectedClass] = useState<number | null>(null);

  const [confirmingClass, setConfirmingClass] = useState<any>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showResult, setShowResult] = useState(false);

  useEffect(() => {
    const fetchEnrollmentData = async () => {
      if (!visible || !course || !session) return;
      
      setIsFetchingData(true);
      try {
        let token = "";
        if (typeof session === 'string') {
          try {
            const parsed = JSON.parse(session);
            token = parsed.accessToken || session;
          } catch {
            token = session;
          }
        } else if (session && typeof session === 'object') {
          token = (session as any).accessToken || String(session);
        }

        // 1. Lấy danh sách học viên
        const studentRes = await fetch(`${API_URL}/api/students`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        const studentText = await studentRes.text();
        let studentData = [];
        if (studentRes.ok && studentText) studentData = JSON.parse(studentText);
        
        // 2. Lấy danh sách lớp học
        const classRes = await fetch(`${API_URL}/api/classes/course-level/${course.id}`);
        const classText = await classRes.text();
        let classData = [];
        if (classRes.ok && classText) classData = JSON.parse(classText);

        const safeStudents = Array.isArray(studentData) ? studentData : [];
        const safeClasses = Array.isArray(classData) ? classData : [];

        setStudents(safeStudents);
        setClasses(safeClasses);

        if (safeStudents.length > 0) {
          setSelectedStudent(safeStudents[0].id);
        }
      } catch (error) {
        console.error("Lỗi fetch enrollment data:", error);
      } finally {
        setIsFetchingData(false);
      }
    };

    fetchEnrollmentData();
  }, [visible, course, session]);

  if (!visible || !course) return null;

  const canSubmit = !!selectedStudent && !!selectedClass;

  const handleBookingRequest = () => {
    if (!selectedStudent || !selectedClass) return;
    setIsSubmitting(true);
    setTimeout(() => {
      setIsSubmitting(false);
      setShowResult(true);
    }, 1500);
  };

  const handlePayNow = () => {
    setShowResult(false);
    if (selectedStudent && selectedClass) {
      onConfirm(selectedStudent, selectedClass);
    }
  };

  const handlePayLater = () => {
    setShowResult(false);
    onClose();
  };

  const executeClassSelection = () => {
    if (confirmingClass) {
      setSelectedClass(confirmingClass.id);
      setConfirmingClass(null);
    }
  };

  return (
    <View className="justify-end" style={[StyleSheet.absoluteFill, styles.overlay]}>
      <Pressable
        style={StyleSheet.absoluteFill}
        onPress={() => {
          if (!isSubmitting && !showResult) onClose();
        }}
      />

      <View
        className="bg-white rounded-t-[32px] w-full max-h-[85%] overflow-hidden"
        style={{ paddingBottom: insets.bottom > 0 ? insets.bottom : 24 }}
      >
        <View className="items-center pt-4 pb-2">
          <View className="w-12 h-1.5 bg-slate-200 rounded-full" />
        </View>

        <View className="flex-row items-center justify-between px-5 pb-4 border-b border-slate-100">
          <View className="flex-1">
            <Text className="text-[17px] font-black text-slate-900">Thông tin đăng ký</Text>
            <Text className="text-[14px] font-semibold text-indigo-600 mt-0.5">{course.name}</Text>
          </View>
          <Pressable
            onPress={onClose}
            disabled={isSubmitting || showResult}
            className="w-8 h-8 bg-slate-100 rounded-full items-center justify-center"
          >
            <Feather name="x" size={18} color={C.slate500} />
          </Pressable>
        </View>

        {isFetchingData ? (
           <View className="py-10 items-center justify-center">
             <ActivityIndicator size="large" color={C.indigo600} />
             <Text className="text-slate-500 mt-3">Đang tải dữ liệu...</Text>
           </View>
        ) : (
          <ScrollView className="px-5 py-4" showsVerticalScrollIndicator={false}>
            <Text className="text-[14px] font-bold text-slate-900 mb-3">1. Chọn hồ sơ học viên</Text>
            <View className="flex-row flex-wrap gap-3 mb-6">
              {students.map((student) => {
                const isSelected = selectedStudent === student.id;
                const avatarLetter = student.fullName ? student.fullName.charAt(0).toUpperCase() : 'A';
                return (
                  <Pressable
                    key={student.id}
                    onPress={() => setSelectedStudent(student.id)}
                    className="flex-row items-center gap-2.5 px-4 py-3 rounded-2xl border"
                    style={{
                      backgroundColor: isSelected ? C.indigo50 : C.white,
                      borderColor: isSelected ? C.indigo600 : C.slate200,
                    }}
                  >
                    <View
                      className="w-8 h-8 rounded-full items-center justify-center"
                      style={{ backgroundColor: isSelected ? C.indigo600 : C.slate200 }}
                    >
                      <Text className="font-bold text-xs" style={{ color: isSelected ? C.white : C.slate600 }}>
                        {avatarLetter}
                      </Text>
                    </View>
                    <View>
                      <Text className="text-[14px] font-bold" style={{ color: isSelected ? C.indigo900 : C.slate700 }}>
                        {student.fullName}
                      </Text>
                    </View>
                  </Pressable>
                );
              })}

              <Pressable className="flex-row items-center gap-2 px-4 py-3 rounded-2xl border border-dashed border-slate-300 bg-slate-50">
                <Feather name="plus" size={18} color={C.slate500} />
                <Text className="text-[13px] font-bold text-slate-600">Thêm hồ sơ</Text>
              </Pressable>
            </View>

            <View className="flex-row items-center justify-between mb-3">
              <Text className="text-[14px] font-bold text-slate-900">2. Chọn lịch học phù hợp</Text>
              <Text className="text-[12px] font-medium text-slate-500">Đang mở {classes.length} lớp</Text>
            </View>
            <View className="gap-3 mb-6">
              {classes.length === 0 ? (
                 <Text className="text-slate-500 text-sm py-4 text-center">Hiện chưa có lớp học nào được mở cho khóa này.</Text>
              ) : classes.map((cls) => {
                const isSelected = selectedClass === cls.id;
                
                // --- SỬA LỖI MAPPING SỐ SLOT TRỐNG TẠI ĐÂY ---
               // Lấy trực tiếp con số đếm từ Backend
               // Lấy trực tiếp con số đếm từ Backend
                const activeStudentsCount = cls.activeStudentsCount ?? 0;

                // Tính số lượng Slot còn trống
                const maxCap = cls.maxCapacity ?? cls.max_capacity ?? 0;
                const slotCount = Math.max(0, maxCap - activeStudentsCount);
                
                const isFull = slotCount === 0;

                const teacherName = cls.teacherName ?? cls.teacher?.fullName ?? 'Đang cập nhật';
                const scheduleStr = cls.schedule ?? (cls.classSchedules && cls.classSchedules.length > 0 ? `${cls.classSchedules[0].dayOfWeek} ${cls.classSchedules[0].startTime}` : 'Đang cập nhật');

                return (
                  <Pressable
                    key={cls.id}
                    onPress={() => {
                        if (!isFull) setConfirmingClass(cls);
                    }}
                    className={`p-4 rounded-2xl border overflow-hidden ${isFull ? 'opacity-60' : ''}`}
                    style={{
                      backgroundColor: isFull ? '#f8fafc' : isSelected ? C.emerald50 : C.white,
                      borderColor: isFull ? C.slate200 : isSelected ? C.emerald500 : C.slate200,
                    }}
                  >
                    {isSelected && !isFull && (
                      <View className="absolute top-0 right-0 bg-emerald-500 px-3 py-1 rounded-bl-xl z-10 flex-row items-center gap-1">
                        <Feather name="check-circle" size={12} color="white" />
                        <Text className="text-[10px] font-bold text-white uppercase tracking-wider">Đang chọn</Text>
                      </View>
                    )}

                    <View className="flex-row items-center justify-between">
                      <View className="flex-1 pr-2 mt-1">
                        <Text className="text-[15px] font-bold mb-2" style={{ color: isSelected && !isFull ? C.emerald900 : '#0f172a' }}>
                          {cls.name}
                        </Text>
                        <View className="flex-row items-center gap-4">
                          <View className="flex-row items-center gap-1.5">
                            <Feather name="clock" size={14} color={isSelected && !isFull ? '#059669' : C.slate500} />
                            <Text className="text-[12px] font-medium" style={{ color: isSelected && !isFull ? C.emerald700 : C.slate600 }}>
                              {scheduleStr}
                            </Text>
                          </View>
                          <View className="flex-row items-center gap-1.5">
                            <Feather name="user" size={14} color={isSelected && !isFull ? '#059669' : C.slate500} />
                            <Text className="text-[12px] font-medium" style={{ color: isSelected && !isFull ? C.emerald700 : C.slate600 }}>
                              GV: {teacherName}
                            </Text>
                          </View>
                        </View>
                      </View>

                      <View
                        className="items-center justify-center px-4 py-2.5 rounded-xl border"
                        style={{
                          backgroundColor: isFull ? C.rose50 : isSelected ? C.emerald500 : '#f8fafc',
                          borderColor: isFull ? C.rose500 : isSelected ? C.emerald500 : C.slate200,
                        }}
                      >
                        <Text className="text-[18px] font-black leading-none mb-0.5" style={{ color: isFull ? C.rose500 : isSelected ? C.white : C.slate700 }}>
                          {slotCount}
                        </Text>
                        <Text className="text-[10px] font-bold uppercase" style={{ color: isFull ? C.rose500 : isSelected ? C.emerald50 : C.slate500 }}>
                          {isFull ? 'HẾT CHỖ' : 'Slot'}
                        </Text>
                      </View>
                    </View>
                  </Pressable>
                );
              })}
            </View>

            <Text className="text-[14px] font-bold text-slate-900 mb-3">3. Thông tin thanh toán</Text>
            <View className="bg-slate-50 p-4 rounded-2xl border border-slate-100 mb-6">
              <View className="flex-row justify-between mb-2">
                <Text className="text-[13px] text-slate-500">Học phí gốc</Text>
                <Text className="text-[13px] font-medium text-slate-700">{course.price?.toLocaleString('vi-VN')}đ</Text>
              </View>
              <View className="flex-row justify-between items-center mt-2 pt-2 border-t border-slate-200">
                <Text className="text-[14px] font-bold text-slate-900">Tổng thanh toán</Text>
                <Text className="text-[20px] font-black text-indigo-600">{course.price?.toLocaleString('vi-VN')}đ</Text>
              </View>
            </View>
          </ScrollView>
        )}

        {!isFetchingData && (
          <View className="px-5 pt-3 border-t border-slate-100 bg-white">
            <Pressable
              onPress={handleBookingRequest}
              disabled={!canSubmit || isSubmitting}
              className="py-4 rounded-2xl items-center flex-row justify-center gap-2"
              style={[{ backgroundColor: canSubmit ? C.indigo600 : C.slate200 }, canSubmit && styles.primaryShadow]}
            >
              {isSubmitting ? (
                <ActivityIndicator color="white" size="small" />
              ) : (
                <Ionicons name="bookmark" size={18} color={canSubmit ? 'white' : C.slate400} />
              )}
              <Text className="font-black text-[15px]" style={{ color: canSubmit ? C.white : C.slate400 }}>
                {isSubmitting ? 'ĐANG GIỮ CHỖ...' : 'XÁC NHẬN GIỮ CHỖ'}
              </Text>
            </Pressable>
            <Text className="text-center text-[11px] font-medium text-slate-500 mt-3">
              Không lo mất slot. Giữ chỗ an toàn trong 48 giờ.
            </Text>
          </View>
        )}

        {/* Modal Hỏi Xác Nhận */}
        {confirmingClass && !showResult && (
          <View
            className="items-center justify-center px-6 rounded-t-[32px]"
            style={[StyleSheet.absoluteFill, styles.confirmOverlay]}
          >
            <View className="bg-white w-full rounded-3xl p-6" style={styles.cardShadow}>
              <View className="w-14 h-14 bg-indigo-50 rounded-full items-center justify-center mb-4">
                <Feather name="help-circle" size={28} color={C.indigo600} />
              </View>
              <Text className="text-[18px] font-black text-slate-900 mb-2">Xác nhận lịch học</Text>
              <Text className="text-[14px] text-slate-600 leading-relaxed mb-6">
                Bạn có chắc chắn muốn chọn <Text className="font-bold text-slate-800">{confirmingClass.name}</Text> cho bé không?
              </Text>

              <View className="flex-row gap-3">
                <Pressable
                  onPress={() => setConfirmingClass(null)}
                  className="flex-1 bg-slate-100 py-3.5 rounded-xl items-center justify-center"
                >
                  <Text className="font-bold text-slate-600">Hủy</Text>
                </Pressable>
                <Pressable
                  onPress={executeClassSelection}
                  className="flex-1 bg-indigo-600 py-3.5 rounded-xl items-center justify-center"
                >
                  <Text className="font-bold text-white">Đồng ý</Text>
                </Pressable>
              </View>
            </View>
          </View>
        )}

        {/* Modal Kết Quả */}
        {showResult && (
          <View
            className="items-center justify-center px-6 rounded-t-[32px]"
            style={[StyleSheet.absoluteFill, styles.resultOverlay]}
          >
            <View className="bg-white w-full rounded-3xl p-6 items-center" style={styles.cardShadow}>
              <View className="w-20 h-20 bg-emerald-100 rounded-full items-center justify-center mb-5 border-4 border-emerald-50">
                <Feather name="check" size={40} color="#059669" />
              </View>

              <Text className="text-[20px] font-black text-slate-900 mb-2 text-center">Giữ chỗ thành công!</Text>
              <Text className="text-[14px] text-slate-500 text-center leading-relaxed mb-8 px-2">
                Slot của bé đã được bảo lưu an toàn. Hóa đơn sẽ hết hạn sau <Text className="font-bold text-rose-500">48 giờ</Text>, bạn có muốn thanh toán ngay bây giờ không?
              </Text>

              <View className="w-full gap-3">
                <Pressable
                  onPress={handlePayNow}
                  className="w-full bg-indigo-600 py-4 rounded-xl items-center justify-center flex-row gap-2"
                >
                  <Text className="font-black text-[15px] text-white">Thanh toán VNPay ngay</Text>
                  <Feather name="arrow-right" size={18} color="white" />
                </Pressable>

                <Pressable
                  onPress={handlePayLater}
                  className="w-full bg-slate-100 py-4 rounded-xl items-center justify-center"
                >
                  <Text className="font-bold text-[15px] text-slate-600">Thanh toán sau</Text>
                </Pressable>
              </View>
            </View>
          </View>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  overlay: { zIndex: 100, elevation: 100, backgroundColor: 'rgba(15,23,42,0.6)' },
  confirmOverlay: { zIndex: 110, backgroundColor: 'rgba(15,23,42,0.4)' },
  resultOverlay: { zIndex: 120, backgroundColor: 'rgba(15,23,42,0.8)' },
  primaryShadow: {
    shadowColor: '#4f46e5',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 4,
  },
  cardShadow: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.2,
    shadowRadius: 16,
    elevation: 8,
  },
});