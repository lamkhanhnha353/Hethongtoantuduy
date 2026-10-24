import { View, Text, TouchableOpacity } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

export function WelcomeHeader() {
  const router = useRouter();
  const insets = useSafeAreaInsets(); 

  return (
    <View 
      // Giữ lại nền màu cũ 
      className="bg-indigo-500 pb-6 px-4 rounded-b-3xl"
      style={{ paddingTop: insets.top + 16 }} 
    >
      {/* Hàng ngang duy nhất chứa: Nút Back + Avatar + Info + Chuông */}
      <View className="flex-row items-center justify-between">
        
        {/* Cụm bên trái: Back + Avatar + Text */}
        <View className="flex-row items-center gap-3">
          {/* Nút Trở về */}
          <TouchableOpacity 
            activeOpacity={0.7} 
            className="w-10 h-10 items-center justify-center rounded-full bg-white/20"
            onPress={() => router.replace('/select-profile')}
          >
            <Feather name="chevron-left" size={24} color="white" />
          </TouchableOpacity>

          {/* Avatar màu xanh */}
          <View className="w-12 h-12 bg-teal-400 rounded-full items-center justify-center border-[1.5px] border-white">
            <Text className="text-white font-bold text-lg">TH</Text>
          </View>

          {/* Thông tin phụ huynh ( */}
          <View>
            <Text className="text-indigo-100 text-[11px] mb-0.5">Xin chào Phụ huynh,</Text>
            <Text className="text-white font-bold text-[15px]">Trần Văn Phụ Huynh</Text>
          </View>
        </View>

        {/* Nút Chuông có Badge đỏ bên phải */}
        <TouchableOpacity 
          activeOpacity={0.7} 
          className="w-10 h-10 items-center justify-center rounded-full bg-white/20 relative"
        >
          <Feather name="bell" size={18} color="white" />
          {/* Chấm đỏ thông báo  */}
          <View className="absolute -top-1 -right-1 bg-rose-500 w-[18px] h-[18px] rounded-full items-center justify-center border-2 border-indigo-500">
            <Text className="text-white text-[9px] font-bold">3</Text>
          </View>
        </TouchableOpacity>
      </View>

      {/* Bộ lọc hồ sơ con */}
      <View className="flex-row mt-6 gap-2">
        <TouchableOpacity className="bg-white px-3 py-1.5 rounded-full flex-row items-center gap-1.5">
          <View className="w-5 h-5 rounded-full bg-indigo-600 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">All</Text>
          </View>
          <Text className="text-indigo-600 text-xs font-bold">Tất cả con</Text>
        </TouchableOpacity>

        <TouchableOpacity className="bg-indigo-400/50 px-3 py-1.5 rounded-full border border-indigo-300 flex-row items-center gap-1.5">
          <View className="w-5 h-5 rounded-full bg-teal-500 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">NA</Text>
          </View>
          <Text className="text-white text-xs font-semibold">Văn A</Text>
        </TouchableOpacity>

        <TouchableOpacity className="bg-indigo-400/50 px-3 py-1.5 rounded-full border border-indigo-300 flex-row items-center gap-1.5">
          <View className="w-5 h-5 rounded-full bg-teal-500 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">TT</Text>
          </View>
          <Text className="text-white text-xs font-semibold">Bé Học Toán</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}