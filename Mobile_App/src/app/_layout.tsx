import '../global.css';
import { Stack } from 'expo-router';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { AppQueryClientProvider } from '@/lib/queryClientProvider';

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <AppQueryClientProvider>
        <Stack screenOptions={{ headerShown: false }}>
          <Stack.Screen name="index" />
          <Stack.Screen name="login" />
          <Stack.Screen name="register" />
        </Stack>
      </AppQueryClientProvider>
    </SafeAreaProvider>
  );
}
