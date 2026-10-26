import { API_URL } from '@/config/env';
import { jwtDecode } from 'jwt-decode'; //cài thư viện này bằng lệnh: npm install jwt-decode

export const paymentService = {
  createEnrollmentReservation: async (studentId: number, classId: number, coursePrice: number, token: string) => {
    try {
      // 1. Trích xuất ParentId từ Token
      let parentId = 0;
      try {
        const decoded: any = jwtDecode(token);
        parentId = parseInt(decoded.nameid || decoded.sub || "0", 10);
      } catch (e) {
        console.error("Lỗi giải mã token để lấy ParentId:", e);
      }

      const requestBody = {
        parentId: parentId, // Thêm ParentId vào request
        studentId: studentId,
        classId: classId,
        amount: coursePrice // Truyền Amount vào request
      };

      // console.log("Đang gửi Request tạo Đăng ký với data:", requestBody);

      // 2. Gọi API Enrollment ( /register)
      // 2. Gọi API Enrollment ( /register)
      const enrollResponse = await fetch(`${API_URL}/api/enrollment/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(requestBody)
      });

      // --- SỬA ĐOẠN NÀY ĐỂ BẮT ĐÚNG THÔNG BÁO LỖI TỪ C# ---
      if (!enrollResponse.ok) {
        let errorMessage = "Không thể tạo hóa đơn lúc này.";
        try {
            // Cố gắng đọc lỗi dạng JSON
            const errorData = await enrollResponse.json(); 
            if (errorData && errorData.message) {
                errorMessage = errorData.message;
            }
        } catch (e) {
            // Nếu không phải JSON, thử đọc dạng text
            const errorText = await enrollResponse.text();
            if (errorText) errorMessage = errorText;
        }
        
        // console.log("CHI TIẾT LỖI TỪ BACKEND ENROLLMENT:", enrollResponse.status, errorMessage);
        // Ném lỗi ra ngoài để màn hình giao diện nhận được
        throw new Error(errorMessage);
      }
      // ----------------------------------------------------

      const enrollData: { invoiceId?: number } = await enrollResponse.json();
      // console.log("Tạo Đăng ký thành công, dữ liệu trả về:", enrollData);

      if (!enrollData.invoiceId) {
        throw new Error('Đăng ký thành công nhưng không nhận được mã hóa đơn từ Server.');
      }

      return enrollData.invoiceId;
      
    } catch (error: any) {
      console.error('Lỗi quy trình thanh toán VNPay:', error);
      // QUAN TRỌNG: Không return null nữa, mà ném (throw) lỗi ra để màn hình giao diện nhận được
      throw error; 
    }
  },

  getVNPayUrlForInvoice: async (invoiceId: number, token: string): Promise<string> => {
    if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
      throw new Error('Mã hóa đơn không hợp lệ. Vui lòng tải lại thông tin đăng ký.');
    }

    const response = await fetch(`${API_URL}/api/payment/create-url/${invoiceId}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    });

    const responseText = await response.text();
    let data: { message?: string; paymentUrl?: string } = {};
    if (responseText) {
      try {
        data = JSON.parse(responseText);
      } catch {
        if (response.ok) {
          throw new Error('Server trả về dữ liệu link thanh toán không hợp lệ.');
        }
      }
    }

    if (!response.ok) {
      throw new Error(data.message || responseText || `Không thể tạo link thanh toán (HTTP ${response.status}).`);
    }

    if (!data.paymentUrl) {
      throw new Error('Không lấy được link VNPay từ Server.');
    }

    return data.paymentUrl;
  }
};