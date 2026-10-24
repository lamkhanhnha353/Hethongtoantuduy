import createApiClient from "./api.service";

class CourseService {
  constructor(baseUrl = "/api/courses") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách tất cả khóa học
  async getAll() {
    return (await this.api.get("/")).data;
  }

  // Lấy chi tiết một khóa học theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới một khóa học
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin khóa học
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa khóa học
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new CourseService();
