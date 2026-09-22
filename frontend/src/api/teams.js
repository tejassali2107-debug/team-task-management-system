import apiClient from "./client";

export const getTeams = () => apiClient.get("/teams").then((res) => res.data);

export const getTeam = (id) => apiClient.get(`/teams/${id}`).then((res) => res.data);

export const createTeam = (payload) => apiClient.post("/teams", payload).then((res) => res.data);

export const updateTeam = (id, payload) =>
  apiClient.put(`/teams/${id}`, payload).then((res) => res.data);

export const deleteTeam = (id) => apiClient.delete(`/teams/${id}`);

export const addTeamMember = (teamId, userId) =>
  apiClient.post(`/teams/${teamId}/members`, { userId }).then((res) => res.data);

export const removeTeamMember = (teamId, userId) =>
  apiClient.delete(`/teams/${teamId}/members/${userId}`).then((res) => res.data);
