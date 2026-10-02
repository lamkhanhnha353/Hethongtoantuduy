/**
 * Backend hiện trả về HAI envelope lỗi khác nhau, cần đọc cả hai:
 *
 * 1. `ValidationProblemDetails` — do `[ApiController]` tự sinh khi ModelState invalid,
 *    short-circuit TRƯỚC khi vào action body (nên `BadRequest(ModelState)` trong
 *    `AuthController` là dead code).
 * 2. Anonymous object — do controller tự `BadRequest()` / `Unauthorized()` / `StatusCode()`.
 */
export type ValidationProblemDetailsPayload = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string | null;
  instance?: string | null;
  /**
   * Key là tên property C# chứ KHÔNG phải tên camelCase trong JSON, vì
   * `ValidationProblemDetails(ModelStateDictionary)` copy nguyên văn key của ModelState
   * và `DictionaryKeyPolicy` mặc định là `null`.
   * Với các endpoint auth sẽ là: `Phone`, `Password`, `Role`, `FullName`, `Email`.
   * Ngoài ra có thể gặp `""` (body rỗng) hoặc `$.phone` (sai kiểu JSON).
   */
  errors?: Record<string, string[]>;
  traceId?: string;
};

export type ApiErrorBody = {
  code?: number;
  message?: string;
  details?: string;
};

export type ApiErrorPayload = ValidationProblemDetailsPayload | ApiErrorBody;

const JSON_PATH_ERROR_KEY = /^\$\.(?<path>.+)$/;

/**
 * Đưa key của `errors` về dạng camelCase để khớp tên field trong form
 * (`phone`, `fullName`, ...) thay vì `Phone`, `FullName`.
 * Key không gắn với field cụ thể (`""`, `$.profiles[0].x`) trả về `null` để bỏ qua.
 */
export function normalizeFieldKey(rawKey: string): string | null {
  const trimmed = rawKey.trim();

  if (trimmed === '') {
    return null;
  }

  const path = trimmed.match(JSON_PATH_ERROR_KEY)?.groups?.path ?? trimmed;
  const name = path.split(/[.[]/u)[0];

  if (!name) {
    return null;
  }

  return name.charAt(0).toLowerCase() + name.slice(1);
}

export function readValidationErrors(
  payload: ApiErrorPayload,
): Record<string, string[]> {
  if (!('errors' in payload) || !payload.errors) {
    return {};
  }

  return Object.entries(payload.errors).reduce<Record<string, string[]>>(
    (acc, [rawKey, messages]) => {
      const key = normalizeFieldKey(rawKey);

      if (key) {
        acc[key] = messages;
      }

      return acc;
    },
    {},
  );
}

export function readErrorMessage(payload: ApiErrorPayload): string | undefined {
  if ('message' in payload && payload.message) {
    return payload.message;
  }

  if ('details' in payload && payload.details) {
    return payload.details;
  }

  return 'detail' in payload && payload.detail ? payload.detail : undefined;
}

export class ApiError extends Error {
  status: number;
  /** Đã normalize về camelCase để map thẳng lên field của form. */
  fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}