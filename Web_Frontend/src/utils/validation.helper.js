/**
 * Utility helper for input validation (dates, numbers, text, phone, email)
 * Dùng chung cho các form trong ứng dụng để đảm bảo ràng buộc dữ liệu nhất quán.
 */

// ─── Regex dùng chung ────────────────────────────────────────────────────────

// Số điện thoại Việt Nam: bắt đầu bằng 0 hoặc +84, theo sau là 9 chữ số
export const PHONE_REGEX = /^(?:\+84|0)(?:\d){9}$/

// Email cơ bản
export const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/

// Mã PIN: 4 - 6 chữ số
export const PIN_REGEX = /^\d{4,6}$/

// Mã code: chữ, số, gạch ngang, gạch dưới
export const CODE_REGEX = /^[A-Za-z0-9_-]+$/

// ─── Chuỗi & số ─────────────────────────────────────────────────────────────

/**
 * Kiểm tra chuỗi rỗng (sau khi trim)
 */
export function isBlank(value) {
  return !value || String(value).trim().length === 0
}

/**
 * Kiểm tra độ dài chuỗi trong khoảng [min, max]
 * @returns {{ valid: boolean, message: string }}
 */
export function validateTextLength(value, { label = 'Nội dung', min = 0, max = Infinity, required = false } = {}) {
  const text = value == null ? '' : String(value).trim()

  if (!text) {
    return required
      ? { valid: false, message: `${label} không được để trống.` }
      : { valid: true, message: '' }
  }
  if (text.length < min) {
    return { valid: false, message: `${label} phải có ít nhất ${min} ký tự.` }
  }
  if (text.length > max) {
    return { valid: false, message: `${label} không được vượt quá ${max} ký tự.` }
  }
  return { valid: true, message: '' }
}

/**
 * Ép giá trị về số và kiểm tra nằm trong khoảng [min, max]
 * @returns {{ valid: boolean, message: string, value: number|null }}
 */
export function validateNumberRange(value, { label = 'Giá trị', min = -Infinity, max = Infinity, required = false, integer = false } = {}) {
  if (value === '' || value === null || value === undefined) {
    return required
      ? { valid: false, message: `${label} không được để trống.`, value: null }
      : { valid: true, message: '', value: null }
  }

  const num = Number(value)
  if (!Number.isFinite(num)) {
    return { valid: false, message: `${label} phải là một số hợp lệ.`, value: null }
  }
  if (integer && !Number.isInteger(num)) {
    return { valid: false, message: `${label} phải là số nguyên.`, value: num }
  }
  if (num < min) {
    return { valid: false, message: `${label} phải lớn hơn hoặc bằng ${min}.`, value: num }
  }
  if (num > max) {
    return { valid: false, message: `${label} phải nhỏ hơn hoặc bằng ${max}.`, value: num }
  }
  return { valid: true, message: '', value: num }
}

// ─── Ngày & giờ ─────────────────────────────────────────────────────────────

/**
 * Chuyển giá trị ngày/giờ sang timestamp (ms). Trả về NaN nếu không hợp lệ.
 */
export function toTimestamp(value) {
  if (!value) return NaN
  const time = new Date(value).getTime()
  return Number.isFinite(time) ? time : NaN
}

/**
 * Ràng buộc thứ tự ngày/giờ: start <= end
 * @param {string|Date} start
 * @param {string|Date} end
 * @param {{ startLabel?: string, endLabel?: string, allowEqual?: boolean, required?: boolean }} options
 * @returns {{ valid: boolean, message: string }}
 */
export function validateDateRange(start, end, {
  startLabel = 'Ngày bắt đầu',
  endLabel = 'Ngày kết thúc',
  allowEqual = true,
  required = false,
} = {}) {
  if (required && !start) {
    return { valid: false, message: `${startLabel} không được để trống.` }
  }
  if (required && !end) {
    return { valid: false, message: `${endLabel} không được để trống.` }
  }

  // Nếu một trong hai trống thì không kiểm tra thứ tự (cho phép tùy chọn)
  if (!start || !end) return { valid: true, message: '' }

  const startTime = toTimestamp(start)
  const endTime = toTimestamp(end)

  if (Number.isNaN(startTime)) {
    return { valid: false, message: `${startLabel} không hợp lệ.` }
  }
  if (Number.isNaN(endTime)) {
    return { valid: false, message: `${endLabel} không hợp lệ.` }
  }

  const isInvalid = allowEqual ? endTime < startTime : endTime <= startTime
  if (isInvalid) {
    return {
      valid: false,
      message: allowEqual
        ? `${endLabel} phải sau hoặc bằng ${startLabel.toLowerCase()}.`
        : `${endLabel} phải sau ${startLabel.toLowerCase()}.`,
    }
  }

  return { valid: true, message: '' }
}

/**
 * Kiểm tra ngày không nằm trong tương lai (dùng cho ngày sinh, ngày áp dụng...)
 */
export function validateNotFuture(value, { label = 'Ngày', allowToday = true } = {}) {
  if (!value) return { valid: true, message: '' }
  const time = toTimestamp(value)
  if (Number.isNaN(time)) return { valid: false, message: `${label} không hợp lệ.` }

  const now = Date.now()
  const limit = allowToday ? new Date().setHours(23, 59, 59, 999) : now
  if (time > limit) {
    return { valid: false, message: `${label} không được ở tương lai.` }
  }
  return { valid: true, message: '' }
}

// ─── Điện thoại & email ─────────────────────────────────────────────────────

export function validatePhone(value, { label = 'Số điện thoại', required = false } = {}) {
  const phone = value == null ? '' : String(value).trim()
  if (!phone) {
    return required
      ? { valid: false, message: `${label} không được để trống.` }
      : { valid: true, message: '' }
  }
  if (!PHONE_REGEX.test(phone)) {
    return { valid: false, message: `${label} không hợp lệ (VD: 0912345678).` }
  }
  return { valid: true, message: '' }
}

export function validateEmail(value, { label = 'Email', required = false } = {}) {
  const email = value == null ? '' : String(value).trim()
  if (!email) {
    return required
      ? { valid: false, message: `${label} không được để trống.` }
      : { valid: true, message: '' }
  }
  if (!EMAIL_REGEX.test(email)) {
    return { valid: false, message: `${label} không đúng định dạng.` }
  }
  if (email.length > 256) {
    return { valid: false, message: `${label} không được vượt quá 256 ký tự.` }
  }
  return { valid: true, message: '' }
}

/**
 * Chạy lần lượt nhiều kết quả kiểm tra, trả về lỗi đầu tiên (nếu có).
 * @param {...{ valid: boolean, message: string }} results
 * @returns {{ valid: boolean, message: string }}
 */
export function firstError(...results) {
  for (const result of results) {
    if (result && !result.valid) return result
  }
  return { valid: true, message: '' }
}
