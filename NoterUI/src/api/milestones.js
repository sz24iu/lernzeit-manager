import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.DEV ? "https://localhost:7291/api" : "/api"
});

export async function getMilestones(studyGoalId) {
  const res = await api.get(`/milestone/${studyGoalId}`);
  return res.data;
}