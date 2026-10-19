import { emailFormat, required } from '@/validations/common';

export type ForgotPasswordValues = {
  email: string;
};

export type ForgotPasswordFieldErrors = { email?: string };

/**
 * Validate form quên mật khẩu (nhập email) — khớp rule của
 * `ForgotPasswordRequest` phía backend (`[Required]` + `[EmailAddress]`).
 */
export function validateForgotPassword(values: ForgotPasswordValues): ForgotPasswordFieldErrors {
  const errors: ForgotPasswordFieldErrors = {};

  const emailRequiredError = required(values.email, 'Email là bắt buộc.');

  if (emailRequiredError) {
    errors.email = emailRequiredError;
  } else {
    const emailFormatError = emailFormat(values.email, 'Email không đúng định dạng.');
    if (emailFormatError) {
      errors.email = emailFormatError;
    }
  }

  return errors;
}
