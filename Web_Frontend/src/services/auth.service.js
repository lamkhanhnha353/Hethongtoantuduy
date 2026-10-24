import createApiClient from "./api.service";

class AuthService {
  constructor(baseUrl = "/api/auth") {
    this.api = createApiClient(baseUrl);
  }

  // Đăng ký tài khoản hệ thống
  async register(data) {
    return (await this.api.post("/register", data)).data;
  }

  // Đăng nhập vào hệ thống
  async login(data) {
    return (await this.api.post("/login", data)).data;
  }
}

export default new AuthService();
