import createApiClient from "./api.service";

class StudentService {
  constructor(baseUrl = "/api/students") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách con cái thuộc về phụ huynh hiện tại
  async getAll() {
    return (await this.api.get("/")).data;
  }

  // Lấy thông tin 1 học sinh theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo hồ sơ học sinh mới
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật / đổi tên học sinh
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Tải lên ảnh đại diện cho học sinh — gửi FormData
  async uploadAvatar(id, file) {
    const formData = new FormData();
    formData.append("file", file);
    return (await this.api.post(`/${id}/avatar`, formData, {
      headers: {
        "Content-Type": "multipart/form-data"
      }
    })).data;
  }

  // Xóa ảnh đại diện của học sinh
  async deleteAvatar(id) {
    return (await this.api.delete(`/${id}/avatar`)).data;
  }

  // Xóa hồ sơ học sinh
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new StudentService();
