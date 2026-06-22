import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.DEV ? "https://localhost:7291/api" : "/api"
});

export async function getStudyGoals() {
  const res = await api.get("/studygoal");
  return res.data;
}

export async function createStudyGoal(payload) {
  const res = await api.post("/studygoal", payload);
  return res.data;
}