import { apiClient } from '@/services/apiClient';
import type {
  CreateStudentRequestDto,
  StudentResponseDto,
  UpdateStudentRequestDto,
} from '@/types/student';

export async function getStudents(): Promise<StudentResponseDto[]> {
  const { data } = await apiClient.get<StudentResponseDto[]>('/api/students');
  return data;
}

export async function getStudent(id: number): Promise<StudentResponseDto> {
  const { data } = await apiClient.get<StudentResponseDto>(`/api/students/${id}`);
  return data;
}

export async function createStudent(payload: CreateStudentRequestDto): Promise<StudentResponseDto> {
  const { data } = await apiClient.post<StudentResponseDto>('/api/students', payload);
  return data;
}

export async function updateStudent(
  id: number,
  payload: UpdateStudentRequestDto,
): Promise<StudentResponseDto> {
  const { data } = await apiClient.put<StudentResponseDto>(`/api/students/${id}`, payload);
  return data;
}

export async function deleteStudent(id: number, pin: string): Promise<void> {
  await apiClient.delete(`/api/students/${id}`, {
    headers: { 'X-Pin': pin },
  });
}

const AVATAR_MAX_BYTES = 5 * 1024 * 1024;
const AVATAR_MIME_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];

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

export async function uploadStudentAvatar(
  id: number,
  image: PickedImage,
): Promise<StudentResponseDto> {
  const form = new FormData();

  if (image.file) {
    form.append('file', image.file);
  } else {
    // React Native nhận `{ uri, name, type }`, còn TypeScript đang khai báo
    // `FormData` theo chuẩn DOM nên phải ép kiểu ở đây.
    form.append('file', { uri: image.uri, name: image.name, type: image.type } as unknown as Blob);
  }

  const { data } = await apiClient.post<StudentResponseDto>(`/api/students/${id}/avatar`, form);
  return data;
}

export async function deleteStudentAvatar(id: number): Promise<StudentResponseDto> {
  const { data } = await apiClient.delete<StudentResponseDto>(`/api/students/${id}/avatar`);
  return data;
}
