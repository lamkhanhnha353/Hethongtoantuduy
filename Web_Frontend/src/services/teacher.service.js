import createApiClient from "./api.service";

class TeacherService {
  constructor(baseUrl = "/api/teachers") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách giáo viên
  async getAll(keyword = "") {
    return (await this.api.get(`/?keyword=${keyword}`)).data;
  }

  // Lấy thông tin 1 giáo viên theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới hồ sơ giáo viên
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin giáo viên
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa tài khoản giáo viên
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new TeacherService();