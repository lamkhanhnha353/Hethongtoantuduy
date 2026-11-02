import createApiClient from "./api.service";

class AdminAccountService {
  constructor(baseUrl = "/api/admin/accounts") {
    this.api = createApiClient(baseUrl);
  }

  async getAll(keyword = "") {
    return (await this.api.get("/", { params: { keyword: keyword || undefined } })).data;
  }

  async updateStatus(id, isActive) {
    return (await this.api.put(`/${id}/status`, { isActive })).data;
  }

  async resetPassword(id) {
    return (await this.api.post(`/${id}/reset-password`)).data;
  }
}

export default new AdminAccountService();
