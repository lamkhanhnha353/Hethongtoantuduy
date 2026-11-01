import axios from "axios";
import { getAccessToken } from "./auth-storage";

const BACKEND_URL = "http://localhost:5114";

const commonConfig = {
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
};

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

  return api;
};
