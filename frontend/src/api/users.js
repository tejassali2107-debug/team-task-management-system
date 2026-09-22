import apiClient from "./client";

export const getUsers = () => apiClient.get("/users").then((res) => res.data);

export const getUser = (id) => apiClient.get(`/users/${id}`).then((res) => res.data);

export const createUser = (payload) => apiClient.post("/users", payload).then((res) => res.data);

export const updateUser = (id, payload) =>
  apiClient.put(`/users/${id}`, payload).then((res) => res.data);

export const deleteUser = (id) => apiClient.delete(`/users/${id}`);
