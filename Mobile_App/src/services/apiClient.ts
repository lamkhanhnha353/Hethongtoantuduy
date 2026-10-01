import { create, isCancel, type AxiosError, type AxiosInstance } from 'axios';

import { API_TIMEOUT_MS, API_URL, REQUEST_HEADERS } from '@/config/env';
import { ApiError, type ApiErrorPayload } from '@/services/apiError';

const NETWORK_ERROR_STATUS = 0;
const INVALID_JSON_STATUS = 502;

function toApiError(status: number, payload: ApiErrorPayload): ApiError {
  const fieldErrors = payload.errors ?? {};
  const firstFieldMessage = Object.values(fieldErrors)[0]?.[0];
  const message =
    firstFieldMessage ??
    payload.message ??
    payload.details ??
    'Đã xảy ra lỗi, vui lòng thử lại.';

  return new ApiError(status, message, fieldErrors);
}

/**
 * Axios instance dùng chung cho toàn app: baseURL, timeout và header đã cấu hình sẵn
 * nên service chỉ cần gọi `apiClient.post(...)` là xong.
 */
export const apiClient: AxiosInstance = create({
  baseURL: API_URL,
  timeout: API_TIMEOUT_MS,
  headers: REQUEST_HEADERS,
});

/**
 * Chuyển mọi lỗi axios về ApiError để tầng UI chỉ cần bắt một kiểu lỗi duy nhất,
 * không cần phân biệt lỗi mạng / timeout / HTTP.
 */
// setup interceptor để chuyển mọi lỗi axios về ApiError    
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorPayload>) => {
    if (isCancel(error) || error.code === 'ERR_CANCELED') {
      throw new ApiError(408, 'Yêu cầu đã bị hủy.');
    }

    if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') {
      throw new ApiError(408, 'Yêu cầu quá thời gian chờ, vui lòng thử lại.');
    }

    if (!error.response) {
      throw new ApiError(
        NETWORK_ERROR_STATUS,
        'Không kết nối được tới máy chủ. Kiểm tra mạng và cấu hình API URL.',
      );
    }

    const payload = error.response.data;

    if (!payload || typeof payload !== 'object') {
      throw new ApiError(INVALID_JSON_STATUS, 'Máy chủ trả về dữ liệu không hợp lệ.');
    }

    throw toApiError(error.response.status, payload);
  },
);

export function authHeaders(accessToken: string) {
  return { Authorization: `Bearer ${accessToken}` };
}
