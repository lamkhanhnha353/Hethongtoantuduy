import '../global.css';
import { Stack } from 'expo-router';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { useAudioHydration } from '@/hooks/useAudio';
import { useAuthHydration } from '@/hooks/useSession';
import { AppQueryClientProvider } from '@/lib/queryClientProvider';

export default function RootLayout() {
  useAuthHydration();
  useAudioHydration();

  return (
    <SafeAreaProvider>
      <AppQueryClientProvider>
        <Stack screenOptions={{ headerShown: false }} />
      </AppQueryClientProvider>
    </SafeAreaProvider>
  );
}
