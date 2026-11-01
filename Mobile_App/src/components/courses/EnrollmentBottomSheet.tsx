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
  onConfirm: (studentId: number, classId: number) => Promise<number>;
  onPayInvoice: (invoiceId: number, paymentUrl?: string) => void;
}

interface CourseEnrollment {
  invoiceId: number | null;
  studentId: number;
  classId: number;
  className: string;
  studentName: string;
  status: 'ACTIVE' | 'PENDING';
}

const isValidInvoiceId = (invoiceId: number | null | undefined): invoiceId is number =>
  typeof invoiceId === 'number' && Number.isInteger(invoiceId) && invoiceId > 0;

export function EnrollmentBottomSheet({ visible, onClose, course, onConfirm, onPayInvoice }: EnrollmentBottomSheetProps) {
  const insets = useSafeAreaInsets();
  const { session } = useSession(); 
  
  const [students, setStudents] = useState<any[]>([]);
  const [classes, setClasses] = useState<any[]>([]);
  const [enrollments, setEnrollments] = useState<CourseEnrollment[]>([]);
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

        const enrollmentRes = await fetch(
          `${API_URL}/api/enrollment/course-level/${course.id}/registrations`,
          { headers: { Authorization: ['Bearer', token].join(' ') } }
        );
        if (!enrollmentRes.ok) {
          throw new Error('Không thể tải trạng thái đăng ký khóa học.');
        }
        const enrollmentData = await enrollmentRes.json();

        const safeStudents = Array.isArray(studentData) ? studentData : [];
        const safeClasses = Array.isArray(classData) ? classData : [];
        const safeEnrollments: CourseEnrollment[] = Array.isArray(enrollmentData)
          ? enrollmentData
              .map(item => {
                const rawInvoiceId = item.invoiceId ?? item.InvoiceId;
                const invoiceId = rawInvoiceId === null || rawInvoiceId === undefined
                  ? null
                  : Number(rawInvoiceId);
                const studentId = Number(item.studentId ?? item.StudentId);
                const classId = Number(item.classId ?? item.ClassId);

                return {
                  invoiceId: invoiceId !== null && Number.isInteger(invoiceId) && invoiceId > 0 ? invoiceId : null,
                  studentId,
                  classId,
                  className: item.className ?? item.ClassName ?? 'Lớp đã chọn',
                  studentName: item.studentName ?? item.StudentName ?? '',
                  status: (item.status ?? item.Status) as CourseEnrollment['status'],
                };
              })
              .filter(enrollment =>
                Number.isInteger(enrollment.studentId)
                && Number.isInteger(enrollment.classId)
                && (enrollment.status === 'ACTIVE' || enrollment.status === 'PENDING')
              )
          : [];

        setStudents(safeStudents);
        setClasses(safeClasses);
        setEnrollments(safeEnrollments);
        setSelectedClass(null);

        if (safeStudents.length > 0) {
          setSelectedStudent(safeStudents[0].id);
        } else {
          setSelectedStudent(null);
        }
      } catch (error) {
        console.error("Lỗi fetch enrollment data:", error);
        Alert.alert('Lỗi', error instanceof Error ? error.message : 'Không thể tải thông tin đăng ký.');
      } finally {
        setIsFetchingData(false);
      }
    };

    fetchEnrollmentData();
  }, [visible, course, session]);

  if (!visible || !course) return null;

  const selectedEnrollment = enrollments.find(enrollment => enrollment.studentId === selectedStudent);
  const canSubmit = !!selectedStudent && (
    selectedEnrollment?.status === 'PENDING'
      ? true
      : !selectedEnrollment && !!selectedClass
  );
  const selectedStudentName = students.find(student => student.id === selectedStudent)?.fullName ?? 'Bé';

  const showAlreadyEnrolledMessage = () => {
    if (!selectedEnrollment) return false;

    const message = selectedEnrollment.status === 'PENDING'
      ? isValidInvoiceId(selectedEnrollment.invoiceId)
        ? `${selectedStudentName} đã có hóa đơn chờ thanh toán tại lớp ${selectedEnrollment.className}. Vui lòng thanh toán hóa đơn hiện tại.`
        : `${selectedStudentName} vẫn đang giữ chỗ tại lớp ${selectedEnrollment.className}. Hãy tạo lại hóa đơn bằng nút bên dưới để tiếp tục.`
      : `${selectedStudentName} đã đăng ký khóa học này tại lớp ${selectedEnrollment.className}.`;

    Alert.alert('Đã đăng ký khóa học', message);
    return true;
  };

  const createReservation = async (studentId: number, classId: number, seatAlreadyReserved = false) => {
    setIsSubmitting(true);
    try {
      const invoiceId = await onConfirm(studentId, classId);
      const reservedClass = classes.find(cls => cls.id === classId);
      const enrollment: CourseEnrollment = {
        invoiceId,
        studentId,
        classId,
        className: reservedClass?.name ?? 'Lớp đã chọn',
        studentName: students.find(student => student.id === studentId)?.fullName ?? 'Bé',
        status: 'PENDING',
      };

      setEnrollments(current => [
        ...current.filter(item => item.studentId !== studentId),
        enrollment,
      ]);
      if (!seatAlreadyReserved) {
        setClasses(current => current.map(cls =>
          cls.id === classId
            ? { ...cls, activeStudentsCount: (cls.activeStudentsCount ?? 0) + 1 }
            : cls
        ));
      }
      setShowResult(true);
    } catch (error) {
      console.error('Lỗi giữ chỗ khóa học:', error);
      Alert.alert('Không thể giữ chỗ', error instanceof Error ? error.message : 'Đã xảy ra lỗi khi đăng ký.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleBookingRequest = async () => {
    if (!selectedStudent) return;
    if (selectedEnrollment?.status === 'ACTIVE') {
      showAlreadyEnrolledMessage();
      return;
    }
    if (selectedEnrollment?.status === 'PENDING') {
      if (isValidInvoiceId(selectedEnrollment.invoiceId)) {
        onPayInvoice(selectedEnrollment.invoiceId);
      } else {
        await createReservation(selectedStudent, selectedEnrollment.classId, true);
      }
      return;
    }
    if (!selectedClass) return;

    await createReservation(selectedStudent, selectedClass);
  };

  const handlePayNow = () => {
    setShowResult(false);
    if (selectedEnrollment && isValidInvoiceId(selectedEnrollment.invoiceId)) {
      onPayInvoice(selectedEnrollment.invoiceId);
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
                const studentEnrollment = enrollments.find(enrollment => enrollment.studentId === student.id);
                const hasEnrollment = !!studentEnrollment;
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
                      {hasEnrollment && (
                        <Text className="text-[10px] font-bold text-emerald-700">
                          {studentEnrollment.status === 'PENDING' ? 'Đã giữ chỗ - chờ thanh toán' : 'Đã đăng ký khóa học'}
                        </Text>
                      )}
                    </View>
                  </Pressable>
                );
              })}

              <Pressable className="flex-row items-center gap-2 px-4 py-3 rounded-2xl border border-dashed border-slate-300 bg-slate-50">
                <Feather name="plus" size={18} color={C.slate500} />
                <Text className="text-[13px] font-bold text-slate-600">Thêm hồ sơ</Text>
              </Pressable>
            </View>
            {selectedEnrollment && (
              <View className="flex-row items-start gap-2 p-3 rounded-xl bg-emerald-50 border border-emerald-100 mb-6">
                <Feather
                  name={selectedEnrollment.status === 'PENDING' ? 'clock' : 'check-circle'}
                  size={17}
                  color={C.emerald700}
                />
                <Text className="flex-1 text-[12px] font-semibold text-emerald-900">
                  {selectedEnrollment.status === 'PENDING'
                    ? isValidInvoiceId(selectedEnrollment.invoiceId)
                      ? `${selectedStudentName} đã giữ chỗ tại lớp ${selectedEnrollment.className}; hóa đơn đang chờ thanh toán.`
                      : `${selectedStudentName} vẫn đang giữ chỗ tại lớp ${selectedEnrollment.className}; hóa đơn trước chưa thanh toán thành công. Hãy tạo lại hóa đơn để tiếp tục.`
                    : `${selectedStudentName} đã đăng ký khóa học này tại lớp ${selectedEnrollment.className}.`}
                </Text>
              </View>
            )}

            <View className="flex-row items-center justify-between mb-3">
              <Text className="text-[14px] font-bold text-slate-900">2. Chọn lịch học phù hợp</Text>
              <Text className="text-[12px] font-medium text-slate-500">Đang mở {classes.length} lớp</Text>
            </View>
            <View className="gap-3 mb-6">
              {classes.length === 0 ? (
                 <Text className="text-slate-500 text-sm py-4 text-center">Hiện chưa có lớp học nào được mở cho khóa này.</Text>
              ) : classes.map((cls) => {
                const isSelected = selectedClass === cls.id;
                const isReservedForSelectedStudent = selectedEnrollment?.classId === cls.id;
                
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
                        if (selectedEnrollment) {
                          showAlreadyEnrolledMessage();
                          return;
                        }
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
                        {isReservedForSelectedStudent && selectedEnrollment?.status === 'PENDING' && (
                          <View className="self-start flex-row items-center gap-1 bg-amber-100 px-2 py-1 rounded-lg mb-2">
                            <Feather name="clock" size={12} color="#b45309" />
                            <Text className="text-[10px] font-bold text-amber-800">ĐÃ ĐĂNG KÝ - CHỜ THANH TOÁN</Text>
                          </View>
                        )}
                        {isReservedForSelectedStudent && selectedEnrollment?.status === 'ACTIVE' && (
                          <View className="self-start flex-row items-center gap-1 bg-emerald-100 px-2 py-1 rounded-lg mb-2">
                            <Feather name="check-circle" size={12} color="#047857" />
                            <Text className="text-[10px] font-bold text-emerald-800">ĐÃ THANH TOÁN</Text>
                          </View>
                        )}
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
            
            {/* Cảnh báo trạng thái đăng ký */}
            {selectedEnrollment && selectedEnrollment.status === 'ACTIVE' && (
              <View className="bg-emerald-50 p-4 rounded-2xl border border-emerald-200 mb-4 flex-row items-center gap-3">
                <Feather name="check-circle" size={20} color="#059669" />
                <Text className="text-emerald-700 text-[13px] font-medium flex-1">
                  Bé đã được ghi danh chính thức vào lớp học này. Bạn không cần thao tác gì thêm.
                </Text>
              </View>
            )}

            {selectedEnrollment && selectedEnrollment.status === 'PENDING' && (
              <View className="bg-amber-50 p-4 rounded-2xl border border-amber-200 mb-4 flex-row items-center gap-3">
                <Feather name="alert-circle" size={20} color="#d97706" />
                <Text className="text-amber-700 text-[13px] font-medium flex-1">
                  {isValidInvoiceId(selectedEnrollment.invoiceId)
                    ? 'Bé đã được giữ chỗ tại lớp này. Hóa đơn chưa thanh toán; nhấn nút bên dưới để thanh toán ngay hoặc quay lại sau.'
                    : 'Bé vẫn đang giữ chỗ tại lớp này nhưng hóa đơn trước không còn hiệu lực. Nhấn nút bên dưới để tạo hóa đơn mới.'}
                </Text>
              </View>
            )}

            {/* Chỉ hiển thị khối tiền tệ nếu bé chưa đăng ký lớp này */}
            {(!selectedEnrollment || (selectedEnrollment && selectedEnrollment.status !== 'ACTIVE')) && (
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
            )}
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
                {isSubmitting
                  ? 'ĐANG XỬ LÝ...'
                  : selectedEnrollment
                    ? selectedEnrollment.status === 'PENDING' 
                        ? isValidInvoiceId(selectedEnrollment.invoiceId) ? 'THANH TOÁN HÓA ĐƠN' : 'TẠO LẠI HÓA ĐƠN'
                        : 'BÉ ĐÃ ĐĂNG KÝ'
                    : 'XÁC NHẬN GIỮ CHỖ'}
              </Text>
            </Pressable>
            <Text className="text-center text-[11px] font-medium text-slate-500 mt-3">
              Slot được giữ khi hóa đơn đang chờ thanh toán. Hãy thanh toán hoặc hủy hóa đơn để giải phóng chỗ.
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

              <Text className="text-[20px] font-black text-slate-900 mb-2 text-center">Đăng ký giữ chỗ thành công!</Text>
              <Text className="text-[14px] text-slate-500 text-center leading-relaxed mb-8 px-2">
                Bé {selectedStudentName} đã được giữ chỗ tại lớp {selectedEnrollment?.className} và đang chờ thanh toán. Vui lòng thanh toán để hoàn tất đăng ký. Bạn có thể thanh toán ngay hoặc quay lại sau.
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