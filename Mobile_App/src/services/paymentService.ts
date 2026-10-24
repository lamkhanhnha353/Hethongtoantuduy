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

      console.log("Đang gửi Request tạo Đăng ký với data:", requestBody);

      // 2. Gọi API Enrollment ( /register)
      const enrollResponse = await fetch(`${API_URL}/api/enrollment/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(requestBody)
      });

      if (!enrollResponse.ok) {
        const errorText = await enrollResponse.text();
        console.log("CHI TIẾT LỖI TỪ BACKEND ENROLLMENT:", enrollResponse.status, errorText);
        throw new Error(`Mã lỗi: ${enrollResponse.status} - Chi tiết: ${errorText}`);
      }

      const enrollData = await enrollResponse.json();
      console.log("Tạo Đăng ký thành công, dữ liệu trả về:", enrollData);

      // 3. Backend đã trả về paymentUrl, ta chỉ việc lấy ra dùng
      if (enrollData.paymentUrl) {
          return enrollData.paymentUrl;
      } else {
          throw new Error('Đăng ký thành công nhưng không lấy được link VNPay từ Server.');
      }
      
    } catch (error) {
      console.error('Lỗi quy trình thanh toán VNPay:', error);
      return null;
    }
  }
};