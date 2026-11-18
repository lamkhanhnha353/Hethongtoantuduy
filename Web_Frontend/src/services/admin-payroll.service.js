import createApiClient from "./api.service";

class AdminPayrollService {
    constructor(baseUrl = "/api/payrolls") {
        this.api = createApiClient(baseUrl);
    }

    async getAll({ month, year } = {}) {
        return (
            await this.api.get("/admin/summary", {
                params: { month, year },
            })
        ).data;
    }

    async getPaged({
        month,
        year,
        page = 1,
        pageSize = 20,
        keyword = "",
        emailStatus = "ALL",
    } = {}) {
        return (
            await this.api.get("/admin/summary/paged", {
                params: {
                    month,
                    year,
                    page,
                    pageSize,
                    keyword: keyword?.trim() || undefined,
                    emailStatus: emailStatus && emailStatus !== "ALL" ? emailStatus : undefined,
                },
            })
        ).data;
    }

    async getDetail({ teacherId, month, year } = {}) {
        return (
            await this.api.get("/detail", {
                params: { teacherId, month, year },
            })
        ).data;
    }

    async updateBaseRate(teacherId, baseRatePerSession) {
        return (
            await this.api.put(`/teachers/${teacherId}/base-rate`, {
                baseRatePerSession,
            })
        ).data;
    }

    async createAdjustment(payload) {
        return (await this.api.post("/adjustments", payload)).data;
    }

    async deleteAdjustment(adjustmentId) {
        return (await this.api.delete(`/adjustments/${adjustmentId}`)).data;
    }

    async previewEmail(payrollId) {
        return (
            await this.api.get(`/${payrollId}/preview-email`, {
                responseType: "text",
            })
        ).data;
    }

    async sendSingleEmail(payrollId) {
        return (await this.api.post(`/${payrollId}/send-email`)).data;
    }

    async sendBatchEmails({ month, year } = {}) {
        return (
            await this.api.post("/send-batch-emails", null, {
                params: { month, year },
            })
        ).data;
    }
}

export default new AdminPayrollService();