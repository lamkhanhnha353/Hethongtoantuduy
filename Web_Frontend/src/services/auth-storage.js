const ACCESS_TOKEN_KEY = "toanTuDuy.accessToken";
const ROLE_KEY = "toanTuDuy.role";

export function getAccessToken() {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRole() {
  return sessionStorage.getItem(ROLE_KEY);
}

export function saveAuthSession(accessToken, role) {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  sessionStorage.setItem(ROLE_KEY, role);
}

export function clearAuthSession() {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(ROLE_KEY);
}
