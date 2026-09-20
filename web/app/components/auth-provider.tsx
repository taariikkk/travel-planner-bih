"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { apiFetch, type UserProfile } from "../lib/api";

const TOKEN_KEY = "travelPlanner.accessToken";

type AuthContextValue = {
  status: "checking" | "authenticated" | "guest";
  token: string | null;
  user: UserProfile | null;
  profileError: boolean;
  establishSession: (token: string, user: UserProfile) => void;
  updateUser: (user: UserProfile) => void;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthContextValue["status"]>("checking");
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<UserProfile | null>(null);
  const [profileError, setProfileError] = useState(false);

  useEffect(() => {
    const storedToken = sessionStorage.getItem(TOKEN_KEY);
    if (!storedToken) {
      queueMicrotask(() => setStatus("guest"));
      return;
    }

    const controller = new AbortController();
    apiFetch("/api/users/me", { signal: controller.signal }, storedToken)
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) {
          sessionStorage.removeItem(TOKEN_KEY);
          setToken(null);
          setUser(null);
          setStatus("guest");
          return;
        }
        if (!response.ok) throw new Error("Profile unavailable");
        setToken(storedToken);
        setUser((await response.json()) as UserProfile);
        setProfileError(false);
        setStatus("authenticated");
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setToken(storedToken);
        setProfileError(true);
        setStatus("authenticated");
      });

    return () => controller.abort();
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    status,
    token,
    user,
    profileError,
    establishSession(nextToken, nextUser) {
      sessionStorage.setItem(TOKEN_KEY, nextToken);
      setToken(nextToken);
      setUser(nextUser);
      setProfileError(false);
      setStatus("authenticated");
    },
    updateUser(nextUser) {
      setUser(nextUser);
      setProfileError(false);
    },
    signOut() {
      sessionStorage.removeItem(TOKEN_KEY);
      setToken(null);
      setUser(null);
      setProfileError(false);
      setStatus("guest");
    },
  }), [profileError, status, token, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
