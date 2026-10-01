import { Platform } from 'react-native';

const DEFAULT_API_URL = 'http://localhost:5114';

function normalize(url: string) {
  return url.replace(/\/+$/, '');
}

export const API_URL = normalize(process.env.EXPO_PUBLIC_API_URL ?? DEFAULT_API_URL);

export const USE_MOCK_API = process.env.EXPO_PUBLIC_USE_MOCK_API !== 'false';

export const API_TIMEOUT_MS = 15000;

/**
 * `expo-secure-store` chỉ có native implementation (android/ios/tvos).
 * Trên web module được stub thành object rỗng nên các method đều không tồn tại,
 * vì vậy phải dùng `localStorage` làm nơi lưu phiên tạm thời ở web.
 */
export const SESSION_STORAGE_MODE = Platform.select({
  web: 'local',
  default: 'secure',
}) as 'local' | 'secure';

export const REQUEST_HEADERS = {
  'Content-Type': 'application/json',
  Accept: 'application/json',
};

/**
 * Android emulator không hiểu được `localhost` của máy host nên cần dùng IP của host.
 * Các nền tảng khác đều dùng được `localhost` trỏ về chính máy đang chạy app.
 */
export const PLATFORM_NOTE = Platform.select({
  android: 'Trên Android emulator hãy đặt EXPO_PUBLIC_API_URL=http://10.0.2.2:5114',
  default: null,
});
