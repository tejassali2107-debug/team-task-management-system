import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { extractErrorMessage } from "../api/client";
import { IconLock, IconMail, IconSparkle } from "../components/icons";

const DEMO_ACCOUNTS = [
  { role: "Admin", email: "admin@taskflow.com", password: "Admin@123" },
  { role: "Manager", email: "manager@taskflow.com", password: "Manager@123" },
  { role: "User", email: "alice@taskflow.com", password: "User@123" },
];

export default function LoginPage() {
  const { login, sessionExpired, clearSessionExpired } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const from = location.state?.from?.pathname || "/dashboard";

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await login(email, password);
      navigate(from, { replace: true });
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <div className="auth-brand">
          <span className="brand-mark">
            <IconSparkle width={18} height={18} />
          </span>
          <span className="brand-name">TaskFlow</span>
        </div>
        <h1>Welcome back</h1>
        <p className="auth-subtitle">Sign in to manage your team's tasks.</p>

        {sessionExpired && (
          <div className="alert alert-warning">
            Your session has expired. Please log in again.
            <button type="button" className="link-button" onClick={clearSessionExpired}>
              Dismiss
            </button>
          </div>
        )}
        {error && <div className="alert alert-error">{error}</div>}

        <label>
          Email
          <div className="input-with-icon">
            <IconMail width={16} height={16} />
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              placeholder="you@company.com"
            />
          </div>
        </label>
        <label>
          Password
          <div className="input-with-icon">
            <IconLock width={16} height={16} />
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              placeholder="••••••••"
            />
          </div>
        </label>

        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
          {loading ? "Signing in..." : "Sign in"}
        </button>

        <p className="auth-switch">
          Don't have an account? <Link to="/register">Register</Link>
        </p>

        <div className="demo-credentials">
          <p>Demo accounts (tap to fill)</p>
          <ul className="demo-account-list">
            {DEMO_ACCOUNTS.map((acc) => (
              <li key={acc.role}>
                <button
                  type="button"
                  className="demo-account-btn"
                  onClick={() => {
                    setEmail(acc.email);
                    setPassword(acc.password);
                  }}
                >
                  <span className={`role-pill role-${acc.role}`}>{acc.role}</span>
                  <span>{acc.email}</span>
                </button>
              </li>
            ))}
          </ul>
        </div>
      </form>
    </div>
  );
}
