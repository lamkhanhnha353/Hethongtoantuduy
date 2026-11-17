import createApiClient from "./api.service";

class ClassScheduleService {
  constructor(baseUrl = "/api/classschedules") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy lịch học theo ID lớp học [GET] /api/classschedules/class/{classId}
  async getByClass(classId) {
    return (await this.api.get(`/class/${classId}`)).data;
  }

  // Lấy thông tin lịch học theo ID [GET] /api/classschedules/{id}
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo lịch học mới [POST] /api/classschedules
  async create(data) {
    return (await this.api.post("", data)).data;
  }

  // Cập nhật thông tin lịch học [PUT] /api/classschedules/{id}
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa lịch học [DELETE] /api/classschedules/{id}
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new ClassScheduleService();
