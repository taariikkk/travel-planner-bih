"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { apiFetch, AuthResponse, getErrorMessage } from "../lib/api";

type AuthFormProps = { mode: "login" | "register" };

export function AuthForm({ mode }: AuthFormProps) {
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isRegister = mode === "register";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);
    try {
      const response = await apiFetch(`/api/auth/${mode}`, { method: "POST", body: JSON.stringify(isRegister ? { email, password, displayName } : { email, password }) });
      if (!response.ok) throw new Error(await getErrorMessage(response, "Prijava nije uspjela."));
      const result = (await response.json()) as AuthResponse;
      sessionStorage.setItem("travelPlanner.accessToken", result.accessToken);
      router.replace("/recommendations");
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Došlo je do greške.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f6f6] px-5 py-10 text-[#10231e]">
      <section className="w-full max-w-md rounded-3xl border border-[#dce5e2] bg-white p-8 shadow-xl shadow-[#10231e]/5 sm:p-10">
        <Link className="flex items-center gap-2 text-base font-bold" href="/"><span className="grid h-8 w-8 place-items-center rounded-full bg-[#075b3a] text-white">⌁</span>Travel Planner BiH</Link>
        <h1 className="mt-10 text-3xl font-bold tracking-tight">{isRegister ? "Kreiraj svoj račun" : "Dobro došao nazad"}</h1>
        <p className="mt-3 leading-6 text-[#607080]">{isRegister ? "Sačuvaj svoja interesovanja i pronađi putovanje po svojoj mjeri." : "Prijavi se da nastaviš s planiranjem putovanja."}</p>
        <form className="mt-8 space-y-5" onSubmit={submit}>
          {isRegister ? <label className="block text-sm font-semibold">Ime za prikaz<input required value={displayName} onChange={(event) => setDisplayName(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label> : null}
          <label className="block text-sm font-semibold">Email<input required type="email" value={email} onChange={(event) => setEmail(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label>
          <label className="block text-sm font-semibold">Lozinka<input required minLength={isRegister ? 8 : undefined} type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="mt-2 w-full rounded-xl border border-[#dce5e2] px-4 py-3 outline-none focus:border-[#075b3a]" /></label>
          {error ? <p className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p> : null}
          <button disabled={isSubmitting} className="w-full rounded-xl bg-[#075b3a] px-5 py-3.5 font-semibold text-white disabled:opacity-60">{isSubmitting ? "Molimo sačekaj…" : isRegister ? "Kreiraj račun" : "Prijavi se"}</button>
        </form>
        <p className="mt-7 text-sm text-[#607080]">{isRegister ? "Već imaš račun?" : "Nemaš račun?"} <Link className="font-bold text-[#2563d9]" href={isRegister ? "/login" : "/register"}>{isRegister ? "Prijavi se" : "Registruj se"}</Link></p>
      </section>
    </main>
  );
}
