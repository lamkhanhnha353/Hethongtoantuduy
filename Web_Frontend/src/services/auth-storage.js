const ACCESS_TOKEN_KEY = "toanTuDuy.accessToken";
const ROLE_KEY = "toanTuDuy.role";
const PROFILE_ID_KEY = "toanTuDuy.profileId";

export function getAccessToken() {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRole() {
  return sessionStorage.getItem(ROLE_KEY);
}

export function getProfileId() {
  const id = sessionStorage.getItem(PROFILE_ID_KEY);
  return id ? parseInt(id, 10) : null;
}

export function saveAuthSession(accessToken, role, profileId = null) {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  sessionStorage.setItem(ROLE_KEY, role);
  if (profileId) {
    sessionStorage.setItem(PROFILE_ID_KEY, String(profileId));
  }
}

export function clearAuthSession() {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(ROLE_KEY);
  sessionStorage.removeItem(PROFILE_ID_KEY);
}
