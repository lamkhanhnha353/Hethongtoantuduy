import '../global.css';
import { Stack } from 'expo-router';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { useAudioHydration } from '@/hooks/useAudio';
import { useAuthHydration } from '@/hooks/useSession';
import { AppQueryClientProvider } from '@/lib/queryClientProvider';

export default function RootLayout() {
  useAuthHydration();
  useAudioHydration();

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <AppQueryClientProvider>
          <Stack screenOptions={{ headerShown: false }} />
        </AppQueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
