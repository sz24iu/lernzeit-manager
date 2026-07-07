import api from "./http";

export async function getMilestones(studyGoalId) {
  const res = await api.get(`/milestone/${studyGoalId}`);
  return res.data;
}

export async function createMilestone(payload) {
  const res = await api.post("/milestone", payload);
  return res.data;
}

export async function updateMilestoneStatus(payload) {
  const res = await api.patch("/milestone", payload);
  return res.data;
}

export async function trackMilestoneStudyTime(milestoneId, trackedMinutes) {
  const res = await api.post(`/milestone/${milestoneId}/track`, {
    trackedMinutes
  });
  return res.data;
}