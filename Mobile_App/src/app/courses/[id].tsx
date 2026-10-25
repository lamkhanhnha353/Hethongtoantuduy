import { useState, useEffect } from 'react';
import { View, Text, ScrollView, TouchableOpacity, Image, ImageBackground, Alert, ActivityIndicator } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Feather, Ionicons } from '@expo/vector-icons';
import * as WebBrowser from 'expo-web-browser'; // Import thư viện mở trình duyệt in-app

import { API_URL } from '@/config/env';
import { EnrollmentBottomSheet } from '@/components/courses/EnrollmentBottomSheet';
import { paymentService } from '@/services/paymentService'; // Import service thanh toán
import { useSession } from '@/hooks/useSession'; // Import hook lấy token

import * as Linking from 'expo-linking';
// Dữ liệu tĩnh (Mock) cho các trường DB chưa có để giao diện đẹp mắt
const STATIC_UI_DATA = {
  description: 'Khóa học phát triển tư duy toàn diện, áp dụng phương pháp chuẩn quốc tế giúp trẻ tiếp thu nhanh, tự tin và đạt kết quả cao trong học tập.',
  minAge: 5,
  maxAge: 12,
  videoThumbnail: 'https://images.unsplash.com/photo-1503676260728-1c00da094a0b?q=80&w=1000&auto=format&fit=crop',
  mapImage: 'https://images.unsplash.com/photo-1524661135-423995f22d0b?q=80&w=1000&auto=format&fit=crop',
  benefits: [
    'Tính nhẩm siêu tốc không cần máy tính',
    'Phát triển cân bằng hai bán cầu não',
    'Tăng khả năng tập trung cao độ',
    'Tự tin hơn với môn Toán ở lớp'
  ],
  syllabus: [
    { phase: 'Giai đoạn 1', title: 'Làm quen cơ bản', desc: 'Nắm vững kiến thức nền tảng và phương pháp tư duy.' },
    { phase: 'Giai đoạn 2', title: 'Thực hành nâng cao', desc: 'Rèn luyện phản xạ và tốc độ xử lý bài tập.' }
  ],
  teacher: {
    name: 'Đội ngũ chuyên gia',
    exp: 'Nhiều năm kinh nghiệm giảng dạy',
    avatar: 'https://randomuser.me/api/portraits/women/44.jpg'
  },
  address: 'Trụ sở chính, Trung tâm Toán Tư Duy',
  rating: 4.8,
  reviews: 124
};

