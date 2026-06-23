import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.DEV ? "https://localhost:7291/api" : "/api"
});

export function setAccessToken(token) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`;
    return;
  }

  delete api.defaults.headers.common.Authorization;
}

export default api;
