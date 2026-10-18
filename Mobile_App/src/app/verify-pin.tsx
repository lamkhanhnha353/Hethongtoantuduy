import { useState } from 'react';
import { router } from 'expo-router';
import { ActivityIndicator, Alert, Animated, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather, MaterialCommunityIcons } from '@expo/vector-icons';

import { AlertBanner } from '@/components/AlertBanner';
import { Avatar } from '@/components/Avatar';
import { PrimaryButton } from '@/components/PrimaryButton';
import { useProfileQuery } from '@/hooks/useProfile';
import { useVerifyPinMutation } from '@/hooks/usePin';
import { toApiError } from '@/services/apiError';

const PIN_MIN = 4;
const PIN_MAX = 6;

export default function ScreenVerifyPin() {
  const {
    data: profile,
    isPending,
    isError,
    error,
    refetch,
  } = useProfileQuery();
  const verifyMutation = useVerifyPinMutation();

  const [pin, setPin] = useState('');
  const [wrongPin, setWrongPin] = useState(false);
  const [apiNotice, setApiNotice] = useState<string | null>(null);
  const [shakeAnim] = useState(() => new Animated.Value(0));

  const profileName = profile?.fullName.trim() || 'Phụ huynh';
  const hasPin = profile?.hasPin ?? false;
  const loadError = isError ? toApiError(error) : null;

  const triggerShake = () => {
    shakeAnim.setValue(0);
    Animated.sequence([
      Animated.timing(shakeAnim, { toValue: 10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: -10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: 10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: 0, duration: 50, useNativeDriver: true }),
    ]).start();
  };

  function submit(value: string) {
    setApiNotice(null);
    verifyMutation.mutate(value, {
      onSuccess: (result) => {
        if (result.valid) {
          router.replace('/(tabs)/home');
          return;
        }
        setWrongPin(true);
        triggerShake();
        setPin('');
      },
      onError: (mutationError) => {
        const apiError = toApiError(mutationError);
        setApiNotice(apiError?.message ?? 'Không xác thực được PIN. Vui lòng thử lại.');
        setPin('');
      },
    });
  }

  function handlePress(num: string) {
    if (wrongPin) setWrongPin(false);
    if (apiNotice) setApiNotice(null);
    if (verifyMutation.isPending || pin.length >= PIN_MAX) return;

    const nextPin = pin + num;
    setPin(nextPin);

    if (nextPin.length === PIN_MAX) {
      setTimeout(() => submit(nextPin), 50);
    }
  }

  function handleDelete() {
    if (wrongPin) setWrongPin(false);
    setPin((prev) => prev.slice(0, -1));
  }

  const renderKey = (num: string) => (
    <TouchableOpacity
      key={num}
      activeOpacity={0.5}
      disabled={verifyMutation.isPending}
      onPress={() => handlePress(num)}
      className="w-[80px] h-16 rounded-2xl bg-white border border-slate-200 shadow-sm flex items-center justify-center"
    >
      <Text className="text-2xl font-semibold text-slate-800">{num}</Text>
    </TouchableOpacity>
  );

  const header = (
    <View className="bg-indigo-600 pt-14 pb-8 relative overflow-hidden">
      <View className="absolute -top-12 -right-12 w-40 h-40 rounded-full bg-cyan-400/20 opacity-50" />
      <View className="absolute bottom-0 left-4 w-24 h-24 rounded-full bg-indigo-300/20 opacity-50" />

      <View className="px-5 flex-row items-center gap-3 relative z-10">
        <TouchableOpacity
          activeOpacity={0.5}
          onPress={() => router.back()}
          className="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center border border-white/30"
        >
          <Feather name="chevron-left" size={24} color="white" />
        </TouchableOpacity>
        <View>
          <Text className="text-sm text-indigo-100">Xác thực bảo mật</Text>
          <Text className="text-lg font-bold text-white leading-tight">Nhập mã PIN</Text>
        </View>
      </View>
    </View>
  );

  if (isPending) {
    return (
      <SafeAreaView className="flex-1 bg-[#f8fafc]" edges={['bottom']}>
        {header}
        <View className="items-center pt-16">
          <ActivityIndicator size="large" color="#4f46e5" />
          <Text className="mt-3 text-sm text-slate-500">Đang tải thông tin hồ sơ...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (loadError || !profile) {
    return (
      <SafeAreaView className="flex-1 bg-[#f8fafc]" edges={['bottom']}>
        {header}
        <View className="px-6 pt-8 gap-3">
          <AlertBanner tone="error" message={loadError?.message ?? 'Không tải được hồ sơ.'} />
          <PrimaryButton
            label="Thử lại"
            onPress={() => {
              void refetch();
            }}
            variant="ghost"
          />
        </View>
      </SafeAreaView>
    );
  }

  if (!hasPin) {
    return (
      <SafeAreaView className="flex-1 bg-[#f8fafc]" edges={['bottom']}>
        {header}
        <View className="flex-1 items-center px-6 pt-8">
          <View className="w-20 h-20 rounded-full bg-amber-100 items-center justify-center mb-4">
            <Feather name="lock" size={32} color="#f59e0b" />
          </View>
          <Text className="text-lg font-bold text-slate-900">Chưa thiết lập PIN</Text>
          <Text className="text-sm text-slate-500 mt-2 text-center leading-5">
            Bạn cần đặt PIN để xác nhận các thao tác quan trọng như xóa học sinh.
          </Text>

          <View className="w-full mt-8 gap-3">
            <PrimaryButton
              label="Đặt PIN ngay"
              onPress={() => router.push('/profile-pin')}
            />
            <PrimaryButton
              label="Bỏ qua, vào không gian quản lý"
              onPress={() => router.replace('/(tabs)/home')}
              variant="ghost"
            />
          </View>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-[#f8fafc]" edges={['bottom']}>
      {header}

      <View className="flex-1 items-center px-6 pt-8 pb-5">
        <View className="items-center mb-8">
          <View className="relative mb-3">
            <Avatar name={profileName} uri={profile.avatar} size={80} />
            <View className="absolute -bottom-1 -right-1 w-8 h-8 rounded-full bg-indigo-600 border-[3px] border-white flex items-center justify-center">
              <Feather name="lock" size={14} color="white" />
            </View>
          </View>
          <Text className="font-bold text-slate-900 text-lg">{profileName}</Text>
          <Text className="text-sm text-slate-500 mt-0.5">Phụ huynh</Text>
        </View>

        <Text className="text-sm text-slate-600 font-medium mb-6">
          Nhập mã PIN để truy cập không gian quản lý
        </Text>

        {apiNotice ? (
          <View className="w-full mb-4">
            <AlertBanner tone="error" message={apiNotice} />
          </View>
        ) : null}

        <Animated.View
          style={{ transform: [{ translateX: shakeAnim }] }}
          className="flex-row items-center gap-5 mb-2"
        >
          {Array.from({ length: PIN_MAX }).map((_, index) => {
            const isFilled = index < pin.length;
            return (
              <View
                key={index}
                className={`w-[18px] h-[18px] rounded-full border-[2.5px] ${
                  wrongPin
                    ? 'border-rose-500 bg-rose-500'
                    : isFilled
                      ? 'bg-indigo-600 border-indigo-600 scale-110'
                      : 'border-slate-300 bg-transparent'
                }`}
              />
            );
          })}
        </Animated.View>

        <View className="h-6 mb-6 justify-center">
          {wrongPin ? (
            <Text className="text-sm text-rose-500 font-medium text-center">
              Sai mã PIN. Vui lòng thử lại
            </Text>
          ) : null}
          {verifyMutation.isPending ? (
            <Text className="text-sm text-indigo-600 font-medium text-center">
              Đang xác thực...
            </Text>
          ) : null}
        </View>

        <View className="w-[264px] flex-row flex-wrap justify-between gap-y-3">
          {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map(renderKey)}

          <TouchableOpacity
            activeOpacity={0.5}
            disabled={verifyMutation.isPending}
            onPress={() =>
              Alert.alert('Thông báo', 'Đăng nhập bằng khuôn mặt đang được phát triển')
            }
            className="w-[80px] h-16 rounded-2xl bg-indigo-50 border border-indigo-100 flex items-center justify-center shadow-sm"
          >
            <MaterialCommunityIcons name="face-recognition" size={28} color="#4f46e5" />
          </TouchableOpacity>

          {renderKey('0')}

          <TouchableOpacity
            activeOpacity={0.5}
            disabled={verifyMutation.isPending || pin.length === 0}
            onPress={handleDelete}
            className="w-[80px] h-16 rounded-2xl bg-white border border-slate-200 flex items-center justify-center shadow-sm"
          >
            <Feather name="delete" size={24} color="#64748b" />
          </TouchableOpacity>
        </View>

        {pin.length >= PIN_MIN && pin.length < PIN_MAX ? (
          <View className="w-[264px] mt-5">
            <PrimaryButton
              label="Xác nhận"
              loading={verifyMutation.isPending}
              onPress={() => submit(pin)}
            />
          </View>
        ) : (
          <Text className="mt-6 text-xs text-slate-400 text-center">
            PIN gồm từ {PIN_MIN} đến {PIN_MAX} chữ số
          </Text>
        )}
      </View>
    </SafeAreaView>
  );
}
