import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import NotificationBell from "./NotificationBell";
import { IconCheckSquare, IconGrid, IconLogout, IconMenu, IconSparkle, IconUser, IconUsers, IconClose } from "./icons";

const NAV_ITEMS = [
  { to: "/dashboard", label: "Dashboard", icon: IconGrid, roles: ["Admin", "Manager", "User"] },
  { to: "/tasks", label: "Tasks", icon: IconCheckSquare, roles: ["Admin", "Manager", "User"] },
  { to: "/teams", label: "Teams", icon: IconUsers, roles: ["Admin", "Manager", "User"] },
  { to: "/users", label: "Users", icon: IconUser, roles: ["Admin"] },
];

export default function Layout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  const items = NAV_ITEMS.filter((item) => item.roles.includes(user.role));
  const initials = user.fullName
    .split(" ")
    .map((p) => p[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

  return (
    <div className="app-shell">
      {mobileOpen && <div className="sidebar-backdrop" onClick={() => setMobileOpen(false)} />}

      <aside className={`sidebar ${mobileOpen ? "sidebar-open" : ""}`}>
        <div className="sidebar-brand">
          <span className="brand-mark">
            <IconSparkle width={18} height={18} />
          </span>
          <span className="brand-name">TaskFlow</span>
          <button className="sidebar-close" onClick={() => setMobileOpen(false)} aria-label="Close menu">
            <IconClose width={18} height={18} />
          </button>
        </div>

        <nav className="sidebar-nav">
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => `sidebar-link ${isActive ? "active" : ""}`}
              >
                <Icon width={18} height={18} />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <span className="avatar">{initials}</span>
            <div className="sidebar-user-info">
              <span className="user-name">{user.fullName}</span>
              <span className={`role-pill role-${user.role}`}>{user.role}</span>
            </div>
          </div>
          <button className="sidebar-logout" onClick={logout}>
            <IconLogout width={17} height={17} />
            <span>Log out</span>
          </button>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <button className="hamburger" onClick={() => setMobileOpen(true)} aria-label="Open menu">
            <IconMenu width={22} height={22} />
          </button>
          <span className="topbar-title">TaskFlow</span>
          <div className="topbar-spacer" />
          <NotificationBell />
        </header>
        <main className="app-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
