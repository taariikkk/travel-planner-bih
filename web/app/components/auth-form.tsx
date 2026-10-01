"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";
import { apiFetch, AuthResponse, getErrorMessage } from "../lib/api";
import { getAuthModeHref, getSafeReturnTo } from "../lib/auth-return-to";
import { text } from "../lib/i18n";
import { useAuth } from "./auth-provider";
import { useLanguage } from "./language-provider";
import styles from "../login/login.module.css";

type AuthFormProps = { mode: "login" | "register" };

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { establishSession } = useAuth();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRegister = mode === "register";
  const t = text[useLanguage()].auth;
  const returnTo = getSafeReturnTo(searchParams.get("returnTo"));
  const otherModeHref = getAuthModeHref(isRegister ? "/login" : "/register", returnTo);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      const response = await apiFetch(`/api/auth/${mode}`, { method: "POST", body: JSON.stringify(isRegister ? { email, password, displayName } : { email, password }) });
      if (!response.ok) throw new Error(await getErrorMessage(response, t.loginFailed));
      const result = (await response.json()) as AuthResponse;
      establishSession(result.accessToken, result.user);
      router.replace(returnTo);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : t.unexpectedError);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className={styles.authMain}>
      <section className={styles.authSection}>
        <h1>{isRegister ? t.registerTitle : t.loginTitle}</h1>
        <p>{isRegister ? t.registerDescription : t.loginDescription}</p>
        <form onSubmit={submit}>
          {isRegister ? <label>{t.displayName}<input required value={displayName} onChange={(event) => setDisplayName(event.target.value)} /></label> : null}
          <label>Email<input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} /></label>
          <label>{t.password}<input required minLength={isRegister ? 8 : undefined} type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
          {error ? <p>{error}</p> : null}
          <button disabled={isSubmitting}>{isSubmitting ? t.submitting : isRegister ? t.register : t.login}</button>
        </form>
        <p>{isRegister ? t.hasAccount : t.noAccount} <Link href={otherModeHref}>{isRegister ? t.login : t.register}</Link></p>
      </section>
    </main>
  );
}
