import createApiClient from "./api.service";

class CourseService {
  constructor(baseUrl = "/api/courses") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách tất cả khóa học [GET] /api/courses
  async getAll() {
    const res = await this.api.get("");
    return res.data;
  }

  // Lấy chi tiết một khóa học theo ID [GET] /api/courses/:id
  async get(id) {
    const res = await this.api.get(`/${id}`);
    return res.data;
  }

  // Tạo mới một khóa học [POST] /api/courses
  async create(data) {
    const res = await this.api.post("", data);
    return res.data;
  }

  // Cập nhật thông tin khóa học [PUT] /api/courses/:id
  async update(id, data) {
    const res = await this.api.put(`/${id}`, data);
    return res.data;
  }

  // Xóa khóa học [DELETE] /api/courses/:id
  async delete(id) {
    const res = await this.api.delete(`/${id}`);
    return res.data;
  }
}

export default new CourseService();
