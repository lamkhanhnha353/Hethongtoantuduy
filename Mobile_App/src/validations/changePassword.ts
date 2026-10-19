import { MIN_PASSWORD_LENGTH, required } from '@/validations/common';

export type ChangePasswordValues = {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
};

export type ChangePasswordFieldErrors = Partial<Record<keyof ChangePasswordValues, string>>;

/**
 * Validate form đổi mật khẩu phía client TRƯỚC khi gọi
 * `POST /api/auth/change-password`. Lỗi server vẫn được merge bổ sung ở màn hình.
 */
export function validateChangePassword(values: ChangePasswordValues): ChangePasswordFieldErrors {
  const errors: ChangePasswordFieldErrors = {};

  const currentError = required(values.currentPassword, 'Mật khẩu hiện tại là bắt buộc.');
  if (currentError) {
    errors.currentPassword = currentError;
  }

  if (values.newPassword.length < MIN_PASSWORD_LENGTH) {
    errors.newPassword = `Mật khẩu mới phải có ít nhất ${MIN_PASSWORD_LENGTH} ký tự.`;
  } else if (values.newPassword === values.currentPassword) {
    errors.newPassword = 'Mật khẩu mới phải khác mật khẩu hiện tại.';
  }

  if (values.confirmNewPassword !== values.newPassword) {
    errors.confirmNewPassword = 'Mật khẩu xác nhận không khớp.';
  }

  return errors;
}
