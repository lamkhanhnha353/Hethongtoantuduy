import { useState, useEffect, useRef } from 'react';
import { View, Text, ScrollView, TouchableOpacity, Image, ImageBackground, Alert, ActivityIndicator, Dimensions } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Feather, Ionicons } from '@expo/vector-icons';
import { VideoView, useVideoPlayer, type VideoSource } from 'expo-video';

import { API_URL } from '@/config/env';
import { EnrollmentBottomSheet } from '@/components/courses/EnrollmentBottomSheet';
import { paymentService } from '@/services/paymentService';
import { useSession } from '@/hooks/useSession';

const { width } = Dimensions.get('window');
const SLIDE_WIDTH = width - 40;
const DEFAULT_VIDEO_THUMBNAIL =
  'https://images.unsplash.com/photo-1503676260728-1c00da094a0b?q=80&w=1000&auto=format&fit=crop';

function getPlayableVideoSource(value: unknown) {
  if (typeof value !== 'string' || !value.trim()) return null;

  const rawUrl = value.trim();
  let url: URL;

  try {
    url = new URL(rawUrl);
  } catch {
    return rawUrl;
  }

  if (url.protocol !== 'https:' && url.protocol !== 'http:') return null;

  const isCloudinaryVideo = url.hostname.endsWith('res.cloudinary.com')
    && url.pathname.includes('/video/upload/');

  if (!isCloudinaryVideo || url.pathname.includes('/s--')) {
    return url.pathname.toLowerCase().endsWith('.m3u8')
      ? { uri: url.toString(), contentType: 'hls' as const }
      : url.toString();
  }

  url.pathname = url.pathname.replace(/(^|[,/])f_auto(?=,|\/|$)/g, '$1f_mp4');
  const fileName = url.pathname.split('/').pop() ?? '';

  if (fileName.toLowerCase().endsWith('.m3u8')) {
    return { uri: url.toString(), contentType: 'hls' as const };
  }

  if (!fileName.toLowerCase().endsWith('.mp4')) {
    url.pathname = url.pathname.replace(/\.(mov|webm|m4v|avi|mkv)$/i, '');
    url.pathname = `${url.pathname}.mp4`;

    if (!/(^|[,/])f_mp4(?=,|\/|$)/i.test(url.pathname)) {
      url.pathname = url.pathname.replace('/video/upload/', '/video/upload/f_mp4/');
    }
  }

  return url.toString();
}

function getCloudinaryVideoThumbnail(value: unknown) {
  if (typeof value !== 'string' || !value.trim()) return null;

  try {
    const url = new URL(value.trim());
    const uploadPath = '/video/upload/';
    const uploadIndex = url.pathname.indexOf(uploadPath);

    if (url.hostname.endsWith('res.cloudinary.com') && uploadIndex !== -1 && !url.pathname.includes('/s--')) {
      const pathAfterUpload = url.pathname.slice(uploadIndex + uploadPath.length);
      url.pathname = `${url.pathname.slice(0, uploadIndex + uploadPath.length)}so_0,f_jpg/${pathAfterUpload}`;
      url.pathname = url.pathname.replace(/\.(mp4|mov|webm|m4v|avi|mkv|m3u8)$/i, '.jpg');
      url.pathname = url.pathname.replace(/(^|[,/])f_auto(?=,|\/|$)/g, '$1f_jpg');
      return url.toString();
    }
  } catch {
    return null;
  }

  return null;
}

