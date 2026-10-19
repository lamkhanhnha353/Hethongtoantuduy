import { useRef, useState } from 'react';
import { Link, router, useLocalSearchParams } from 'expo-router';
import { ActivityIndicator, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-aware-scroll-view';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather, FontAwesome5 } from '@expo/vector-icons'; // Sử dụng bộ Icon có sẵn của Expo
import Svg, { Path } from 'react-native-svg';

import { AlertBanner } from '@/components/AlertBanner';
import { SESSION_EXPIRED_MESSAGE } from '@/lib/sessionExpiry';
import { toApiError } from '@/services/apiError';
import { useLoginMutation } from '@/hooks/useAuthMutations';

type FieldErrors = Partial<Record<'phone' | 'password', string>>;

const SOCIAL_NOT_READY_MESSAGE =
  'Đăng nhập bằng Google hoặc Facebook đang được phát triển. Bạn vui lòng dùng số điện thoại.';

export default function ScreenLogin() {
  const { reason, resetNotice, resetTone } = useLocalSearchParams<{
    reason?: string;
    resetNotice?: string;
    resetTone?: string;
  }>();
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false); 
  const [remember, setRemember] = useState(false); 
  const [notice, setNotice] = useState<string | null>(null);
  const passwordRef = useRef<TextInput>(null);

  const loginMutation = useLoginMutation();

  const apiError = loginMutation.isError ? toApiError(loginMutation.error) : null;
  const isSessionExpired = reason === 'expired';

  const fieldErrors: FieldErrors = apiError
    ? {
        phone: apiError.fieldErrors.phone?.[0],
        password: apiError.fieldErrors.password?.[0],
      }
    : {};

  function handleSubmit() {
    setNotice(null);
    loginMutation.mutate(
      { phone: phone.trim(), password },
      { onSuccess: () => router.replace('/select-profile') },
    );
  }

  
  return (
    <SafeAreaView className="flex-1 bg-[#f8fafc]" edges={['top', 'bottom']}>
      <KeyboardAwareScrollView
        className="flex-1"
        contentContainerClassName="flex-grow pb-8"
        enableOnAndroid
        extraScrollHeight={24}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        {/* Phần Logo và Tiêu đề */}
        <View className="px-6 pt-10 pb-6 items-center relative">
          <View className="mb-4 relative">
            <View className="w-16 h-16 rounded-2xl bg-indigo-500 items-center justify-center shadow-lg shadow-indigo-300">
              <Text className="text-white text-2xl font-black tracking-tight">TT</Text>
            </View>
            <View className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-amber-400 items-center justify-center shadow-sm">
              <Text className="text-white text-xs font-bold">★</Text>
            </View>
          </View>
          <Text className="text-xl font-bold text-indigo-700 tracking-tight">Toán Tư Duy</Text>
          <Text className="text-sm text-slate-500 mt-1 text-center">Đăng nhập để theo dõi tiến độ học của con</Text>
        </View>

        {/* Khung Form */}
        <View className="px-5">
          <View className="bg-white rounded-3xl border border-slate-100 shadow-sm p-5 mb-6">
            <Text className="text-lg font-bold text-slate-800 mb-4">Chào mừng trở lại</Text>

            {/* Hiển thị lỗi/thông báo */}
            {isSessionExpired ? <AlertBanner tone="error" message={SESSION_EXPIRED_MESSAGE} /> : null}
            {resetNotice ? (
              <AlertBanner
                tone={resetTone === 'success' ? 'success' : 'info'}
                message={resetNotice}
              />
            ) : null}
            {apiError ? <AlertBanner tone="error" message={apiError.message} /> : null}
            {notice ? <AlertBanner tone="info" message={notice} /> : null}

            {/* Input: Số điện thoại */}
            <View className="mb-4 mt-2">
              <Text className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Số điện thoại</Text>
              <View className="relative justify-center">
                <View className="absolute left-3.5 z-10">
                  <Feather name="phone" size={18} color="#94a3b8" />
                </View>
                <TextInput
                  className={`w-full rounded-xl py-3.5 pl-11 pr-4 text-slate-800 text-sm bg-white border ${fieldErrors.phone ? 'border-red-500' : 'border-slate-200'} focus:border-indigo-500`}
                  placeholder="0901 234 567"
                  placeholderTextColor="#94a3b8"
                  keyboardType="phone-pad"
                  autoCapitalize="none"
                  autoComplete="tel"
                  returnKeyType="next"
                  value={phone}
                  onChangeText={setPhone}
                  onSubmitEditing={() => passwordRef.current?.focus()}
                />
              </View>
              {fieldErrors.phone && <Text className="text-red-500 text-xs mt-1">{fieldErrors.phone}</Text>}
            </View>

            {/* Input: Mật khẩu */}
            <View className="mb-4">
              <Text className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider mb-1.5">Mật khẩu</Text>
              <View className="relative justify-center">
                <View className="absolute left-3.5 z-10">
                  <Feather name="lock" size={18} color="#94a3b8" />
                </View>
                <TextInput
                  ref={passwordRef}
                  className={`w-full rounded-xl py-3.5 pl-11 pr-12 text-slate-800 text-sm bg-white border ${fieldErrors.password ? 'border-red-500' : 'border-slate-200'} focus:border-indigo-500`}
                  placeholder="••••••••"
                  placeholderTextColor="#94a3b8"
                  secureTextEntry={!showPassword}
                  autoComplete="password"
                  returnKeyType="go"
                  value={password}
                  onChangeText={setPassword}
                  onSubmitEditing={handleSubmit}
                />
                <TouchableOpacity
                  className="absolute right-3.5 z-10 p-1"
                  onPress={() => setShowPassword(!showPassword)}
                >
                  <Feather name={showPassword ? "eye" : "eye-off"} size={18} color="#94a3b8" />
                </TouchableOpacity>
              </View>
              {fieldErrors.password && <Text className="text-red-500 text-xs mt-1">{fieldErrors.password}</Text>}
            </View>

            {/* Checkbox Ghi nhớ & Quên mật khẩu */}
            <View className="flex-row items-center justify-between mb-5">
              <TouchableOpacity
                className="flex-row items-center gap-2"
                onPress={() => setRemember(!remember)}
                activeOpacity={0.7}
              >
                <View className={`w-4 h-4 rounded border items-center justify-center ${remember ? 'bg-indigo-500 border-indigo-500' : 'border-slate-300'}`}>
                  {remember && <Feather name="check" size={12} color="white" />}
                </View>
                <Text className="text-xs text-slate-500">Ghi nhớ đăng nhập</Text>
              </TouchableOpacity>

              <Link href="/forgot-password" asChild onPress={() => setNotice(null)}>
                <TouchableOpacity>
                  <Text className="text-xs font-semibold text-indigo-500">Quên mật khẩu?</Text>
                </TouchableOpacity>
              </Link>
            </View>

            {/* Nút Đăng nhập Chính */}
            <TouchableOpacity
              className="w-full py-3.5 rounded-xl bg-indigo-500 flex-row justify-center items-center"
              onPress={handleSubmit}
              disabled={loginMutation.isPending}
              activeOpacity={0.8}
            >
              {loginMutation.isPending ? (
                <ActivityIndicator color="white" size="small" />
              ) : (
                <Text className="text-white text-sm font-bold">Đăng nhập</Text>
              )}
            </TouchableOpacity>
          </View>

          {/* Dải phân cách hoặc tiếp tục với*/}
          <View className="flex-row items-center justify-center gap-3 my-2 px-2">
            <View className="flex-1 h-px bg-slate-200" />
            <Text className="text-[11px] text-slate-400 font-medium">hoặc tiếp tục với</Text>
            <View className="flex-1 h-px bg-slate-200" />
          </View>

        {/* Nút Đăng nhập Mạng xã hội */}
          <View className="flex-row justify-center items-center gap-5 my-5">
            <TouchableOpacity
              className="w-[52px] h-[52px] rounded-full border border-slate-200 bg-white items-center justify-center shadow-sm"
              onPress={() => setNotice(SOCIAL_NOT_READY_MESSAGE)}
              activeOpacity={0.7}
            >
              <Svg width="24" height="24" viewBox="0 0 24 24">
                <Path fill="#EA4335" d="M5.27 14.27l-.88 3.28-3.21.07A11.96 11.96 0 010 12c0-1.99.48-3.86 1.34-5.52l2.86.52 1.25 2.84A7.16 7.16 0 004.8 12c0 .79.14 1.55.4 2.27z"/>
                <Path fill="#34A853" d="M23.5 12.27c0-.82-.07-1.42-.23-2.04H12v3.86h6.55c-.13.9-.55 2.24-1.59 3.15l3.07 2.38c1.84-1.7 2.9-4.2 2.9-7.35z"/>
                <Path fill="#4A90E2" d="M12 24c3.24 0 5.95-1.07 7.93-2.91l-3.07-2.38c-.82.55-1.92.93-3.24.93a7.18 7.18 0 01-6.79-4.96l-3.24 2.5A12 12 0 0012 24z"/>
                <Path fill="#FBBC05" d="M12 4.75c2.12 0 3.55.92 4.37 1.68l3.19-3.11C17.94 1.19 15.24 0 12 0A12 12 0 001.25 6.48l3.4 2.64A7.18 7.18 0 0112 4.75z"/>
              </Svg>
            </TouchableOpacity>
            
            <TouchableOpacity
              className="w-[52px] h-[52px] rounded-full border border-slate-200 bg-white items-center justify-center shadow-sm"
              onPress={() => setNotice(SOCIAL_NOT_READY_MESSAGE)}
              activeOpacity={0.7}
            >
              <FontAwesome5 name="facebook" size={24} color="#1877F2" />
            </TouchableOpacity>
          </View>

          {/* Link Đăng ký & Điều khoản */}
          <Text className="text-center text-sm text-slate-500 mt-2">
            Chưa có tài khoản?{' '}
            <Link href="/register" asChild>
              <Text className="text-indigo-600 font-bold">Đăng ký ngay</Text>
            </Link>
          </Text>

          <Text className="text-center text-[10px] text-slate-400 mt-5 leading-relaxed">
            Bằng việc đăng nhập, bạn đồng ý với{'\n'}
            <Text className="text-slate-500">Điều khoản sử dụng</Text> & <Text className="text-slate-500">Chính sách bảo mật</Text>
          </Text>
        </View>
      </KeyboardAwareScrollView>
    </SafeAreaView>
  );
}