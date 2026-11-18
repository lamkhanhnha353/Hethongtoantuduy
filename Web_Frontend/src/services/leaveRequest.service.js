import createApiClient from "./api.service";

class LeaveRequestService {
  constructor(baseUrl = "/api/leaverequests") {
    this.api = createApiClient(baseUrl);
  }

  async getPending() {
    return (await this.api.get("/pending")).data;
  }

  async getPendingPaged({ page = 1, pageSize = 20 } = {}) {
    return (await this.api.get("/pending/paged", { params: { page, pageSize } })).data;
  }

  async process(id, isApproved) {
    return (await this.api.put(`/${id}/process`, null, { params: { isApproved } })).data;
  }
}

export default new LeaveRequestService();
