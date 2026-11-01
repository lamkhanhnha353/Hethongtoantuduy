import { API_URL } from '@/config/env';
import type { Invoice } from '@/types/invoice';

async function readError(response: Response, fallback: string): Promise<Error> {
  const body = await response.text();
  if (!body) return new Error(fallback);

  try {
    const data = JSON.parse(body) as { message?: string; title?: string };
    return new Error(data.message || data.title || fallback);
  } catch {
    return new Error(body || fallback);
  }
}

export const invoiceService = {
  async getHistory(token: string): Promise<Invoice[]> {
    const response = await fetch(`${API_URL}/api/invoice/history`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw await readError(response, 'Không thể tải danh sách hóa đơn.');
    }

    const data: unknown = await response.json();
    if (!Array.isArray(data)) {
      throw new Error('Dữ liệu lịch sử hóa đơn không hợp lệ.');
    }

    return data as Invoice[];
  },

  async cancel(invoiceId: number, token: string): Promise<void> {
    const response = await fetch(`${API_URL}/api/invoice/cancel/${invoiceId}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      throw await readError(response, 'Không thể hủy hóa đơn.');
    }
  },
};
