import { View, Text, TouchableOpacity, ScrollView } from 'react-native';
import { Feather } from '@expo/vector-icons';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

export function WelcomeHeader() {
  const insets = useSafeAreaInsets();

  return (
    <View 
      className="bg-indigo-600 relative overflow-hidden" 
      style={{ paddingTop: insets.top + 10, paddingBottom: 15 }}
    >
  
      <View className="absolute -top-16 -right-10 w-48 h-48 rounded-full bg-cyan-300/20 opacity-50" />
      <View className="absolute bottom-0 left-0 w-32 h-32 rounded-full bg-indigo-300/20 opacity-50" />

      {/* Thông tin Phụ huynh & Chuông thông báo */}
      <View className="px-5 flex-row items-center justify-between z-10">
        <View className="flex-row items-center gap-3">
          <View className="w-12 h-12 rounded-full border-2 border-white/80 bg-white/20 items-center justify-center shadow-sm">
            <Text className="text-white font-bold text-lg">MA</Text>
          </View>
          <View>
            <Text className="text-xs text-indigo-100 font-medium">Xin chào Phụ huynh,</Text>
            <Text className="text-lg font-bold text-white tracking-tight">Nguyễn Minh Anh</Text>
          </View>
        </View>

        <TouchableOpacity 
          activeOpacity={0.7}
          className="relative w-10 h-10 rounded-full bg-white/20 items-center justify-center border border-white/30"
        >
          <Feather name="bell" size={20} color="white" />
          <View className="absolute -top-1 -right-1 w-4 h-4 bg-rose-500 rounded-full items-center justify-center border border-indigo-600">
            <Text className="text-white text-[9px] font-bold">3</Text>
          </View>
        </TouchableOpacity>
      </View>

      {/* Thanh trượt chọn con*/}
      <ScrollView 
        horizontal 
        showsHorizontalScrollIndicator={false} 
        className="mt-5 z-10" 
        contentContainerStyle={{ paddingHorizontal: 20, gap: 10 }}
      >
        <TouchableOpacity activeOpacity={0.7} className="flex-row items-center gap-2 bg-white px-3.5 py-1.5 rounded-full shadow-sm">
          <View className="w-5 h-5 rounded-full bg-indigo-600 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">All</Text>
          </View>
          <Text className="text-indigo-900 text-xs font-bold">Tất cả con</Text>
        </TouchableOpacity>

        <TouchableOpacity activeOpacity={0.7} className="flex-row items-center gap-2 bg-white/20 px-3.5 py-1.5 rounded-full border border-white/30">
          <View className="w-5 h-5 rounded-full bg-white/30 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">An</Text>
          </View>
          <Text className="text-white text-xs font-semibold">Minh An</Text>
        </TouchableOpacity>

        <TouchableOpacity activeOpacity={0.7} className="flex-row items-center gap-2 bg-white/20 px-3.5 py-1.5 rounded-full border border-white/30">
          <View className="w-5 h-5 rounded-full bg-white/30 items-center justify-center">
            <Text className="text-white text-[10px] font-bold">Bảo</Text>
          </View>
          <Text className="text-white text-xs font-semibold">Gia Bảo</Text>
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}