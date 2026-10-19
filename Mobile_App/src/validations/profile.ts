import { emailFormat, required } from '@/validations/common';

export type ProfileFormValues = {
  fullName: string;
  email: string;
};

export type ProfileFieldErrors = { fullName?: string; email?: string };

export type ProfileFormOptions = {
  /**
   * Email chỉ bắt buộc/khóa theo trạng thái hồ sơ (Parent chưa xác minh email).
   * Khi `false`, bỏ qua mọi check của field email.
   */
  requireEmail: boolean;
};

/**
 * Validate form Cá nhân trước khi gửi `PUT /api/profile` và `PUT /api/profile/email`.
 */
export function validateProfileForm(
  values: ProfileFormValues,
  options: ProfileFormOptions,
): ProfileFieldErrors {
  const errors: ProfileFieldErrors = {};

  const nameError = required(values.fullName, 'Họ và tên không được để trống.');
  if (nameError) {
    errors.fullName = nameError;
  }

  if (options.requireEmail) {
    const emailRequiredError = required(values.email, 'Email không được để trống.');

    if (emailRequiredError) {
      errors.email = emailRequiredError;
    } else {
      errors.email = emailFormat(values.email, 'Email không đúng định dạng.');
    }
  }

  return errors;
}
