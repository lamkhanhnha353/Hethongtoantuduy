import createApiClient from "./api.service";

class ParentService {
  constructor(baseUrl = "/api/parents") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy danh sách phụ huynh
  async getAll(keyword = "") {
    return (await this.api.get(`/?keyword=${keyword}`)).data;
  }

  // Lấy danh sách tất cả hồ sơ học viên từ Backend
  async getAllStudents(keyword = "") {
    try {
      const res = await this.api.get(`/students${keyword ? `?keyword=${keyword}` : ""}`);
      return res.data;
    } catch {
      // Fallback: Lấy qua danh sách phụ huynh
      const parents = await this.getAll(keyword);
      const students = [];
      parents.forEach(p => {
        (p.students || []).forEach(s => {
          students.push({
            ...s,
            parentId: p.id,
            parentName: p.fullName,
            parentPhone: p.phone,
            parentEmail: p.email
          });
        });
      });
      return students;
    }
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
