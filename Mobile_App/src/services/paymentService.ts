import { apiClient } from '@/services/apiClient';

type EnrollmentResponse = {
  invoiceId?: number;
};

type PaymentUrlResponse = {
  paymentUrl?: string;
};

export const paymentService = {
  async createEnrollmentReservation(studentId: number, classId: number,): Promise<number> {
    const { data } = await apiClient.post<EnrollmentResponse>('/api/enrollment/register', {
      studentId,
      classId,
    });

    const invoiceId = data?.invoiceId;
    if (typeof invoiceId !== 'number' || !Number.isInteger(invoiceId) || invoiceId <= 0) {
      throw new Error('Đăng ký thành công nhưng không nhận được mã hóa đơn từ Server.');
    }

    return invoiceId;
  },

  async getVNPayUrlForInvoice(invoiceId: number): Promise<string> {
    if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
      throw new Error('Mã hóa đơn không hợp lệ. Vui lòng tải lại thông tin đăng ký.');
    }

    const { data } = await apiClient.post<PaymentUrlResponse>(
      `/api/payment/create-url/${invoiceId}`,
    );

    if (typeof data?.paymentUrl !== 'string' || !data.paymentUrl) {
      throw new Error('Không lấy được link VNPay từ Server.');
    }

    return data.paymentUrl;
  },
};
