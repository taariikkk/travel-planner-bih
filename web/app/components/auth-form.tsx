"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { apiFetch, AuthResponse, getErrorMessage } from "../lib/api";
import { text } from "../lib/i18n";
import { useAuth } from "./auth-provider";
import { useLanguage } from "./language-provider";

type AuthFormProps = { mode: "login" | "register" };

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const { establishSession } = useAuth();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRegister = mode === "register";
  const t = text[useLanguage()].auth;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      const response = await apiFetch(`/api/auth/${mode}`, { method: "POST", body: JSON.stringify(isRegister ? { email, password, displayName } : { email, password }) });
      if (!response.ok) throw new Error(await getErrorMessage(response, t.loginFailed));
      const result = (await response.json()) as AuthResponse;
      establishSession(result.accessToken, result.user);
      router.replace("/recommendations");
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : t.unexpectedError);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f6f6] px-5 py-10 text-[#10231e]">
      <section className="w-full max-w-md rounded-3xl border border-[#dce5e2] bg-white p-8 shadow-xl shadow-[#10231e]/5 sm:p-10">
        <h1 className="mt-10 text-3xl font-bold tracking-tight">{isRegister ? t.registerTitle : t.loginTitle}</h1>
        <p className="mt-3 leading-6 text-[#607080]">{isRegister ? t.registerDescription : t.loginDescription}</p>
        <form className="mt-8 space-y-5" onSubmit={submit}>
          {isRegister ? <label className="block text-sm font-semibold">{t.displayName}<input required value={displayName} onChange={(event) => setDisplayName(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label> : null}
          <label className="block text-sm font-semibold">Email<input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label>
          <label className="block text-sm font-semibold">{t.password}<input required minLength={isRegister ? 8 : undefined} type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label>
          {error ? <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p> : null}
          <button disabled={isSubmitting} className="w-full rounded-xl bg-[#075b3a] px-5 py-3.5 font-semibold text-white disabled:opacity-60">{isSubmitting ? t.submitting : isRegister ? t.register : t.login}</button>
        </form>
        <p className="mt-7 text-sm text-[#607080]">{isRegister ? t.hasAccount : t.noAccount} <Link className="font-bold text-[#2563d9]" href={isRegister ? "/login" : "/register"}>{isRegister ? t.login : t.register}</Link></p>
      </section>
    </main>
  );
}
