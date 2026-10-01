export type ApiErrorPayload = {
  code?: number;
  message?: string;
  details?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  status: number;
  fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}
