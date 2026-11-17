import '../global.css';
import { Stack, usePathname } from 'expo-router';
import * as ScreenOrientation from 'expo-screen-orientation';
import { useEffect } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { Platform } from 'react-native';

import { useAudioHydration } from '@/hooks/useAudio';
import { useAuthHydration } from '@/hooks/useSession';
import { AppQueryClientProvider } from '@/lib/queryClientProvider';

export default function RootLayout() {
  useAuthHydration();
  useAudioHydration();
  const pathname = usePathname();

  useEffect(() => {
    if (Platform.OS === 'web' || pathname === '/students/quiz' || pathname === '/students/assignment') {
      return;
    }

    void ScreenOrientation.lockAsync(ScreenOrientation.OrientationLock.PORTRAIT_UP).catch((error: unknown) => {
      console.error('Could not lock the app to portrait orientation.', error);
    });
  }, [pathname]);

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
