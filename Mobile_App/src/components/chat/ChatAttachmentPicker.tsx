import { Feather } from '@expo/vector-icons';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import { useCallback, useEffect, useRef, useState, type ComponentProps } from 'react';
import {
  Alert,
  AppState,
  Modal,
  Platform,
  Pressable,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';

import type { ChatAttachment } from '@/services/chat.service';
import { toApiError } from '@/services/apiError';

type PickedAsset = {
  uri: string;
  name?: string | null;
  fileName?: string | null;
  mimeType?: string | null;
  file?: Blob | null;
};

const FILE_TYPES_BY_EXTENSION: Record<string, { mimeType: string; extension: string }> = {
  pdf: { mimeType: 'application/pdf', extension: 'pdf' },
  doc: { mimeType: 'application/msword', extension: 'doc' },
  docx: {
    mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    extension: 'docx',
  },
  xls: { mimeType: 'application/vnd.ms-excel', extension: 'xls' },
  xlsx: {
    mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    extension: 'xlsx',
  },
  ppt: { mimeType: 'application/vnd.ms-powerpoint', extension: 'ppt' },
  pptx: {
    mimeType: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    extension: 'pptx',
  },
  jpg: { mimeType: 'image/jpeg', extension: 'jpg' },
  jpeg: { mimeType: 'image/jpeg', extension: 'jpg' },
  png: { mimeType: 'image/png', extension: 'png' },
  webp: { mimeType: 'image/webp', extension: 'webp' },
  heic: { mimeType: 'image/heic', extension: 'heic' },
  heif: { mimeType: 'image/heif', extension: 'heif' },
};

const FILE_TYPES_BY_MIME_TYPE = Object.fromEntries(
  Object.values(FILE_TYPES_BY_EXTENSION).map((fileType) => [fileType.mimeType, fileType]),
);

function toChatAttachment(
  asset: PickedAsset,
  fallbackMimeType = 'application/octet-stream',
): ChatAttachment {
  const uriName = asset.uri.split(/[?#]/, 1)[0].split('/').pop();
  const originalName = asset.name ?? asset.fileName ?? uriName ?? 'attachment';
  const originalExtension = originalName.split('.').pop()?.toLowerCase() ?? '';
  const uriExtension = uriName?.split('.').pop()?.toLowerCase() ?? '';
  const fileType =
    FILE_TYPES_BY_MIME_TYPE[asset.mimeType ?? ''] ??
    FILE_TYPES_BY_EXTENSION[originalExtension] ??
    FILE_TYPES_BY_EXTENSION[uriExtension] ??
    FILE_TYPES_BY_MIME_TYPE[fallbackMimeType];
  const mimeType = fileType?.mimeType ?? asset.mimeType ?? fallbackMimeType;
  const extension = fileType?.extension;
  const name =
    extension && !originalName.toLowerCase().endsWith(`.${extension}`)
      ? `${originalName.replace(/\.[^.]*$/, '')}.${extension}`
      : originalName;

  return { uri: asset.uri, name, type: mimeType, file: asset.file };
}

export function ChatAttachmentPicker({
  disabled,
  onSelect,
}: {
  disabled: boolean;
  onSelect: (attachment: ChatAttachment) => Promise<void>;
}) {
  const handledUrisRef = useRef(new Set<string>());
  const recoveringRef = useRef(false);
  const pickerInFlightRef = useRef(false);
  const [sheetVisible, setSheetVisible] = useState(false);

  const sendPickedAsset = useCallback(async (asset: PickedAsset) => {
    if (handledUrisRef.current.has(asset.uri)) return;
    handledUrisRef.current.add(asset.uri);
    await onSelect(toChatAttachment(asset, 'image/jpeg'));
  }, [onSelect]);

  const recoverImagePickerResult = useCallback(async () => {
    if (Platform.OS !== 'android' || recoveringRef.current || pickerInFlightRef.current) {
      return false;
    }

    recoveringRef.current = true;
    let result:
      | ImagePicker.ImagePickerResult
      | ImagePicker.ImagePickerErrorResult
      | null;
    try {
      result = await ImagePicker.getPendingResultAsync();
    } catch (error) {
      Alert.alert('Không thể khôi phục ảnh', toApiError(error).message);
      return true;
    } finally {
      recoveringRef.current = false;
    }

    if (!result) return false;
    if ('code' in result) {
      Alert.alert('Không thể khôi phục ảnh', result.message);
      return true;
    }
    if (result.canceled || !result.assets[0]) return false;

    try {
      await sendPickedAsset(result.assets[0]);
    } catch (error) {
      Alert.alert('Không gửi được ảnh', toApiError(error).message);
    }
    return true;
  }, [sendPickedAsset]);

  useEffect(() => {
    void recoverImagePickerResult();

    const subscription = AppState.addEventListener('change', (state) => {
      if (state === 'active') {
        void recoverImagePickerResult();
      }
    });

    return () => subscription.remove();
  }, [recoverImagePickerResult]);

  const chooseFromLibrary = useCallback(async () => {
    let permission: ImagePicker.MediaLibraryPermissionResponse;
    try {
      permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    } catch (error) {
      Alert.alert('Không thể truy cập thư viện ảnh', toApiError(error).message);
      return;
    }
    if (!permission.granted) {
      Alert.alert('Cần quyền truy cập ảnh', 'Hãy cấp quyền thư viện ảnh để gửi hình.');
      return;
    }

    handledUrisRef.current.clear();
    pickerInFlightRef.current = true;
    let result: ImagePicker.ImagePickerResult;
    try {
      result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images'],
        quality: 0.85,
        ...(Platform.OS === 'ios'
          ? { preferredAssetRepresentationMode: ImagePicker.UIImagePickerPreferredAssetRepresentationMode.Compatible }
          : {}),
      });
    } catch (error) {
      pickerInFlightRef.current = false;
      const recovered = await recoverImagePickerResult();
      if (!recovered) {
        Alert.alert('Không mở được thư viện ảnh', toApiError(error).message);
      }
      return;
    }

    try {
      if (!result.canceled && result.assets[0]) {
        await sendPickedAsset(result.assets[0]);
      }
    } catch (error) {
      Alert.alert('Không gửi được ảnh', toApiError(error).message);
    } finally {
      pickerInFlightRef.current = false;
    }
  }, [recoverImagePickerResult, sendPickedAsset]);

  const takePhoto = useCallback(async () => {
    let permission: ImagePicker.CameraPermissionResponse;
    try {
      permission = await ImagePicker.requestCameraPermissionsAsync();
    } catch (error) {
      Alert.alert('Không thể truy cập máy ảnh', toApiError(error).message);
      return;
    }
    if (!permission.granted) {
      Alert.alert('Cần quyền sử dụng máy ảnh', 'Hãy cấp quyền máy ảnh để chụp và gửi hình.');
      return;
    }

    handledUrisRef.current.clear();
    pickerInFlightRef.current = true;
    let result: ImagePicker.ImagePickerResult;
    try {
      result = await ImagePicker.launchCameraAsync({
        mediaTypes: ['images'],
        quality: 0.7,
        allowsEditing: true,
        ...(Platform.OS === 'ios'
          ? {
              preferredAssetRepresentationMode:
                ImagePicker.UIImagePickerPreferredAssetRepresentationMode.Compatible,
            }
          : {}),
      });
    } catch (error) {
      pickerInFlightRef.current = false;
      const recovered = await recoverImagePickerResult();
      if (!recovered) {
        Alert.alert('Không mở được máy ảnh', toApiError(error).message);
      }
      return;
    }

    try {
      if (!result.canceled && result.assets[0]) {
        await sendPickedAsset(result.assets[0]);
      }
    } catch (error) {
      Alert.alert('Không gửi được ảnh', toApiError(error).message);
    } finally {
      pickerInFlightRef.current = false;
    }
  }, [recoverImagePickerResult, sendPickedAsset]);

  const chooseDocument = useCallback(async () => {
    try {
      handledUrisRef.current.clear();
      const result = await DocumentPicker.getDocumentAsync({
        type: [
          'application/pdf',
          'application/msword',
          'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
          'application/vnd.ms-excel',
          'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          'application/vnd.ms-powerpoint',
          'application/vnd.openxmlformats-officedocument.presentationml.presentation',
        ],
        copyToCacheDirectory: true,
        multiple: false,
      });

      if (!result.canceled) {
        await onSelect(toChatAttachment(result.assets[0]));
      }
    } catch (error) {
      Alert.alert('Không mở được tài liệu', toApiError(error).message);
    }
  }, [onSelect]);

  function closeThen(action: () => void) {
    setSheetVisible(false);
    setTimeout(action, 250);
  }

  return (
    <>
      <TouchableOpacity
        onPress={() => setSheetVisible(true)}
        disabled={disabled}
        className="ml-1 h-11 w-11 items-center justify-center rounded-full"
        accessibilityLabel="Gửi ảnh hoặc tài liệu"
      >
        <Feather name="paperclip" size={23} color={disabled ? '#94a3b8' : '#475569'} />
      </TouchableOpacity>

      <Modal
        animationType="fade"
        onRequestClose={() => setSheetVisible(false)}
        transparent
        visible={sheetVisible}
      >
        <Pressable
          onPress={() => setSheetVisible(false)}
          className="flex-1 justify-end bg-slate-950/40"
        >
          <Pressable
            onPress={(event) => event.stopPropagation()}
            className="rounded-t-3xl bg-white px-5 pb-8 pt-5"
          >
            <View className="mb-5 flex-row items-center justify-between">
              <View>
                <Text className="text-lg font-bold text-slate-900">Tệp đính kèm</Text>
                <Text className="mt-1 text-sm text-slate-500">Chọn nội dung muốn gửi</Text>
              </View>
              <TouchableOpacity
                onPress={() => setSheetVisible(false)}
                className="h-10 w-10 items-center justify-center rounded-full bg-slate-100"
                accessibilityLabel="Đóng tệp đính kèm"
              >
                <Feather name="x" size={22} color="#475569" />
              </TouchableOpacity>
            </View>

            <View className="flex-row justify-around">
              <AttachmentAction
                icon="camera"
                label="Chụp ảnh"
                onPress={() => closeThen(() => void takePhoto())}
              />
              <AttachmentAction
                icon="image"
                label="Thư viện"
                onPress={() => closeThen(() => void chooseFromLibrary())}
              />
              <AttachmentAction
                icon="file-text"
                label="Tài liệu"
                onPress={() => closeThen(() => void chooseDocument())}
              />
            </View>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}

function AttachmentAction({
  icon,
  label,
  onPress,
}: {
  icon: ComponentProps<typeof Feather>['name'];
  label: string;
  onPress: () => void;
}) {
  return (
    <TouchableOpacity onPress={onPress} className="min-w-[88px] items-center py-2">
      <View className="mb-2 h-14 w-14 items-center justify-center rounded-2xl bg-indigo-50">
        <Feather name={icon} size={24} color="#4f46e5" />
      </View>
      <Text className="text-xs font-medium text-slate-700">{label}</Text>
    </TouchableOpacity>
  );
}
