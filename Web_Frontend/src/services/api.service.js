import axios from "axios";

const commonConfig = {
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
};

export default (baseURL) => {
  const api = axios.create({
    baseURL,
    ...commonConfig,
  });

  api.interceptors.request.use((config) => {
    const accessToken = localStorage.getItem("toanTuDuy.accessToken");
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  });

  return api;
};
