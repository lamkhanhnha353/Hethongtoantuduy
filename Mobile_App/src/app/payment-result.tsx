import { useEffect } from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Feather } from '@expo/vector-icons';
import * as WebBrowser from 'expo-web-browser'; // Import thêm thư viện này

export default function PaymentResultScreen() {
  const { code } = useLocalSearchParams();
  const router = useRouter();
  const isSuccess = code === '00';

  useEffect(() => {
    // Ép đóng triệt để trình duyệt web đang chạy ngầm ngay khi màn hình kết quả hiện lên
    WebBrowser.dismissBrowser();
  }, []);

  return (
    <View className="flex-1 bg-white items-center justify-center p-6">
       <Feather name={isSuccess ? "check-circle" : "x-circle"} size={90} color={isSuccess ? "#10b981" : "#f43f5e"} />
       
       <Text className="text-[24px] font-black mt-6 text-slate-800 text-center">
         {isSuccess ? "Thanh toán thành công!" : "Giao dịch chưa hoàn tất"}
       </Text>
       
       <Text className="text-center text-[15px] text-slate-500 mt-3 mb-10 leading-relaxed">
         {isSuccess 
            ? "Hóa đơn đã được ghi nhận. Chỗ ngồi của bé đã được giữ chính thức trên hệ thống." 
            : "Quá trình thanh toán bị hủy hoặc xảy ra lỗi. Hóa đơn được lưu trong mục Chờ thanh toán."}
       </Text>
       
       <TouchableOpacity 
          className="bg-indigo-600 px-12 py-4 rounded-2xl shadow-lg shadow-indigo-300"
          onPress={() => router.back()} // Đổi lệnh navigate('/') thành back() để lùi về trang Chi tiết
       >
          <Text className="text-white font-black text-[15px]">QUAY LẠI KHÓA HỌC</Text>
       </TouchableOpacity>
    </View>
  );
}