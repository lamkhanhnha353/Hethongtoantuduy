import createApiClient from "./api.service";
import {
  clearAuthSession,
  getAccessToken,
  getRole,
  getProfileId,
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

  // Đổi mật khẩu
  async changePassword(data) {
    return (await this.api.post("/change-password", data)).data;
  }

  // Đăng nhập vào hệ thống
  async login(data) {
    const response = (await this.api.post("/login", data)).data;
    const accessToken = response.accessToken ?? response.AccessToken;
    const role = response.role ?? response.Role;
    const profileId = response.profileId ?? response.ProfileId;

    if (!accessToken || typeof role !== "string") {
      throw new Error("Phản hồi đăng nhập không hợp lệ.");
    }

    if (!["admin", "teacher"].includes(role.toLowerCase())) {
      clearAuthSession();
      throw new Error(
        role.toLowerCase() === "parent"
          ? "Tài khoản Phụ huynh chỉ đăng nhập trên ứng dụng."
          : "Chỉ tài khoản Admin hoặc Teacher được phép đăng nhập trên web.",
      );
    }

    saveAuthSession(accessToken, role, profileId);
    return { ...response, accessToken, role, profileId };
  }

  async googleLogin(idToken) {
    const response = (await this.api.post("/google-login", { idToken })).data;
    const accessToken = response.accessToken ?? response.AccessToken;
    const refreshToken = response.refreshToken ?? response.RefreshToken;
    const role = response.role ?? response.Role;
    const profileId = response.profileId ?? response.ProfileId;
    const email = response.email ?? response.Email;
    const fullName = response.fullName ?? response.FullName;

    if (!accessToken || typeof role !== "string") {
      throw new Error("Phản hồi đăng nhập Google không hợp lệ.");
    }

    if (!["admin", "teacher"].includes(role.toLowerCase())) {
      clearAuthSession();
      throw new Error(
        role.toLowerCase() === "parent"
          ? "Tài khoản Phụ huynh chỉ đăng nhập trên ứng dụng."
          : "Chỉ tài khoản Admin hoặc Teacher được phép đăng nhập trên web.",
      );
    }

    const userInfo = { email, fullName, role, profileId };
    saveAuthSession(accessToken, role, profileId, userInfo);
    return { ...response, accessToken, refreshToken, role, profileId, email, fullName };
  }

  getAccessToken() {
    return getAccessToken();
  }

  getRole() {
    return getRole();
  }

  getProfileId() {
    return getProfileId();
  }

  isAuthenticated() {
    const role = getRole()?.toLowerCase();
    const hasAccessToken = Boolean(getAccessToken());
    const hasWebRole = ["admin", "teacher"].includes(role);

    if (hasAccessToken && !hasWebRole) {
      clearAuthSession();
    }

    return hasAccessToken && hasWebRole;
  }

  logout() {
    clearAuthSession();
  }
}

export default new AuthService();
