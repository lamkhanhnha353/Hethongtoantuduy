import createApiClient from "./api.service";

class ProfileService {
  constructor(baseUrl = "/api/profile") {
    this.api = createApiClient(baseUrl);
  }

  // Lấy thông tin cá nhân của người dùng hiện tại
  async get() {
    return (await this.api.get("/")).data;
  }

  // Cập nhật họ tên người dùng
  async update(data) {
    return (await this.api.put("/", data)).data;
  }

  // Thiết lập mã PIN lần đầu hoặc đổi mã PIN
  async updatePin(data) {
    return (await this.api.put("/pin", data)).data;
  }

  // Xác thực mã PIN (endpoint chưa được cấu hình, đặt chỗ tạm)
  async verifyPin(data) {
    return (await this.api.post("/pin/verify", data)).data;
  }

  // Cập nhật ảnh đại diện (Avatar) — gửi FormData
  async uploadAvatar(formData) {
    return (
      await this.api.post("/avatar", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      })
    ).data;
  }

  // Xóa ảnh đại diện
  async deleteAvatar() {
    return (await this.api.delete("/avatar")).data;
  }
}

export default new ProfileService();
