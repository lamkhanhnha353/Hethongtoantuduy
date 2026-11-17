import axios from "axios";

// Cho phép tùy chỉnh URL qua biến môi trường hoặc mặc định qua port 8000 của FastAPI Worker
const WORKER_HOST = import.meta.env.VITE_IMPORT_WORKER_URL || "http://127.0.0.1:8000";

function toRequestError(error, fallbackMessage) {
  const requestError = new Error(
    error.response?.data?.detail || error.message || fallbackMessage,
  );
  if (error.response) {
    requestError.status = error.response.status;
  }
  return requestError;
}

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
      const res = await this.api.get(`/api/parser/job/${jobId}`, { timeout: 10000 });
      return res.data;
    } catch (err) {
      if (err.response) {
        throw toRequestError(err, "Không thể kiểm tra tiến độ Job.");
      }

      try {
        const proxyRes = await axios.get(`/api/parser/job/${jobId}`, { timeout: 10000 });
        return proxyRes.data;
      } catch (fallbackErr) {
        throw toRequestError(fallbackErr, "Không thể kết nối đến Import Worker.");
      }
    }
  }

  /**
   * Tự động thăm dò tiến độ xử lý (Polling) cho đến khi Job COMPLETED hoặc FAILED
   * @param {string} jobId
   * @param {(statusData: any) => void} onProgressCallback
   * @param {number} intervalMs - Khoảng cách giữa các lần check (mặc định 2500ms)
   * @param {number} maxAttempts - Số lần thăm dò tối đa (mặc định 240 lần ~ 10 phút)
   */
  async pollJobUntilDone(jobId, onProgressCallback = null, intervalMs = 2500, maxAttempts = 240) {
    let attempts = 0;
    let consecutiveConnectionErrors = 0;

    return new Promise((resolve, reject) => {
      let timer;
      const poll = async () => {
        attempts++;
        try {
          const statusData = await this.getJobStatus(jobId);
          consecutiveConnectionErrors = 0;

          if (onProgressCallback && typeof onProgressCallback === "function") {
            onProgressCallback(statusData);
          }

          if (statusData.status === "COMPLETED") {
            clearTimeout(timer);
            resolve(statusData);
          } else if (statusData.status === "FAILED") {
            clearTimeout(timer);
            const errReason = statusData.error_message || "Xử lý thất bại không xác định";
            reject(new Error(errReason));
          } else if (attempts >= maxAttempts) {
            clearTimeout(timer);
            reject(new Error("Thời gian xử lý quá lâu (Timeout). Vui lòng thử lại với file nhỏ hơn."));
          } else {
            timer = setTimeout(poll, intervalMs);
          }
        } catch (err) {
          if (err.status && err.status < 500) {
            clearTimeout(timer);
            reject(err);
            return;
          }

          consecutiveConnectionErrors++;
          if (consecutiveConnectionErrors >= 5 || attempts >= maxAttempts) {
            clearTimeout(timer);
            reject(
              new Error(
                `Mất kết nối với Import Worker sau ${consecutiveConnectionErrors} lần thử. `
                + "Hãy kiểm tra worker và chạy lại lệnh khởi động.",
              ),
            );
            return;
          }

          const retryDelay = Math.min(intervalMs * 2 ** (consecutiveConnectionErrors - 1), 10000);
          timer = setTimeout(poll, retryDelay);
        }
      };

      timer = setTimeout(poll, 0);
    });
  }
}

export default new ImportWorkerService();
