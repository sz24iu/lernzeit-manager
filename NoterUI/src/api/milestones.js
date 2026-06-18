import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7291/api"
});

export async function getMilestones(studyGoalId) {
  const res = await api.get(`/milestone/${studyGoalId}`);
  return res.data;
}