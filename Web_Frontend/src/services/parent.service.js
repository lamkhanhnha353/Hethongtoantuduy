import createApiClient from "./api.service";

class ParentService {
  constructor(baseUrl = "/api/parents") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách phụ huynh
  async getAll(keyword = "") {
    return (await this.api.get(`/?keyword=${keyword}`)).data;
  }

  // Lấy thông tin 1 phụ huynh theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới tài khoản phụ huynh (dành cho Admin)
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin phụ huynh
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Cập nhật email của phụ huynh
  async updateEmail(id, data) {
    return (await this.api.put(`/${id}/email`, data)).data;
  }

  // Xóa tài khoản phụ huynh
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new ParentService();