import axios from "axios";

// Đọc địa chỉ backend (mặc định http://localhost:5114)
const BACKEND_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5114";

const commonConfig = {
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
};

export default (baseURL = "") => {
  let fullBaseURL = baseURL;
  if (!baseURL.startsWith("http://") && !baseURL.startsWith("https://")) {
    const base = BACKEND_URL.replace(/\/+$/, "");
    const path = baseURL ? (baseURL.startsWith("/") ? baseURL : `/${baseURL}`) : "";
    fullBaseURL = `${base}${path}`;
  }

  return axios.create({
    baseURL: fullBaseURL,
    ...commonConfig,
  });
};