function CourseVideoPlayer({
  source,
  thumbnail,
  topInset,
  onClose,
}: {
  source: VideoSource;
  thumbnail: string;
  topInset: number;
  onClose: () => void;
}) {
  const [videoError, setVideoError] = useState<string | null>(null);
  const [hasRenderedFirstFrame, setHasRenderedFirstFrame] = useState(false);
  const player = useVideoPlayer(source, videoPlayer => {
    videoPlayer.muted = false;
  });

  useEffect(() => {
    const subscription = player.addListener('statusChange', event => {
      if (event.status === 'readyToPlay') {
        player.play();
      } else if (event.status === 'error') {
        setVideoError(event.error?.message ?? 'Không thể tải video. Vui lòng kiểm tra lại đường dẫn video.');
      }
    });

    if (player.status === 'readyToPlay') {
      player.play();
    }

    return () => {
      subscription.remove();
      player.pause();
    };
  }, [player]);

  return (
    <View className="w-full h-72 bg-black relative">
      <TouchableOpacity
        onPress={onClose}
        className="absolute left-4 z-20 w-10 h-10 rounded-full bg-black/50 border border-white/30 items-center justify-center"
        style={{ top: topInset + 8 }}
      >
        <Ionicons color="white" name="close" size={24} />
      </TouchableOpacity>

      <VideoView
        player={player}
        style={{ width: '100%', height: '100%' }}
        nativeControls
        contentFit="contain"
        playsInline
        surfaceType="textureView"
        onFirstFrameRender={() => setHasRenderedFirstFrame(true)}
      />
      {!hasRenderedFirstFrame && !videoError && player.status !== 'error' && (
        <View className="absolute inset-0 items-center justify-center bg-black">
          <Image
            source={{ uri: thumbnail }}
            className="absolute inset-0 h-full w-full"
            resizeMode="cover"
          />
          <ActivityIndicator size="large" color="#ffffff" />
          <Text className="mt-2 rounded bg-black/60 px-3 py-1 text-xs text-white">
            Đang tải video...
          </Text>
        </View>
      )}
      {(videoError || player.status === 'error') && (
        <Text className="absolute bottom-3 left-4 right-4 rounded-lg bg-black/70 p-2 text-center text-xs text-white">
          Video không thể phát: {videoError ?? 'Không thể tải video. Vui lòng kiểm tra lại đường dẫn video.'}
        </Text>
      )}
    </View>
  );
}

