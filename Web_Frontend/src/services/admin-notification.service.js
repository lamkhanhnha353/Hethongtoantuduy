import createApiClient from "./api.service";

class AdminNotificationService {
  constructor(baseUrl = "/api/admin/notifications") {
    this.api = createApiClient(baseUrl);
  }

  async getHistory(params = {}) {
    return (await this.api.get("", { params })).data;
  }

  async create(notification) {
    return (await this.api.post("", notification)).data;
  }
}

export default new AdminNotificationService();
