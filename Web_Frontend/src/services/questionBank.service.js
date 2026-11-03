import createApiClient from "./api.service";

class QuestionBankService {
  constructor(baseUrl = "/api/question-banks") {
    this.api = createApiClient(baseUrl);
  }

  // ─── Question Banks (Ngân hàng câu hỏi) ──────────────────────────────────
  /**
   * Lấy danh sách ngân hàng câu hỏi (lọc theo courseId, keyword)
   * GET /api/question-banks
   */
  async getAll(courseId = null, keyword = null) {
    const params = {};
    if (courseId) params.courseId = courseId;
    if (keyword) params.keyword = keyword;
    const res = await this.api.get("", { params });
    return res.data;
  }

  /**
   * Lấy chi tiết ngân hàng câu hỏi kèm toàn bộ câu hỏi bên trong
   * GET /api/question-banks/{id}
   */
  async getById(id) {
    const res = await this.api.get(`/${id}`);
    return res.data;
  }

  /**
   * Tạo ngân hàng câu hỏi mới
   * POST /api/question-banks
   * @param {{ courseId: number, name: string, description?: string }} data
   */
  async create(data) {
    const res = await this.api.post("", data);
    return res.data;
  }

  /**
   * Cập nhật thông tin ngân hàng câu hỏi
   * PUT /api/question-banks/{id}
   * @param {number} id
   * @param {{ courseId: number, name: string, description?: string }} data
   */
  async update(id, data) {
    const res = await this.api.put(`/${id}`, data);
    return res.data;
  }

  /**
   * Xóa ngân hàng câu hỏi
   * DELETE /api/question-banks/{id}
   */
  async delete(id) {
    const res = await this.api.delete(`/${id}`);
    return res.data;
  }

  // ─── Questions (Câu hỏi) ────────────────────────────────────────────────
  /**
   * Lấy danh sách câu hỏi theo ngân hàng (lọc theo difficulty, type)
   * GET /api/question-banks/{bankId}/questions
   */
  async getQuestions(bankId, difficulty = null, type = null) {
    const params = {};
    if (difficulty) params.difficulty = difficulty;
    if (type) params.type = type;
    const res = await this.api.get(`/${bankId}/questions`, { params });
    return res.data;
  }

  /**
   * Lấy chi tiết một câu hỏi theo ID
   * GET /api/question-banks/questions/{id}
   */
  async getQuestionById(id) {
    const res = await this.api.get(`/questions/${id}`);
    return res.data;
  }

  /**
   * Thêm câu hỏi mới vào ngân hàng
   * POST /api/question-banks/questions
   * @param {{ bankId: number, type: string, content: string, imageUrl?: string, difficulty: string, answer: string }} data
   */
  async createQuestion(data) {
    const res = await this.api.post("/questions", data);
    return res.data;
  }

  /**
   * Cập nhật câu hỏi
   * PUT /api/question-banks/questions/{id}
   */
  async updateQuestion(id, data) {
    const res = await this.api.put(`/questions/${id}`, data);
    return res.data;
  }

  /**
   * Xóa câu hỏi khỏi ngân hàng
   * DELETE /api/question-banks/questions/{id}
   */
  async deleteQuestion(id) {
    const res = await this.api.delete(`/questions/${id}`);
    return res.data;
  }
}

export default new QuestionBankService();
