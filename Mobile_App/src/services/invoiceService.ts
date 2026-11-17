import { apiClient } from '@/services/apiClient';
import type { Invoice } from '@/types/invoice';

export const invoiceService = {
  async getHistory(): Promise<Invoice[]> {
    const { data } = await apiClient.get<unknown>('/api/invoice/history');
    if (!Array.isArray(data)) {
      throw new Error('Dữ liệu lịch sử hóa đơn không hợp lệ.');
    }

    return data as Invoice[];
  },

  async cancel(invoiceId: number): Promise<void> {
    await apiClient.post(`/api/invoice/cancel/${invoiceId}`);
  },
};
