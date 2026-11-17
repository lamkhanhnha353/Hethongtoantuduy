import { reactive, computed, readonly } from "vue";
import profileService from "./profile.service";
import authService from "./auth.service";

const state = reactive({
  profile: null,
  isLoading: false,
  error: null,
  isInitialized: false,
});

function getDefaultProfile(role) {
  const normalizedRole = (role || "").toLowerCase();
  const isAdmin = normalizedRole === "admin";
  return {
    accountId: 1,
    fullName: isAdmin ? "Quản trị viên" : "Giáo viên Toán Tư Duy",
    role: isAdmin ? "Admin" : "Teacher",
    phone: "0901234567",
    email: isAdmin ? "admin@toantuduy.edu.vn" : "giaovien@toantuduy.edu.vn",
    emailConfirm: true,
    avatar: null,
    hasPin: false,
  };
}

export function useUserState() {
  async function fetchProfile(force = false) {
    if (state.isInitialized && !force && state.profile) {
      return state.profile;
    }

    if (!authService.isAuthenticated()) {
      state.profile = null;
      state.isInitialized = true;
      return null;
    }

    state.isLoading = true;
    state.error = null;

    try {
      const data = await profileService.get();
      state.profile = {
        ...data,
        fullName: data.fullName || data.FullName || (authService.getRole() === "Admin" ? "Quản trị viên" : "Giáo viên"),
        role: data.role || data.Role || authService.getRole() || "Admin",
        phone: data.phone || data.Phone || "",
        email: data.email || data.Email || "",
        avatar: data.avatar || data.Avatar || null,
        hasPin: Boolean(data.hasPin ?? data.HasPin),
        emailConfirm: Boolean(data.emailConfirm ?? data.EmailConfirm),
      };
    } catch (err) {
      // Backend may return 400 for Admin if not yet mapped or network issue
      console.warn("Lỗi khi tải thông tin hồ sơ cá nhân:", err);
      if (!state.profile) {
        state.profile = getDefaultProfile(authService.getRole());
      }
    } finally {
      state.isLoading = false;
      state.isInitialized = true;
    }

    return state.profile;
  }

  function setProfile(newProfile) {
    state.profile = {
      ...(state.profile || {}),
      ...newProfile,
    };
  }

  async function updateProfile(payload) {
    state.isLoading = true;
    state.error = null;
    try {
      const updated = await profileService.update(payload);
      if (updated && (updated.fullName || updated.FullName)) {
        setProfile({
          fullName: updated.fullName || updated.FullName,
          email: updated.email ?? updated.Email ?? state.profile?.email,
          avatar: updated.avatar ?? updated.Avatar ?? state.profile?.avatar,
        });
      } else {
        setProfile({ fullName: payload.fullName });
      }
      return state.profile;
    } catch (err) {
      // If error occurs, update locally for UX responsiveness if appropriate
      if (payload.fullName) {
        setProfile({ fullName: payload.fullName });
      }
      throw err;
    } finally {
      state.isLoading = false;
    }
  }

  async function uploadAvatar(file) {
    const formData = new FormData();
    formData.append("file", file);
    const res = await profileService.uploadAvatar(formData);
    const newAvatarUrl = res?.avatar || res?.Avatar || (typeof res === "string" ? res : null);
    if (newAvatarUrl) {
      setProfile({ avatar: newAvatarUrl });
    }
    return res;
  }

  async function removeAvatar() {
    await profileService.deleteAvatar();
    setProfile({ avatar: null });
  }

  const profile = computed(() => state.profile || getDefaultProfile(authService.getRole()));
  const initials = computed(() => {
    const name = profile.value.fullName || "Admin";
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  });

  return {
    profile,
    initials,
    isLoading: computed(() => state.isLoading),
    error: computed(() => state.error),
    fetchProfile,
    setProfile,
    updateProfile,
    uploadAvatar,
    removeAvatar,
  };
}
