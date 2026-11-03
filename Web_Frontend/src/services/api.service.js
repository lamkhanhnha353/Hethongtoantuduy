import axios from "axios";
import { clearAuthSession, getAccessToken } from "./auth-storage";

const BACKEND_URL = "http://localhost:5114";

const commonConfig = {
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
};

let redirectingAfterSessionExpiry = false;

export default (baseURL = "") => {
  let fullBaseURL = baseURL;
  if (!/^https?:\/\//i.test(baseURL)) {
    const base = BACKEND_URL.replace(/\/+$/, "");
    const path = baseURL ? `/${baseURL.replace(/^\/+/, "")}` : "";
    fullBaseURL = `${base}${path}`;
  }

  const api = axios.create({
    baseURL: fullBaseURL,
    ...commonConfig,
  });

  api.interceptors.request.use((config) => {
    const accessToken = getAccessToken();
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  });

  api.interceptors.response.use(
    (response) => response,
    (error) => {
      const requestUrl = error.config?.url ?? "";
      const responseBody = error.response?.data;
      const isLoginRequest = requestUrl.includes("/login");

      if (
        error.response?.status === 401 &&
        !responseBody &&
        !isLoginRequest &&
        getAccessToken()
      ) {
        clearAuthSession();
        if (!redirectingAfterSessionExpiry && window.location.pathname !== "/login") {
          redirectingAfterSessionExpiry = true;
          window.location.replace("/login");
        }
      }

      return Promise.reject(error);
    },
  );

  return api;
};
