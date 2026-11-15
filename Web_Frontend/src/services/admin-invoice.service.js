import createApiClient from "./api.service";

class AdminInvoiceService {
  constructor(baseUrl = "/api/admin/invoices") {
    this.api = createApiClient(baseUrl);
  }

  async getAll() {
    return (await this.api.get("/")).data;
  }

  async getPaged({
    page = 1,
    pageSize = 20,
    keyword = "",
    status = "",
  } = {}) {
    return (
      await this.api.get("/paged", {
        params: {
          page,
          pageSize,
          keyword: keyword?.trim() || undefined,
          status: status && status !== "ALL" ? status : undefined,
        },
      })
    ).data;
  }
}

export default new AdminInvoiceService();
