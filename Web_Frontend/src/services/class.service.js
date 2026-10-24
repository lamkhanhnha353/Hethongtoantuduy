import createApiClient from "./api.service";

class ClassService {
  constructor(baseUrl = "/api/classes") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách tất cả lớp học
  async getAll() {
    return (await this.api.get("/")).data;
  }

  // Lấy danh sách lớp học theo ID cấp độ khóa học
  async getByCourseLevel(levelId) {
    return (await this.api.get(`/course-level/${levelId}`)).data;
  }

  // Lấy thông tin một lớp học theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới một lớp học
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin lớp học
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa lớp học
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new ClassService();