export default function CourseDetailScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { id } = useLocalSearchParams(); 
  const { session } = useSession();

  const [course, setCourse] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [showEnrollmentSheet, setShowEnrollmentSheet] = useState(false);
  const [isProcessingPayment, setIsProcessingPayment] = useState(false);
  
  const [activeSlide, setActiveSlide] = useState(0);
  const scrollRef = useRef<ScrollView>(null);
  const [activeFaq, setActiveFaq] = useState<number | null>(null);

  const [isVideoPlaying, setIsVideoPlaying] = useState(false);

  useEffect(() => {
    const fetchCourseDetail = async () => {
      try {
        const response = await fetch(`${API_URL}/api/courselevels/${id}`);
        if (!response.ok) throw new Error("Không thể tải thông tin khóa học");
        
        const realData = await response.json();
        
        setCourse({
          id: realData.id,
          code: realData.code,
          name: realData.courseName ? `${realData.courseName} - ${realData.name}` : realData.name,
          price: realData.fee,
          totalSessions: realData.totalSessions,
          status: realData.status,
          courseId: realData.courseId,
          
          description: realData.description || 'Đang cập nhật mô tả...',
          minAge: realData.minAge || 0,
          maxAge: realData.maxAge || 0,
          rating: realData.rating || 5.0,
          reviews: realData.reviews || 0,
          
          videoThumbnail: getCloudinaryVideoThumbnail(realData.videoUrl) ?? DEFAULT_VIDEO_THUMBNAIL,
          videoUrl: getPlayableVideoSource(realData.videoUrl),
          
          benefits: realData.benefits || [],
          facilityImages: realData.facilityImages || [],
          syllabus: realData.syllabus || [],
          faqs: realData.faqs || [],
          
          testimonials: [
            { parent: 'Mẹ bé Bảo Nam', comment: '"Bé nhà mình từ chỗ sợ học Toán nay đã tự giác làm bài, tính nhẩm nhanh hơn cả mẹ bấm máy."', avatar: 'https://randomuser.me/api/portraits/women/44.jpg' },
            { parent: 'Ba bé Minh An', comment: '"Chương trình học rất thực tế, đội ngũ giáo viên tận tình và luôn bám sát tiến độ của con."', avatar: 'https://randomuser.me/api/portraits/men/32.jpg' }
          ]
        });
        setIsVideoPlaying(false);
      } catch (error) {
        Alert.alert("Lỗi", "Đã xảy ra lỗi khi tải dữ liệu.");
        router.back();
      } finally {
        setIsLoading(false);
      }
    };

    if (id) fetchCourseDetail();
  }, [id]);

  useEffect(() => {
    if (!course?.facilityImages?.length) return;
    const interval = setInterval(() => {
      setActiveSlide((prev) => {
        const next = (prev + 1) % course.facilityImages.length;
        scrollRef.current?.scrollTo({ x: next * SLIDE_WIDTH, animated: true });
        return next;
      });
    }, 5000);
    return () => clearInterval(interval);
  }, [course]);

  const getAccessToken = () => {
    if (typeof session === 'string') {
      try { return JSON.parse(session).accessToken || session; } catch { return session; }
    }
    return session && typeof session === 'object' ? (session as any).accessToken : '';
  };

  const handleConfirmEnrollment = (studentId: number, classId: number) =>
    paymentService.createEnrollmentReservation(studentId, classId, course.price, getAccessToken());

  const handleScroll = (event: any) => {
    const scrollPosition = event.nativeEvent.contentOffset.x;
    const currentSlide = Math.round(scrollPosition / SLIDE_WIDTH);
    setActiveSlide(currentSlide);
  };

  const handlePlayVideo = () => {
    if (course?.videoUrl) {
      setIsVideoPlaying(true);
    } else {
      Alert.alert('Thông báo', 'Khóa học này chưa cập nhật video giới thiệu.');
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
        
        {/* HERO SECTION - XỬ LÝ VIDEO IN-APP */}
        {isVideoPlaying ? (
          <CourseVideoPlayer
            source={course.videoUrl}
            thumbnail={course.videoThumbnail}
            topInset={insets.top}
            onClose={() => setIsVideoPlaying(false)}
          />
        ) : (
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
                <Feather color="white" name="share-2" size={18}/>
              </TouchableOpacity>
            </View>

            <View className="items-center justify-center flex-1 z-10">
              <TouchableOpacity onPress={handlePlayVideo} className="w-16 h-16 rounded-full bg-white/30 items-center justify-center border-2 border-white/50 backdrop-blur-md pl-1">
                <Ionicons color="white" name="play" size={32}/>
              </TouchableOpacity>
              <Text className="text-white font-semibold mt-2 drop-shadow-md">Xem video giới thiệu</Text>
            </View>

            <View className="px-5 pb-6 z-10">
              <View className="flex-row items-center gap-2 mb-2">
                <View className={`${course.status?.toLowerCase() === 'open' || course.status?.toLowerCase() === 'active' ? 'bg-emerald-500' : 'bg-slate-500'} px-2 py-0.5 rounded-md`}>
                  <Text className="text-white text-[10px] font-bold uppercase tracking-widest">
                    {course.status?.toLowerCase() === 'open' || course.status?.toLowerCase() === 'active' ? 'Đang chiêu sinh' : 'Sắp mở'}
                  </Text>
                </View>
                <View className="flex-row items-center gap-1 bg-black/40 px-2 py-0.5 rounded-md">
                  <Ionicons color="#fbbf24" name="star" size={12}/>
                  <Text className="text-white text-[11px] font-bold">{course.rating} ({course.reviews} đánh giá)</Text>
                </View>
              </View>
              <Text className="text-white text-3xl font-black leading-tight drop-shadow-md">{course.name}</Text>
            </View>
          </ImageBackground>
        )}

        <View className="px-5 pt-6 pb-32">
          {/* THỐNG KÊ NHANH */}
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

          {/* MÔ TẢ & LỢI ÍCH */}
          <Text className="text-[19px] font-black text-slate-900 mb-3">Tổng quan khóa học</Text>
          <Text className="text-[15px] text-slate-600 leading-relaxed mb-6">{course.description}</Text>

          {course.benefits?.length > 0 && (
            <>
              <Text className="text-[19px] font-black text-slate-900 mb-4">Con bạn sẽ đạt được gì?</Text>
              <View className="flex-row flex-wrap gap-3 mb-8">
                {course.benefits.map((item: string, index: number) => (
                  <View className="bg-indigo-50 px-4 py-2.5 rounded-xl border border-indigo-100 flex-row items-center gap-2" key={index}>
                    <Feather color="#4f46e5" name="check-circle" size={16}/>
                    <Text className="text-[13px] text-indigo-900 font-semibold">{item}</Text>
                  </View>
                ))}
              </View>
            </>
          )}

          {/* LỘ TRÌNH MINH BẠCH */}
          {course.syllabus?.length > 0 && (
            <>
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
            </>
          )}

          {/* CƠ SỞ VẬT CHẤT (CAROUSEL) */}
          {course.facilityImages?.length > 0 && (
            <>
              <Text className="text-[19px] font-black text-slate-900 mb-4">Môi trường học tập</Text>
              <View className="mb-8">
                <ScrollView 
                  ref={scrollRef}
                  horizontal 
                  pagingEnabled 
                  showsHorizontalScrollIndicator={false}
                  onScroll={handleScroll}
                  scrollEventThrottle={16}
                  className="rounded-3xl overflow-hidden border border-slate-100 bg-white"
                >
                  {course.facilityImages.map((imageUrl: string, index: number) => (
                    <View key={index} style={{ width: SLIDE_WIDTH }} className="h-48 relative">
                      <Image source={{ uri: imageUrl }} className="w-full h-full" resizeMode="cover" />
                      <View className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/60 to-transparent h-16 justify-end p-3">
                        <Text className="text-white font-bold text-[13px]">Cơ sở vật chất chuẩn quốc tế</Text>
                      </View>
                    </View>
                  ))}
                </ScrollView>
                
                <View className="flex-row justify-center mt-3 gap-2">
                  {course.facilityImages.map((_: any, index: number) => (
                    <View 
                      key={index} 
                      className={`h-1.5 rounded-full ${index === activeSlide ? 'w-6 bg-indigo-600' : 'w-1.5 bg-slate-300'}`} 
                    />
                  ))}
                </View>
              </View>
            </>
          )}

          {/* CÂU HỎI THƯỜNG GẶP (FAQ) */}
          {course.faqs?.length > 0 && (
            <>
              <Text className="text-[19px] font-black text-slate-900 mb-4">Câu hỏi thường gặp</Text>
              <View className="mb-8">
                {course.faqs.map((faq: any, index: number) => {
                  const isOpen = activeFaq === index;
                  return (
                    <View key={index} className="bg-white rounded-2xl mb-3 shadow-sm shadow-slate-200/50 border border-slate-100 overflow-hidden">
                      <TouchableOpacity 
                        activeOpacity={0.7} 
                        className="p-4 flex-row items-center justify-between"
                        onPress={() => setActiveFaq(isOpen ? null : index)}
                      >
                        <Text className={`flex-1 text-[14px] font-bold ${isOpen ? 'text-indigo-600' : 'text-slate-800'}`}>
                          {faq.question}
                        </Text>
                        <View className={`w-6 h-6 rounded-full items-center justify-center ${isOpen ? 'bg-indigo-100' : 'bg-slate-100'}`}>
                          <Feather name={isOpen ? "chevron-up" : "chevron-down"} size={16} color={isOpen ? "#4f46e5" : "#64748b"} />
                        </View>
                      </TouchableOpacity>
                      {isOpen && (
                        <View className="px-4 pb-4 pt-1">
                          <Text className="text-[13.5px] text-slate-600 leading-relaxed">
                            {faq.answer}
                          </Text>
                        </View>
                      )}
                    </View>
                  );
                })}
              </View>
            </>
          )}

          {/* SOCIAL PROOF (ĐÁNH GIÁ THỰC TẾ) */}
          <Text className="text-[19px] font-black text-slate-900 mb-4">Phụ huynh nói gì về chúng tôi</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 12 }}>
            {course.testimonials.map((item: any, index: number) => (
              <View key={index} className="bg-white border border-slate-100 p-4 rounded-2xl w-[280px] shadow-sm shadow-slate-200/50">
                <Text className="text-slate-600 italic text-[14px] leading-relaxed mb-4">{item.comment}</Text>
                <View className="flex-row items-center gap-3">
                  <Image source={{ uri: item.avatar }} className="w-10 h-10 rounded-full bg-slate-200" />
                  <View>
                    <Text className="font-bold text-slate-800 text-[13px]">{item.parent}</Text>
                    <View className="flex-row mt-0.5">
                      {[1, 2, 3, 4, 5].map(i => <Ionicons key={i} name="star" size={10} color="#fbbf24" />)}
                    </View>
                  </View>
                </View>
              </View>
            ))}
          </ScrollView>
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

      <EnrollmentBottomSheet 
        visible={showEnrollmentSheet}
        onClose={() => setShowEnrollmentSheet(false)}
        course={course}
        onConfirm={handleConfirmEnrollment}
        onPayInvoice={undefined}
      />

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