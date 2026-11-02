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

  // Lấy danh sách học viên hiện tại của lớp học
  async getStudents(classId) {
    return (await this.api.get(`/${classId}/students`)).data;
  }

  // Thêm học viên vào lớp học
  async addStudent(classId, studentId) {
    return (await this.api.post(`/${classId}/students`, { studentId })).data;
  }

  // Xóa / rút học viên khỏi lớp học
  async removeStudent(classId, studentId) {
    return (await this.api.delete(`/${classId}/students/${studentId}`)).data;
  }

  // Lấy danh sách lớp được phân công cho giáo viên hiện tại
  async getAssignedClasses(userProfile = null, teacherProfileId = null) {
    const rawClasses = await this.getAll();
    const list = Array.isArray(rawClasses) ? rawClasses : (rawClasses.items || []);
    if (!userProfile) return list;

    const role = (userProfile.role || "").toLowerCase();
    if (role === "admin") return list;

    const teacherFullName = (userProfile.fullName || "").trim().toLowerCase();

    const assigned = list.filter((c) => {
      if (teacherProfileId && c.teacherId === teacherProfileId) return true;
      if (teacherFullName && c.teacherName && c.teacherName.trim().toLowerCase() === teacherFullName) return true;
      return false;
    });

    return assigned.length > 0 ? assigned : list;
  }
}

export default new ClassService();
