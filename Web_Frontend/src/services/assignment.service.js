import createApiClient from "./api.service";

class AssignmentService {
  constructor(baseUrl = "/api/assignments") {
    this.api = createApiClient(baseUrl);
  }

  /**
   * Lấy tất cả bài kiểm tra gắn với một buổi học (Lesson)
   * GET /api/assignments/lesson/{lessonId}
   */
  async getByLesson(lessonId) {
    const res = await this.api.get(`/lesson/${lessonId}`);
    return res.data;
  }

  /**
   * Lấy chi tiết bài kiểm tra theo ID (kèm danh sách câu hỏi và học sinh được giao)
   * GET /api/assignments/{id}
   */
  async getById(id) {
    const res = await this.api.get(`/${id}`);
    return res.data;
  }

  /**
   * Tạo bài kiểm tra mới gắn với buổi học (Lesson)
   * POST /api/assignments
   * @param {{
   *   lessonId: number,
   *   title: string,
   *   startTime: string,
   *   deadline: string,
   *   timeLimitMinutes?: number,
   *   maxAttempt?: number,
   *   status?: string,
   *   questions?: Array<{ questionId: number, orderIndex: number, pointValue: number }>,
   *   assignAllStudentsInClass?: boolean,
   *   specificStudentIds?: number[]
   * }} data
   */
  async create(data) {
    const res = await this.api.post("", data);
    return res.data;
  }

  /**
   * Nhân bản bài kiểm tra có sẵn sang một buổi học (Lesson) mới
   * POST /api/assignments/clone
   * @param {{
   *   sourceAssignmentId: number,
   *   targetLessonId: number,
   *   newTitle?: string,
   *   startTime?: string,
   *   deadline?: string,
   *   assignAllStudentsInClass?: boolean,
   *   specificStudentIds?: number[]
   * }} data
   */
  async clone(data) {
    const res = await this.api.post("/clone", data);
    return res.data;
  }

  /**
   * Cập nhật thông tin, hạn nộp, và cơ cấu câu hỏi bài kiểm tra
   * PUT /api/assignments/{id}
   */
  async update(id, data) {
    const res = await this.api.put(`/${id}`, data);
    return res.data;
  }

  /**
   * Xóa bài kiểm tra
   * DELETE /api/assignments/{id}
   */
  async delete(id) {
    const res = await this.api.delete(`/${id}`);
    return res.data;
  }

  /**
   * Đính kèm / Giao bài kiểm tra riêng lẻ cho một hoặc nhiều học sinh trong lớp
   * POST /api/assignments/{id}/students
   * @param {number} id
   * @param {number[]} studentIds
   */
  async assignStudents(id, studentIds) {
    const res = await this.api.post(`/${id}/students`, { studentIds });
    return res.data;
  }

  /**
   * Hủy giao bài kiểm tra cho một học sinh lẻ (khi chưa nộp bài)
   * DELETE /api/assignments/{id}/students/{studentId}
   * @param {number} id
   * @param {number} studentId
   */
  async unassignStudent(id, studentId) {
    const res = await this.api.delete(`/${id}/students/${studentId}`);
    return res.data;
  }

  /**
   * Lấy danh sách các bài kiểm tra được giao của một học sinh
   * GET /api/assignments/student/{studentId}?lessonId={lessonId}
   * @param {number} studentId
   * @param {number|null} lessonId
   */
  async getByStudent(studentId, lessonId = null) {
    const params = {};
    if (lessonId) params.lessonId = lessonId;
    const res = await this.api.get(`/student/${studentId}`, { params });
    return res.data;
  }

  /**
   * Chấm điểm và lưu nhận xét đánh giá cho một học sinh lẻ
   * PUT /api/assignments/{id}/grade/{studentId}
   * @param {number} assignmentId
   * @param {number} studentId
   * @param {{ score?: number, teacherFeedback?: string, status?: string }} data
   */
  async gradeStudent(assignmentId, studentId, data) {
    const res = await this.api.put(`/${assignmentId}/grade/${studentId}`, data);
    return res.data;
  }

  /**
   * Lưu điểm và nhận xét cho hàng loạt học sinh trong bài kiểm tra
   * PUT /api/assignments/{id}/batch-grade
   * @param {number} assignmentId
   * @param {Array<{ studentId: number, score?: number, teacherFeedback?: string, status?: string }>} grades
   */
  async batchGrade(assignmentId, grades) {
    const res = await this.api.put(`/${assignmentId}/batch-grade`, { grades });
    return res.data;
  }

  /**
   * Lấy chi tiết bài làm của một học sinh trong bài kiểm tra (bao gồm câu hỏi, đáp án, điểm số và nhận xét)
   * GET /api/assignments/{id}/submission/{studentId}
   * @param {number} assignmentId
   * @param {number} studentId
   */
  async getStudentSubmission(assignmentId, studentId) {
    const res = await this.api.get(`/${assignmentId}/submission/${studentId}`);
    return res.data;
  }
}

export default new AssignmentService();
