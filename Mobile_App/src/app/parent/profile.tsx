import { View, Text, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { router } from 'expo-router';
import { Feather } from '@expo/vector-icons';
import { useClearSessionMutation } from '@/hooks/useSession';

export default function ProfileScreen() {
  const logout = useClearSessionMutation();

  const handleLogout = () => {
    logout();
    router.replace('/login');
  };

  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-[#f1f5f9] px-6">
      <View className="items-center mb-10">
        <View className="w-20 h-20 rounded-full bg-indigo-200 items-center justify-center mb-4 border-4 border-white shadow-sm">
          <Feather name="user" size={32} color="#4f46e5" />
        </View>
        <Text className="text-xl font-bold text-slate-800">Tài khoản của tôi</Text>
        <Text className="text-sm text-slate-500 mt-1">Giao diện quản lý đang phát triển</Text>
      </View>

      {/* Nút Đăng xuất tạm thời */}
      <TouchableOpacity 
        activeOpacity={0.8}
        onPress={handleLogout}
        className="w-full flex-row items-center justify-center gap-2 bg-rose-500 py-3.5 rounded-2xl shadow-sm"
      >
        <Feather name="log-out" size={18} color="white" />
        <Text className="text-white font-semibold text-sm">Đăng xuất (Tạm thời)</Text>
      </TouchableOpacity>
    </SafeAreaView>
  );
}