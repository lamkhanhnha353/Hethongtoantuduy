import { Feather } from '@expo/vector-icons';
import { Image } from 'expo-image';
import { File, Paths } from 'expo-file-system';
import * as MediaLibrary from 'expo-media-library/legacy';
import { useState } from 'react';
import {
  Alert,
  Modal,
  Pressable,
  Text,
  TouchableOpacity,
  useWindowDimensions,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

export function ChatImageViewer({
  imageUrl,
  onClose,
}: {
  imageUrl: string | null;
  onClose: () => void;
}) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const [savingImage, setSavingImage] = useState(false);
  const [imageSize, setImageSize] = useState<{ url: string; width: number; height: number } | null>(
    null,
  );
  const { width: screenWidth, height: screenHeight } = useWindowDimensions();
  const maxWidth = Math.min(screenWidth - 32, 720);
  const maxHeight = screenHeight * 0.82;
  const loadedSize = imageSize?.url === imageUrl ? imageSize : null;
  const imageScale = loadedSize
    ? Math.min(maxWidth / loadedSize.width, maxHeight / loadedSize.height)
    : 1;
  const displayWidth = loadedSize ? loadedSize.width * imageScale : maxWidth;
  const displayHeight = loadedSize ? loadedSize.height * imageScale : maxHeight;

  function close() {
    setFailedUrl(null);
    setImageSize(null);
    onClose();
  }

  async function saveImage() {
    if (!imageUrl || savingImage) return;

    setSavingImage(true);
    try {
      const permission = await MediaLibrary.requestPermissionsAsync(true, ['photo']);
      if (!permission.granted) {
        Alert.alert('Cần quyền truy cập', 'Cho phép ứng dụng lưu ảnh vào thư viện thiết bị.');
        return;
      }

      const extension = imageUrl.match(/\.(png|jpe?g|webp|gif)(?:$|[?#])/i)?.[1] ?? 'jpg';
      const filename = `chat-image-${Date.now()}.${extension}`;
      const downloadedFile = await File.downloadFileAsync(
        imageUrl,
        new File(Paths.cache, filename),
      );
      await MediaLibrary.createAssetAsync(downloadedFile.uri);
      Alert.alert('Đã lưu ảnh', 'Ảnh đã được lưu vào thư viện thiết bị.');
    } catch (error) {
      Alert.alert(
        'Không thể lưu ảnh',
        error instanceof Error ? error.message : 'Vui lòng thử lại sau.',
      );
    } finally {
      setSavingImage(false);
    }
  }

  return (
    <Modal
      animationType="fade"
      onRequestClose={close}
      presentationStyle="overFullScreen"
      statusBarTranslucent
      transparent
      visible={imageUrl !== null}
    >
      <Pressable
        onPress={close}
        className="flex-1 items-center justify-center bg-black/95"
        accessibilityLabel="Đóng ảnh"
      >
        <SafeAreaView className="absolute inset-0" pointerEvents="box-none">
          <View className="flex-row justify-end gap-2 p-3">
            <TouchableOpacity
              onPress={(event) => {
                event.stopPropagation();
                void saveImage();
              }}
              disabled={savingImage}
              className="h-11 w-11 items-center justify-center rounded-full bg-white/15"
              accessibilityLabel="Tải ảnh về thiết bị"
            >
              {savingImage ? (
                <Feather name="loader" size={21} color="white" />
              ) : (
                <Feather name="download" size={21} color="white" />
              )}
            </TouchableOpacity>
            <TouchableOpacity
              onPress={(event) => {
                event.stopPropagation();
                close();
              }}
              className="h-11 w-11 items-center justify-center rounded-full bg-white/15"
              accessibilityLabel="Đóng ảnh"
            >
              <Feather name="x" size={25} color="white" />
            </TouchableOpacity>
          </View>
        </SafeAreaView>

        {imageUrl ? (
          <Pressable
            onPress={(event) => event.stopPropagation()}
            style={{ width: displayWidth, height: displayHeight }}
          >
            {failedUrl === imageUrl ? (
              <View className="items-center px-8">
                <Feather name="image" size={40} color="white" />
                <Text className="mt-3 text-center text-sm text-white">
                  Không tải được ảnh. Vui lòng kiểm tra kết nối mạng.
                </Text>
              </View>
            ) : (
              <Image
                source={{ uri: imageUrl }}
                style={{ width: displayWidth, height: displayHeight }}
                contentFit="fill"
                cachePolicy="disk"
                transition={150}
                onLoad={({ source }) => {
                  if (source.width > 0 && source.height > 0) {
                    setImageSize({
                      url: imageUrl,
                      width: source.width,
                      height: source.height,
                    });
                  }
                }}
                onError={() => setFailedUrl(imageUrl)}
              />
            )}
          </Pressable>
        ) : null}
      </Pressable>
    </Modal>
  );
}
