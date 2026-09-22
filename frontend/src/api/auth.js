import apiClient from "./client";

export const login = (email, password) =>
  apiClient.post("/auth/login", { email, password }).then((res) => res.data);

export const register = (fullName, email, password) =>
  apiClient.post("/auth/register", { fullName, email, password }).then((res) => res.data);

export const getMe = () => apiClient.get("/auth/me").then((res) => res.data);
