import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.DEV ? "https://localhost:7291/api" : "/api"
});

api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      localStorage.removeItem("noter.jwt");
      delete api.defaults.headers.common.Authorization;
    }

    return Promise.reject(error);
  }
);

export function setAccessToken(token) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`;
    return;
  }

  delete api.defaults.headers.common.Authorization;
}

export default api;
