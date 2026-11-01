import createApiClient from "./api.service";

class AttendanceService {
  constructor(baseUrl = "/api/attendance") {
    this.api = createApiClient(baseUrl);
  }

  /**
   * Lấy danh sách điểm danh cả lớp trong buổi học, kèm thống kê và đơn xin nghỉ phép
   * GET /api/attendance/lesson/{lessonId}
   */
  async getByLesson(lessonId) {
    return (await this.api.get(`/lesson/${lessonId}`)).data;
  }

  /**
   * Điểm danh cho nhiều học sinh trong buổi học (truyền danh sách qua Body)
   * POST /api/attendance/lesson/{lessonId}
   * @param {number} lessonId
   * @param {Array<{ studentId: number, status: string, teacherNotes?: string }>} items
   */
  async saveBatchByLesson(lessonId, items) {
    return (await this.api.post(`/lesson/${lessonId}`, items)).data;
  }

  /**
   * Điểm danh cho nhiều học sinh qua payload có sẵn lessonId
   * POST /api/attendance/batch
   * @param {{ lessonId: number, attendances: Array<{ studentId: number, status: string, teacherNotes?: string }> }} payload
   */
  async saveBatch(payload) {
    return (await this.api.post("/batch", payload)).data;
  }

  /**
   * Cập nhật điểm danh cho một học sinh lẻ
   * PUT /api/attendance/lesson/{lessonId}/student/{studentId}
   * @param {number} lessonId
   * @param {number} studentId
   * @param {{ studentId?: number, status: string, teacherNotes?: string }} data
   */
  async updateSingle(lessonId, studentId, data) {
    return (await this.api.put(`/lesson/${lessonId}/student/${studentId}`, data)).data;
  }

  /**
   * Lấy lịch sử điểm danh của một học sinh (kèm classId lọc tùy chọn)
   * GET /api/attendance/student/{studentId}?classId={classId}
   * @param {number} studentId
   * @param {number} [classId]
   */
  async getStudentHistory(studentId, classId = null) {
    const params = classId ? { classId } : {};
    return (await this.api.get(`/student/${studentId}`, { params })).data;
  }
}

export default new AttendanceService();
