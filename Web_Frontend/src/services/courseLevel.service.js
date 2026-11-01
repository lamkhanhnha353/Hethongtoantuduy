import createApiClient from "./api.service";

class CourseLevelService {
  constructor(baseUrl = "/api/courselevels") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách các cấp độ theo ID khóa học
  async getByCourse(courseId) {
    return (await this.api.get(`/course/${courseId}`)).data;
  }

  // Lấy thông tin một cấp độ theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới một cấp độ
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin cấp độ
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa cấp độ
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new CourseLevelService();
