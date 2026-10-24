import { View, Text, TouchableOpacity } from 'react-native';
import { Feather } from '@expo/vector-icons';

export interface CourseType {
  id: number;
  code: string;
  name: string;
  minAge: number;
  maxAge: number;
  duration: string;
  price: number;
  color: string;
  type: string;
  status: 'open' | 'soon';
}

interface CourseCardProps {
  course: CourseType;
  // BẮT BUỘC: Nhận lệnh chuyển trang từ component cha
  onPress: () => void; 
}

export function CourseCard({ course, onPress }: CourseCardProps) {
  const isOpen = course.status === 'open';

  return (
    <TouchableOpacity 
      activeOpacity={0.8} 
      onPress={onPress} // Dùng lệnh truyền từ ngoài
      className="bg-white rounded-[24px] p-3 mb-4 flex-row items-center border border-slate-100"
      style={{
        shadowColor: "#6366f1",
        shadowOffset: { width: 0, height: 6 },
        shadowOpacity: 0.06,
        shadowRadius: 12,
        elevation: 3
      }}
    >
      <View className={`w-[100px] h-[110px] rounded-2xl items-center justify-center ${course.color} relative overflow-hidden`}>
        <View className="absolute top-0 right-0 w-16 h-16 bg-white/20 rounded-full -translate-y-6 translate-x-6" />
        <Text className="text-white text-3xl font-black tracking-tighter">{course.code}</Text>
      </View>

      <View className="flex-1 pl-4 pr-2 py-1">
        <View className="flex-row items-center justify-between mb-1.5">
          {isOpen ? (
            <View className="flex-row items-center gap-1.5 bg-emerald-50 px-2.5 py-1 rounded-md border border-emerald-100">
              <View className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
              <Text className="text-[9px] font-bold text-emerald-700 uppercase tracking-wider">Chiêu sinh</Text>
            </View>
          ) : (
            <View className="flex-row items-center gap-1.5 bg-slate-100 px-2.5 py-1 rounded-md border border-slate-200">
              <View className="w-1.5 h-1.5 rounded-full bg-slate-400" />
              <Text className="text-[9px] font-bold text-slate-500 uppercase tracking-wider">Sắp mở</Text>
            </View>
          )}
          <Feather name="bookmark" size={16} color="#cbd5e1" />
        </View>

        <Text className="font-extrabold text-slate-900 text-[17px] leading-snug mb-1" numberOfLines={2}>
          {course.name}
        </Text>

        <Text className="text-[12px] font-medium text-slate-500 mb-3">
          {course.minAge}–{course.maxAge} tuổi • {course.duration}
        </Text>
        
        <View className="flex-row items-center justify-between mt-auto">
          <Text className="text-[18px] font-black text-indigo-600 tracking-tight">
            {course.price.toLocaleString('vi-VN')}đ
          </Text>
          <View className="bg-indigo-600 px-4 py-2 rounded-xl shadow-sm shadow-indigo-200">
            <Text className="text-[12px] font-bold text-white">Đăng ký</Text>
          </View>
        </View>
      </View>
    </TouchableOpacity>
  );
}