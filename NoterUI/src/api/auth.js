import api from "./http";

export async function login(payload) {
  const res = await api.post("/authentication/login", payload);
  return res.data;
}
