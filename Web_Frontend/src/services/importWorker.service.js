import axios from "axios";

// Cho phép tùy chỉnh URL qua biến môi trường hoặc mặc định qua port 8000 của FastAPI Worker
const WORKER_HOST = import.meta.env.VITE_IMPORT_WORKER_URL || "http://localhost:8000";

class ImportWorkerService {
  constructor() {
    this.api = axios.create({
      baseURL: WORKER_HOST,
      timeout: 120000, // 2 phút cho các file đề thi lớn
    });
  }

  /**
   * Kiểm tra tình trạng hoạt động (Health check) của Import Worker
   */
  async checkHealth() {
    try {
      const res = await this.api.get("/health", { timeout: 3000 });
      return res.status === 200 && res.data?.status === "healthy";
    } catch {
      // Thử qua Vite proxy nếu gọi trực tiếp thất bại do CORS hoặc firewall nội bộ
      try {
        const proxyRes = await axios.get("/health", { timeout: 3000 });
        return proxyRes.status === 200;
      } catch {
        return false;
      }
    }
  }

  /**
   * Gửi file đề thi (.docx, .pdf) lên Import Worker để bắt đầu tiến trình xử lý
   * @param {File} file - File đề thi từ máy tính
   * @param {number} bankId - ID ngân hàng câu hỏi đích
   * @param {string|null} jobId - (Tùy chọn) Mã Job custom
   * @returns {Promise<{ job_id: string, status: string, message: string, file_name: string, bank_id: number, check_status_url: string }>}
   */
  async uploadFile(file, bankId, jobId = null) {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("bank_id", String(bankId));
    if (jobId) {
      formData.append("job_id", jobId);
    }

    try {
      const res = await this.api.post("/api/parser/process-file", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });
      return res.data;
    } catch (err) {
      // Fallback thử qua proxy `/api/parser/process-file`
      try {
        const proxyRes = await axios.post("/api/parser/process-file", formData, {
          headers: {
            "Content-Type": "multipart/form-data",
          },
        });
        return proxyRes.data;
      } catch (fallbackErr) {
        throw (
          err.response?.data?.detail ||
          fallbackErr.response?.data?.detail ||
          err.message ||
          "Không thể kết nối đến Question Import Worker (Port 8000)."
        );
      }
    }
  }

  /**
   * Lấy chi tiết trạng thái xử lý của Job và danh sách câu hỏi staging đã bóc tách
   * @param {string} jobId
   */
  async getJobStatus(jobId) {
    try {
      const res = await this.api.get(`/api/parser/job/${jobId}`);
      return res.data;
    } catch (err) {
      try {
        const proxyRes = await axios.get(`/api/parser/job/${jobId}`);
        return proxyRes.data;
      } catch (fallbackErr) {
        throw (
          err.response?.data?.detail ||
          fallbackErr.response?.data?.detail ||
          err.message ||
          "Không thể kiểm tra tiến độ Job."
        );
      }
    }
  }

  /**
   * Tự động thăm dò tiến độ xử lý (Polling) cho đến khi Job COMPLETED hoặc FAILED
   * @param {string} jobId
   * @param {(statusData: any) => void} onProgressCallback
   * @param {number} intervalMs - Khoảng cách mỗi lần check (mặc định 1500ms)
   * @param {number} maxAttempts - Số lần thăm dò tối đa (mặc định 80 lần ~ 2 phút)
   */
  async pollJobUntilDone(jobId, onProgressCallback = null, intervalMs = 1500, maxAttempts = 80) {
    let attempts = 0;

    return new Promise((resolve, reject) => {
      const timer = setInterval(async () => {
        attempts++;
        try {
          const statusData = await this.getJobStatus(jobId);

          if (onProgressCallback && typeof onProgressCallback === "function") {
            onProgressCallback(statusData);
          }

          if (statusData.status === "COMPLETED") {
            clearInterval(timer);
            resolve(statusData);
          } else if (statusData.status === "FAILED") {
            clearInterval(timer);
            const errReason = statusData.error_message || "Xử lý thất bại không xác định";
            reject(new Error(errReason));
          } else if (attempts >= maxAttempts) {
            clearInterval(timer);
            reject(new Error("Thời gian xử lý quá lâu (Timeout). Vui lòng thử lại với file nhỏ hơn."));
          }
        } catch (err) {
          if (attempts >= maxAttempts) {
            clearInterval(timer);
            reject(err);
          }
        }
      }, intervalMs);
    });
  }
}

export default new ImportWorkerService();
