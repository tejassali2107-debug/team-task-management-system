import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import * as authApi from "../api/auth";
import { registerUnauthorizedHandler } from "../api/client";

const AuthContext = createContext(null);

function readStoredUser() {
  try {
    const raw = localStorage.getItem("user");
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [ready, setReady] = useState(false);
  const navigate = useNavigate();

  const clearSession = useCallback(() => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    setUser(null);
  }, []);

  const logout = useCallback(() => {
    clearSession();
    navigate("/login");
  }, [clearSession, navigate]);

  useEffect(() => {
    registerUnauthorizedHandler(() => {
      const hadUser = !!localStorage.getItem("token");
      clearSession();
      if (hadUser) {
        setSessionExpired(true);
      }
      navigate("/login");
    });
  }, [clearSession, navigate]);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      setReady(true);
      return;
    }

    authApi
      .getMe()
      .then((freshUser) => {
        setUser(freshUser);
        localStorage.setItem("user", JSON.stringify(freshUser));
      })
      .catch(() => clearSession())
      .finally(() => setReady(true));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const login = useCallback(async (email, password) => {
    const data = await authApi.login(email, password);
    localStorage.setItem("token", data.token);
    localStorage.setItem("user", JSON.stringify(data.user));
    setUser(data.user);
    setSessionExpired(false);
    return data.user;
  }, []);

  const register = useCallback(async (fullName, email, password) => {
    const data = await authApi.register(fullName, email, password);
    localStorage.setItem("token", data.token);
    localStorage.setItem("user", JSON.stringify(data.user));
    setUser(data.user);
    setSessionExpired(false);
    return data.user;
  }, []);

  const value = useMemo(
    () => ({ user, login, register, logout, ready, sessionExpired, clearSessionExpired: () => setSessionExpired(false) }),
    [user, login, register, logout, ready, sessionExpired]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
