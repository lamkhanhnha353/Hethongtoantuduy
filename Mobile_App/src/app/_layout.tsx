import '../global.css';
import { Stack } from 'expo-router';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { useAuthHydration } from '@/hooks/useSession';
import { AppQueryClientProvider } from '@/lib/queryClientProvider';

export default function RootLayout() {
  useAuthHydration();

  return (
    <SafeAreaProvider>
      <AppQueryClientProvider>
        <Stack screenOptions={{ headerShown: false }}>
          <Stack.Screen name="index" />
          <Stack.Screen name="login" />
          <Stack.Screen name="register" />
          <Stack.Screen name="forgot-password" />
          <Stack.Screen name="home" />
        </Stack>
      </AppQueryClientProvider>
    </SafeAreaProvider>
  );
}
