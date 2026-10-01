import { Link, useRouter } from 'expo-router';
import { ActivityIndicator, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { PrimaryButton } from '@/components/PrimaryButton';
import { useSessionQuery } from '@/hooks/useSession';

export default function ScreenHome() {
  const router = useRouter();
  const { data: session, isPending: checking } = useSessionQuery();

  if (checking) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-white" edges={['top', 'bottom']}>
        <ActivityIndicator size="large" color="#0284c7" />
        <Text className="mt-3 text-sm text-slate-500">Đang kiểm tra phiên đăng nhập...</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-white" edges={['top', 'bottom']}>
      <View className="flex-1 items-center justify-center px-6">
        <Text className="text-center text-2xl font-bold text-slate-900">
          Hệ thống Toán Tư Duy
        </Text>

        {session ? (
          <View className="mt-6 w-full max-w-sm rounded-2xl border border-slate-200 bg-slate-50 p-5">
            <Text className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Đã đăng nhập
            </Text>
            {session.fullName ? (
              <Text className="mt-2 text-lg font-semibold text-slate-900">{session.fullName}</Text>
            ) : null}
            <Text className="mt-1 text-sm text-slate-600">
              Số điện thoại: {session.phone}
            </Text>
            <Text className="mt-1 text-sm text-slate-600">
              Vai trò: {session.role === 'Parent' ? 'Phụ huynh' : 'Giáo viên'}
            </Text>
            <Text className="mt-1 text-sm text-slate-600">
              Hồ sơ: #{session.profileId}
            </Text>
          </View>
        ) : (
          <Text className="mt-3 text-center text-base text-slate-600">
            Chưa đăng nhập. Hãy đăng nhập hoặc đăng ký tài khoản để bắt đầu.
          </Text>
        )}

        <View className="mt-8 w-full max-w-sm gap-3">
          <PrimaryButton
            label="Đăng nhập"
            onPress={() => router.push('/login')}
            variant={session ? 'ghost' : 'primary'}
          />
          <PrimaryButton
            label="Đăng ký tài khoản"
            onPress={() => router.push('/register')}
            variant="ghost"
          />
        </View>

        {!session ? (
          <Text className="mt-8 text-center text-xs text-slate-400">
            Xem thông tin khác tại{' '}
            <Link className="text-sky-600" href="/login">
              trang đăng nhập
            </Link>
          </Text>
        ) : null}
      </View>
    </SafeAreaView>
  );
}
