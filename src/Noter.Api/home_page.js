import axios from "axios";

const api = axios.create({
    baseURL: "https://localhost:5001/api"
});

export const getStudyGoals = async () => {
    const res = await api.get("/studygoals");
    return res.data;
};