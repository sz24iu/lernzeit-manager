import api from "./http";

export async function getStudyGoals() {
  const res = await api.get("/studygoal");
  return res.data;
}

export async function createStudyGoal(payload) {
  const res = await api.post("/studygoal", payload);
  return res.data;
}