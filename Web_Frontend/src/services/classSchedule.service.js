import createApiClient from "./api.service";

class ClassScheduleService {
  constructor(baseUrl = "/api/classschedules") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy lịch học theo ID lớp học
  async getByClass(classId) {
    return (await this.api.get(`/class/${classId}`)).data;
  }

  // Lấy thông tin lịch học theo ID
  async get(id) {
    return (await this.api.get(`/${id}`)).data;
  }

  // Tạo lịch học mới
  async create(data) {
    return (await this.api.post("/", data)).data;
  }

  // Cập nhật thông tin lịch học
  async update(id, data) {
    return (await this.api.put(`/${id}`, data)).data;
  }

  // Xóa lịch học
  async delete(id) {
    return (await this.api.delete(`/${id}`)).data;
  }
}

export default new ClassScheduleService();
