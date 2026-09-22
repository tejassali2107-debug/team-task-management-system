import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import NotificationBell from "./NotificationBell";

const NAV_ITEMS = [
  { to: "/dashboard", label: "Dashboard", roles: ["Admin", "Manager", "User"] },
  { to: "/tasks", label: "Tasks", roles: ["Admin", "Manager", "User"] },
  { to: "/teams", label: "Teams", roles: ["Admin", "Manager", "User"] },
  { to: "/users", label: "Users", roles: ["Admin"] },
];

export default function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header-left">
          <span className="app-logo">TaskFlow</span>
          <nav className="app-nav">
            {NAV_ITEMS.filter((item) => item.roles.includes(user.role)).map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) => `nav-link ${isActive ? "active" : ""}`}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>
        <div className="app-header-right">
          <NotificationBell />
          <div className="user-menu">
            <span className="user-name">{user.fullName}</span>
            <span className={`role-pill role-${user.role}`}>{user.role}</span>
            <button className="link-button" onClick={logout}>
              Log out
            </button>
          </div>
        </div>
      </header>
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}
