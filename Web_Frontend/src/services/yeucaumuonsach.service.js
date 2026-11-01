import createApiClient from "./api.service";

class YeuCauMuonSachService {
  constructor(baseUrl = "/api/yeucaumuonsach") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy tất cả yêu cầu
  async getAll() {
    return (await this.api.get("/")).data;
  }

  // Tạo yêu cầu mới (dùng cho phía Độc giả)
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Lấy chi tiết một yêu cầu (nếu cần)
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Cập nhật trạng thái (Duyệt/Trả/Hủy...)
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa yêu cầu (nếu cần)
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new YeuCauMuonSachService();
