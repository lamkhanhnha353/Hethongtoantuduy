import createApiClient from "./api.service";

class LessonService {
  constructor(baseUrl = "/api/lessons") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy tất cả buổi học [GET] /api/lessons
  async getAll() {
    return (await this.api.get("")).data;
  }

  // Lấy danh sách buổi học theo ID lớp [GET] /api/lessons/class/:classId
  async getByClass(classId) {
    return (await this.api.get(`/class/${classId}`)).data;
  }

  // Lấy thông tin 1 buổi học theo ID [GET] /api/lessons/:id
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo mới buổi học [POST] /api/lessons
  async create(data) {
    return (await this.api.post("", data)).data;
  }

  // Cập nhật buổi học [PUT] /api/lessons/:id
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa buổi học [DELETE] /api/lessons/:id
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new LessonService();
