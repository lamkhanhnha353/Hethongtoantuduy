import { API_URL } from '@/config/env';
import { jwtDecode } from 'jwt-decode'; //cài thư viện này bằng lệnh: npm install jwt-decode

export const paymentService = {
  processEnrollmentAndGetVNPayUrl: async (studentId: number, classId: number, coursePrice: number, token: string) => {
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

      const enrollData = await enrollResponse.json();
      // console.log("Tạo Đăng ký thành công, dữ liệu trả về:", enrollData);

      // 3. Backend đã trả về paymentUrl, ta chỉ việc lấy ra dùng
      if (enrollData.paymentUrl) {
          return enrollData.paymentUrl;
      } else {
          throw new Error('Đăng ký thành công nhưng không lấy được link VNPay từ Server.');
      }
      
    } catch (error: any) {
      console.error('Lỗi quy trình thanh toán VNPay:', error);
      // QUAN TRỌNG: Không return null nữa, mà ném (throw) lỗi ra để màn hình giao diện nhận được
      throw error; 
    }
  }
};