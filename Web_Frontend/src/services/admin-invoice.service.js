import createApiClient from "./api.service";

class AdminInvoiceService {
  constructor(baseUrl = "/api/admin/invoices") {
    this.api = createApiClient(baseUrl);
  }

  async getAll() {
    return (await this.api.get("/")).data;
  }
}

export default new AdminInvoiceService();