export default function CourseDetailScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { id } = useLocalSearchParams(); 
  const { session } = useSession(); // Lấy session từ custom hook

  const [course, setCourse] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  // State quản lý Bottom Sheet Đăng ký
  const [showEnrollmentSheet, setShowEnrollmentSheet] = useState(false);
  
  // State xử lý hiển thị Loading toàn màn hình khi đang gọi API VNPay
  const [isProcessingPayment, setIsProcessingPayment] = useState(false);

  useEffect(() => {
    const fetchCourseDetail = async () => {
      try {
        const response = await fetch(`${API_URL}/api/courselevels/${id}`);
        if (!response.ok) throw new Error("Không thể tải thông tin khóa học");
        
        const realData = await response.json();
        
        setCourse({
          ...STATIC_UI_DATA,
          id: realData.id,
          code: realData.code,
          name: realData.name,
          price: realData.fee,
          totalSessions: realData.totalSessions,
          status: realData.status,
          courseId: realData.courseId,
          color: (realData.code || '').toUpperCase().includes('FM') ? 'bg-indigo-600' : 
                 (realData.code || '').toUpperCase().includes('SB') ? 'bg-sky-500' : 'bg-blue-500'
        });
      } catch (error) {
        Alert.alert("Lỗi", "Đã xảy ra lỗi khi tải dữ liệu.");
        router.back();
      } finally {
        setIsLoading(false);
      }
    };

    if (id) fetchCourseDetail();
  }, [id]);

  const getAccessToken = () => {
    if (typeof session === 'string') {
      try {
        return JSON.parse(session).accessToken || session;
      } catch {
        return session;
      }
    }

    return session && typeof session === 'object'
      ? (session as any).accessToken
      : '';
  };

  const handleConfirmEnrollment = (studentId: number, classId: number) =>
    paymentService.createEnrollmentReservation(studentId, classId, course.price, getAccessToken());

  const handlePayInvoice = async (invoiceId: number, paymentUrl?: string) => {
    if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
      Alert.alert('Không thể thanh toán', 'Không tìm thấy mã hóa đơn hợp lệ. Vui lòng tải lại thông tin đăng ký.');
      return;
    }

    setShowEnrollmentSheet(false);
    setIsProcessingPayment(true);

    try {
      const vnpayUrl = paymentUrl ?? await paymentService.getVNPayUrlForInvoice(invoiceId, getAccessToken());
      let isLinkHandled = false;

      const subscription = Linking.addEventListener('url', (event) => {
        if (!isLinkHandled && event.url.includes('payment-result')) {
          isLinkHandled = true;
          const responseCode = new URL(event.url).searchParams.get('code');
          WebBrowser.dismissBrowser();
          router.push(`/payment-result?code=${responseCode}`);
        }
      });

      try {
        await WebBrowser.openBrowserAsync(vnpayUrl);
      } finally {
        subscription.remove();
      }
    } catch (error) {
      Alert.alert(
        'Lỗi thanh toán',
        error instanceof Error ? error.message : 'Đã xảy ra sự cố trong lúc xử lý thanh toán.'
      );
    } finally {
      setIsProcessingPayment(false);
    }
  };

  if (isLoading || !course) {
    return (
      <View className="flex-1 items-center justify-center bg-slate-50">
        <ActivityIndicator color="#4f46e5" size="large"/>
        <Text className="mt-4 text-slate-500 font-medium">Đang tải thông tin khóa học...</Text>
      </View>
    );
  }

  return (
    <View className="flex-1 bg-slate-50 relative">
      <ScrollView className="flex-1" showsVerticalScrollIndicator={false}>
        
        <ImageBackground 
          source={{ uri: course.videoThumbnail }}
          className="w-full h-72 justify-between" 
          imageStyle={{ borderBottomLeftRadius: 32, borderBottomRightRadius: 32 }}
        >
          <View className="absolute inset-0 bg-slate-900/40 rounded-b-[32px]"/>

          <View className="px-5 flex-row items-center justify-between z-10" style={{ paddingTop: insets.top + 10 }}>
            <TouchableOpacity onPress={() => router.back()} className="w-10 h-10 rounded-full bg-white/20 border border-white/30 items-center justify-center backdrop-blur-md">
              <Ionicons color="white" name="arrow-back" size={22}/>
            </TouchableOpacity>
            <TouchableOpacity className="w-10 h-10 rounded-full bg-white/20 border border-white/30 items-center justify-center backdrop-blur-md">
              <Feather color="white" name="heart" size={20}/>
            </TouchableOpacity>
          </View>

          <View className="items-center justify-center flex-1 z-10">
            <TouchableOpacity className="w-16 h-16 rounded-full bg-white/30 items-center justify-center border-2 border-white/50 backdrop-blur-md pl-1">
              <Ionicons color="white" name="play" size={32}/>
            </TouchableOpacity>
          </View>

          <View className="px-5 pb-6 z-10">
            <View className="flex-row items-center gap-2 mb-2">
              <View className={`${course.status?.toLowerCase() === 'open' ? 'bg-emerald-500' : 'bg-slate-500'} px-2 py-0.5 rounded-md`}>
                <Text className="text-white text-[10px] font-bold uppercase tracking-widest">
                  {course.status?.toLowerCase() === 'open' ? 'Đang chiêu sinh' : 'Sắp mở'}
                </Text>
              </View>
              <View className="flex-row items-center gap-1 bg-black/40 px-2 py-0.5 rounded-md">
                <Ionicons color="#fbbf24" name="star" size={12}/>
                <Text className="text-white text-[11px] font-bold">{course.rating} ({course.reviews})</Text>
              </View>
            </View>
            <Text className="text-white text-3xl font-black leading-tight drop-shadow-md">{course.name}</Text>
          </View>
        </ImageBackground>

        <View className="px-5 pt-6 pb-32">
          <View className="bg-white rounded-3xl p-5 shadow-sm shadow-slate-200/50 flex-row justify-between mb-8 border border-slate-100">
            <View className="items-center">
              <View className="w-10 h-10 rounded-full bg-orange-50 items-center justify-center mb-2"><Feather color="#f97316" name="smile" size={20}/></View>
              <Text className="text-[11px] text-slate-500 font-medium">Độ tuổi</Text>
              <Text className="text-[14px] font-extrabold text-slate-800">{course.minAge}-{course.maxAge}</Text>
            </View>
            <View className="w-[1px] bg-slate-100"/>
            <View className="items-center">
              <View className="w-10 h-10 rounded-full bg-blue-50 items-center justify-center mb-2"><Feather color="#3b82f6" name="calendar" size={20}/></View>
              <Text className="text-[11px] text-slate-500 font-medium">Thời lượng</Text>
              <Text className="text-[14px] font-extrabold text-slate-800">{Math.ceil(course.totalSessions / 8)} tháng</Text>
            </View>
            <View className="w-[1px] bg-slate-100"/>
            <View className="items-center">
              <View className="w-10 h-10 rounded-full bg-emerald-50 items-center justify-center mb-2"><Feather color="#10b981" name="book-open" size={20}/></View>
              <Text className="text-[11px] text-slate-500 font-medium">Số buổi</Text>
              <Text className="text-[14px] font-extrabold text-slate-800">{course.totalSessions}</Text>
            </View>
          </View>

          <Text className="text-[19px] font-black text-slate-900 mb-3">Tổng quan khóa học</Text>
          <Text className="text-[15px] text-slate-600 leading-relaxed mb-6">{course.description}</Text>

          <Text className="text-[19px] font-black text-slate-900 mb-4">Lợi ích đạt được</Text>
          <View className="flex-row flex-wrap gap-3 mb-8">
            {course.benefits.map((item: string, index: number) => (
              <View className="bg-indigo-50 px-4 py-2.5 rounded-xl border border-indigo-100 flex-row items-center gap-2" key={index}>
                <Feather color="#4f46e5" name="check-circle" size={16}/>
                <Text className="text-[13px] text-indigo-900 font-semibold">{item}</Text>
              </View>
            ))}
          </View>

          <Text className="text-[19px] font-black text-slate-900 mb-4">Lộ trình học tập</Text>
          <View className="bg-white rounded-3xl p-5 shadow-sm shadow-slate-200/50 border border-slate-100 mb-8">
            {course.syllabus.map((item: any, index: number) => (
              <View className="flex-row mb-6 last:mb-0 relative" key={index}>
                {index !== course.syllabus.length - 1 && <View className="absolute left-4 top-8 bottom-[-32px] w-[2px] bg-slate-100"/>}
                <View className="w-8 h-8 rounded-full bg-indigo-100 items-center justify-center mr-4 z-10 border-4 border-white">
                  <Text className="text-indigo-600 font-bold text-xs">{index + 1}</Text>
                </View>
                <View className="flex-1 pt-1">
                  <Text className="text-[11px] font-bold text-indigo-500 uppercase tracking-widest mb-1">{item.phase}</Text>
                  <Text className="text-[15px] font-bold text-slate-800 mb-1">{item.title}</Text>
                  <Text className="text-[13px] text-slate-500 leading-relaxed">{item.desc}</Text>
                </View>
              </View>
            ))}
          </View>

          <Text className="text-[19px] font-black text-slate-900 mb-4">Giáo viên phụ trách</Text>
          <View className="bg-white rounded-2xl p-4 shadow-sm shadow-slate-200/50 border border-slate-100 flex-row items-center gap-4 mb-8">
            <Image className="w-16 h-16 rounded-full bg-slate-200" source={{ uri: course.teacher.avatar }}/>
            <View className="flex-1">
              <Text className="text-[16px] font-bold text-slate-900 mb-1">{course.teacher.name}</Text>
              <Text className="text-[13px] text-slate-500">{course.teacher.exp}</Text>
            </View>
          </View>
          
          <Text className="text-[19px] font-black text-slate-900 mb-4">Địa điểm học</Text>
          <View className="bg-white rounded-3xl overflow-hidden shadow-sm shadow-slate-200/50 border border-slate-100 mb-4">
            <ImageBackground source={{ uri: course.mapImage }} className="w-full h-40 items-center justify-center">
              <View className="w-12 h-12 bg-rose-500 rounded-full items-center justify-center border-4 border-white/50 shadow-lg">
                <Ionicons name="location" size={24} color="white" />
              </View>
            </ImageBackground>
            <View className="p-4 flex-row items-start gap-3">
              <Feather name="map-pin" size={20} color="#4f46e5" className="mt-1" />
              <View className="flex-1">
                <Text className="text-[14px] font-bold text-slate-800 mb-1">Trụ sở chính</Text>
                <Text className="text-[13px] text-slate-500 leading-relaxed">{course.address}</Text>
              </View>
            </View>
          </View>
        </View>
      </ScrollView>

      {/* STICKY FOOTER */}
      <View 
        className="absolute bottom-0 left-0 right-0 bg-white border-t border-slate-200 flex-row items-center justify-between px-5 pt-3 pb-6 shadow-[0_-15px_30px_rgba(0,0,0,0.06)] z-50" 
        style={{ paddingBottom: insets.bottom > 0 ? insets.bottom + 10 : 24 }}
      >
        <View>
          <Text className="text-[11px] font-extrabold text-slate-400 uppercase tracking-widest mb-0.5">Học phí trọn khóa</Text>
          <Text className="text-[26px] font-black text-indigo-600 tracking-tight">
            {course.price.toLocaleString('vi-VN')}<Text className="text-xl">đ</Text>
          </Text>
        </View>

        <TouchableOpacity 
          activeOpacity={0.8} 
          className="bg-indigo-600 px-8 py-4 rounded-2xl shadow-lg shadow-indigo-300 flex-row items-center gap-2" 
          onPress={() => setShowEnrollmentSheet(true)}
        >
          <Text className="text-white font-black text-[15px]">ĐĂNG KÝ NGAY</Text>
          <Feather color="white" name="arrow-right" size={18}/>
        </TouchableOpacity>
      </View>

      {/* BOTTOM SHEET ĐĂNG KÝ */}
      <EnrollmentBottomSheet 
        visible={showEnrollmentSheet}
        onClose={() => setShowEnrollmentSheet(false)}
        course={course}
        onConfirm={handleConfirmEnrollment}
        onPayInvoice={handlePayInvoice}
      />

      {/* OVERLAY LOADING: Hiển thị khi đang chờ gọi API thanh toán */}
      {isProcessingPayment && (
        <View className="absolute inset-0 bg-slate-900/60 z-[100] items-center justify-center">
          <View className="bg-white px-8 py-6 rounded-2xl items-center shadow-2xl">
            <ActivityIndicator size="large" color="#4f46e5" className="mb-4" />
            <Text className="text-[16px] font-black text-slate-900 mb-1">Đang xử lý</Text>
            <Text className="text-[13px] text-slate-500">Đang khởi tạo cổng thanh toán VNPay...</Text>
          </View>
        </View>
      )}
    </View>
  );
}