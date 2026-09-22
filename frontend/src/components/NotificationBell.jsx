import { useEffect, useRef, useState } from "react";
import {
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from "../api/notifications";

function timeAgo(dateString) {
  const diffMs = Date.now() - new Date(dateString).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export default function NotificationBell() {
  const [notifications, setNotifications] = useState([]);
  const [open, setOpen] = useState(false);
  const containerRef = useRef(null);

  const load = () => {
    getNotifications()
      .then(setNotifications)
      .catch(() => {});
  };

  useEffect(() => {
    load();
    const interval = setInterval(load, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    function handleClickOutside(event) {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  const handleMarkAllRead = async () => {
    await markAllNotificationsRead();
    load();
  };

  const handleItemClick = async (notification) => {
    if (!notification.isRead) {
      await markNotificationRead(notification.id);
      load();
    }
  };

  return (
    <div className="notification-bell" ref={containerRef}>
      <button className="icon-button" onClick={() => setOpen((o) => !o)} aria-label="Notifications">
        <span aria-hidden="true">&#128276;</span>
        {unreadCount > 0 && <span className="badge-count">{unreadCount}</span>}
      </button>

      {open && (
        <div className="notification-dropdown">
          <div className="notification-dropdown-header">
            <span>Notifications</span>
            {unreadCount > 0 && (
              <button className="link-button" onClick={handleMarkAllRead}>
                Mark all read
              </button>
            )}
          </div>
          <div className="notification-list">
            {notifications.length === 0 && <p className="empty-state">No notifications yet.</p>}
            {notifications.map((n) => (
              <button
                key={n.id}
                className={`notification-item ${n.isRead ? "" : "unread"}`}
                onClick={() => handleItemClick(n)}
              >
                <strong>{n.title}</strong>
                <span>{n.message}</span>
                <small>{timeAgo(n.createdAt)}</small>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
