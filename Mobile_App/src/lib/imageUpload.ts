/**
 * Hạ tầng dùng chung cho ảnh đại diện (avatar) của mọi hồ sơ: lọc/kiểm tra ảnh
 * theo đúng ràng buộc của backend (`ImageUploadRules`) và dựng `FormData` để
 * upload trên cả React Native lẫn web. Các service chỉ cần bind endpoint.
 */

export const AVATAR_MAX_BYTES = 5 * 1024 * 1024;
export const AVATAR_MIME_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];

/** Ảnh đã được lọc hợp lệ, sẵn sàng đưa vào `FormData` của request upload. */
export type PickedImage = {
  uri: string;
  name: string;
  type: string;
  size?: number | null;
  file?: Blob | null;
};

/** Dữ liệu tối thiểu nhận từ trình chọn ảnh (khớp `ImagePickerAsset`). */
export type PickedImageSource = {
  uri: string;
  fileName?: string | null;
  mimeType?: string | null;
  fileSize?: number | null;
  file?: Blob | null;
};

function readMimeType(source: PickedImageSource): string {
  if (source.mimeType) {
    return source.mimeType.toLowerCase();
  }

  const extension = source.uri.split('.').pop()?.toLowerCase();
  if (extension === 'png') return 'image/png';
  if (extension === 'webp') return 'image/webp';
  if (extension === 'jpg' || extension === 'jpeg') return 'image/jpeg';

  return 'image/jpeg';
}

function extensionOf(mimeType: string): string {
  if (mimeType === 'image/png') return 'png';
  if (mimeType === 'image/webp') return 'webp';
  return 'jpg';
}

/**
 * Kiểm tra ảnh theo đúng ràng buộc của backend (`ImageUploadRules`) trước khi
 * upload để báo lỗi ngay trên máy thay vì chờ server trả về 400.
 */
export function prepareAvatarUpload(
  source: PickedImageSource,
): { image: PickedImage } | { error: string } {
  const mimeType = readMimeType(source);

  if (!AVATAR_MIME_TYPES.includes(mimeType)) {
    return { error: 'Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.' };
  }

  if (source.fileSize != null && source.fileSize > AVATAR_MAX_BYTES) {
    return { error: 'Ảnh vượt quá giới hạn 5MB. Vui lòng chọn ảnh nhỏ hơn.' };
  }

  return {
    image: {
      uri: source.uri,
      name: `avatar.${extensionOf(mimeType)}`,
      type: mimeType,
      size: source.fileSize,
      file: source.file,
    },
  };
}

/**
 * Dựng `FormData` cho request upload avatar. Trên web dùng luôn `Blob` sẵn có;
 * trên React Native phải đưa `{ uri, name, type }` (TypeScript khai báo `FormData`
 * theo chuẩn DOM nên ép kiểu ở đây).
 */
export function toAvatarFormData(image: PickedImage): FormData {
  const form = new FormData();

  if (image.file) {
    form.append('file', image.file);
  } else {
    form.append('file', { uri: image.uri, name: image.name, type: image.type } as unknown as Blob);
  }

  return form;
}
