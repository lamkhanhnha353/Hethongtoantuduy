/** Độ dài mật khẩu tối thiểu — khớp rule `MinLength(6)` của backend. */
export const MIN_PASSWORD_LENGTH = 6;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/u;

/**
 * Trả về `message` nếu value rỗng sau khi trim, ngược lại `undefined`.
 * Dùng được cho mọi field bắt buộc (mật khẩu chỉ check độ rỗng, không trim giá trị).
 */
export function required(value: string, message: string): string | undefined {
  return value.trim() === '' ? message : undefined;
}

/**
 * Check định dạng email — chỉ gọi khi field đã có giá trị,
 * phần bắt buộc do `required` đảm nhiệm.
 */
export function emailFormat(value: string, message: string): string | undefined {
  const trimmed = value.trim();

  if (trimmed === '') {
    return undefined;
  }

  return EMAIL_PATTERN.test(trimmed) ? undefined : message;
}
