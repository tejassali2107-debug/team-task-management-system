import apiClient from "./client";

export const getNotifications = () => apiClient.get("/notifications").then((res) => res.data);

export const markNotificationRead = (id) =>
  apiClient.patch(`/notifications/${id}/read`).then((res) => res.data);

export const markAllNotificationsRead = () =>
  apiClient.patch("/notifications/read-all").then((res) => res.data);
