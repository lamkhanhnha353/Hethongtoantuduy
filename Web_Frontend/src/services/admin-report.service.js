import createApiClient from "./api.service";

class AdminReportService {
  constructor(baseUrl = "/api/admin/reports") {
    this.api = createApiClient(baseUrl);
  }

  async getRevenue(months = 6) {
    return (await this.api.get("/revenue", { params: { months } })).data;
  }

  async getStudentGrowth(months = 6) {
    return (await this.api.get("/student-growth", { params: { months } })).data;
  }

  async getAttendance(days = 90, classId = null) {
    const params = { days };
    if (classId) params.classId = classId;
    return (await this.api.get("/attendance", { params })).data;
  }
}

export default new AdminReportService();
