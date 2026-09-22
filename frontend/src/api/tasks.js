import apiClient from "./client";

export const getTasks = (filter = {}) =>
  apiClient.get("/tasks", { params: filter }).then((res) => res.data);

export const getTask = (id) => apiClient.get(`/tasks/${id}`).then((res) => res.data);

export const createTask = (payload) => apiClient.post("/tasks", payload).then((res) => res.data);

export const updateTask = (id, payload) =>
  apiClient.put(`/tasks/${id}`, payload).then((res) => res.data);

export const updateTaskStatus = (id, status) =>
  apiClient.patch(`/tasks/${id}/status`, { status }).then((res) => res.data);

export const deleteTask = (id) => apiClient.delete(`/tasks/${id}`);

export const getComments = (taskId) =>
  apiClient.get(`/tasks/${taskId}/comments`).then((res) => res.data);

export const addComment = (taskId, content) =>
  apiClient.post(`/tasks/${taskId}/comments`, { content }).then((res) => res.data);
