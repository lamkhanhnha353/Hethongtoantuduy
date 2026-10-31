import createApiClient from "./api.service";
import {
  clearAuthSession,
  getAccessToken,
  getRole,
  saveAuthSession,
} from "./auth-storage";

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
    const response = (await this.api.post("/login", data)).data;
    const accessToken = response.accessToken ?? response.AccessToken;
    const role = response.role ?? response.Role;

    if (!accessToken || typeof role !== "string") {
      throw new Error("Phản hồi đăng nhập không hợp lệ.");
    }

    if (!["admin", "teacher"].includes(role.toLowerCase())) {
      clearAuthSession();
      throw new Error("Chỉ tài khoản Admin hoặc Teacher được phép đăng nhập.");
    }

    saveAuthSession(accessToken, role);
    return { ...response, accessToken, role };
  }

  getAccessToken() {
    return getAccessToken();
  }

  getRole() {
    return getRole();
  }

  isAuthenticated() {
    const role = getRole()?.toLowerCase();
    return Boolean(getAccessToken() && ["admin", "teacher"].includes(role));
  }

  logout() {
    clearAuthSession();
  }
}

export default new AuthService();
