"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { apiFetch, getErrorMessage, Recommendation, UserProfile } from "../lib/api";

const interests = [
  ["historija", "Historija"], ["kultura", "Kultura"], ["priroda", "Priroda"], ["planina", "Planina"],
  ["avantura", "Avantura"], ["hrana", "Hrana"], ["vino", "Vino"], ["rijeka", "Rijeka"],
] as const;

const budgets = [["budget", "Niži"], ["standard", "Srednji"], ["premium", "Viši"]] as const;
const seasons = [["spring", "Proljeće"], ["summer", "Ljeto"], ["autumn", "Jesen"], ["winter", "Zima"]] as const;

export function RecommendationWizard() {
  const router = useRouter();
  const tokenRef = useRef<string | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [selectedInterests, setSelectedInterests] = useState<string[]>([]);
  const [budgetTier, setBudgetTier] = useState("standard");
  const [travelDays, setTravelDays] = useState(3);
  const [season, setSeason] = useState("summer");
  const [language, setLanguage] = useState("bs");
  const [results, setResults] = useState<Recommendation[]>([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const storedToken = sessionStorage.getItem("travelPlanner.accessToken");
    if (!storedToken) {
      router.replace("/login");
      return;
    }
    tokenRef.current = storedToken;
    apiFetch("/api/users/me", {}, storedToken)
      .then(async (response) => {
        if (!response.ok) throw new Error("Sesija je istekla. Prijavi se ponovo.");
        const user = (await response.json()) as UserProfile;
        setProfile(user);
        setSelectedInterests(user.preferences);
      })
      .catch((exception: unknown) => setError(exception instanceof Error ? exception.message : "Profil nije dostupan."))
      .finally(() => setIsLoading(false));
  }, [router]);

  function toggleInterest(interest: string) {
    setSelectedInterests((current) => current.includes(interest) ? current.filter((item) => item !== interest) : [...current, interest]);
  }

  async function findRecommendations(nextLanguage = language) {
    const token = tokenRef.current;
    if (!token || !profile) return;
    setError("");
    setIsLoading(true);
    try {
      const updateProfile = await apiFetch("/api/users/me", { method: "PUT", body: JSON.stringify({ displayName: profile.displayName, preferences: selectedInterests }) }, token);
      if (!updateProfile.ok) throw new Error(await getErrorMessage(updateProfile, "Interesovanja nisu sačuvana."));
      const response = await apiFetch("/api/destinations/recommend", { method: "POST", body: JSON.stringify({ budgetTier, travelDays, season, language: nextLanguage }) }, token);
      if (!response.ok) throw new Error(await getErrorMessage(response, "Preporuke nisu dostupne."));
      setResults((await response.json()) as Recommendation[]);
      setLanguage(nextLanguage);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Došlo je do greške.");
    } finally {
      setIsLoading(false);
    }
  }

  function changeLanguage(nextLanguage: string) {
    if (nextLanguage !== language && results.length > 0) void findRecommendations(nextLanguage);
    else setLanguage(nextLanguage);
  }

  function logout() {
    sessionStorage.removeItem("travelPlanner.accessToken");
    router.replace("/");
  }

  if (isLoading && !profile) return <main className="grid min-h-screen place-items-center text-[#607080]">Učitavamo tvoj profil…</main>;

  return (
    <main className="min-h-screen bg-white text-[#10231e]">
      <nav className="border-b border-[#dce5e2]">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
          <Link className="flex items-center gap-3 text-lg font-bold tracking-tight" href="/"><span className="grid h-9 w-9 place-items-center rounded-full bg-[#075b3a] text-white">⌁</span>Travel Planner BiH</Link>
          <div className="flex items-center gap-5 text-sm font-semibold"><span className="hidden text-[#607080] sm:inline">{profile?.displayName}</span><button className="text-[#2563d9]" onClick={logout}>Odjavi se</button></div>
        </div>
      </nav>
      <section className="relative overflow-hidden border-b border-[#e7efec] bg-[#fbfdfc] px-6 py-14 text-center">
        <div className="absolute -left-16 top-[-100px] h-56 w-56 rounded-full border-[22px] border-[#e9f1ee]" />
        <h1 className="relative text-4xl font-bold tracking-tight sm:text-5xl">Pronađimo tvoje idealno putovanje</h1>
        <p className="relative mx-auto mt-4 max-w-2xl text-lg text-[#607080]">Kombinujemo tvoja interesovanja s kuriranim destinacijama Bosne i Hercegovine.</p>
      </section>
      <section className="mx-auto max-w-7xl px-6 py-10">
        <div className="grid gap-8 rounded-3xl border border-[#dce5e2] bg-white p-6 shadow-sm lg:grid-cols-[1.6fr_1fr_1fr_1.1fr_auto] lg:items-center">
          <fieldset><legend className="mb-3 text-sm font-bold">Koja iskustva tražiš?</legend><div className="flex flex-wrap gap-2">{interests.map(([value, label]) => <button key={value} onClick={() => toggleInterest(value)} className={`rounded-full px-3 py-2 text-sm font-medium ${selectedInterests.includes(value) ? "bg-[#075b3a] text-white" : "bg-[#f3f6f6] text-[#40515d]"}`}>{label}</button>)}</div></fieldset>
          <fieldset><legend className="mb-3 text-sm font-bold">Budžet</legend><div className="flex flex-wrap gap-2">{budgets.map(([value, label]) => <button key={value} onClick={() => setBudgetTier(value)} className={`rounded-full px-3 py-2 text-sm font-medium ${budgetTier === value ? "bg-[#075b3a] text-white" : "bg-[#f3f6f6]"}`}>{label}</button>)}</div></fieldset>
          <label className="block text-sm font-bold">Koliko dana putuješ?<input min="1" max="14" type="number" value={travelDays} onChange={(event) => setTravelDays(Number(event.target.value))} className="mt-3 w-full rounded-xl border border-[#dce5e2] px-3 py-2.5 font-medium outline-none focus:border-[#075b3a]" /></label>
          <fieldset><legend className="mb-3 text-sm font-bold">Sezona</legend><div className="flex flex-wrap gap-2">{seasons.map(([value, label]) => <button key={value} onClick={() => setSeason(value)} className={`rounded-full px-3 py-2 text-sm font-medium ${season === value ? "bg-[#075b3a] text-white" : "bg-[#f3f6f6]"}`}>{label}</button>)}</div></fieldset>
          <button disabled={isLoading} onClick={() => void findRecommendations()} className="rounded-xl bg-[#075b3a] px-5 py-3.5 font-semibold whitespace-nowrap text-white disabled:opacity-60">{isLoading ? "Učitavanje…" : "Prikaži preporuke →"}</button>
        </div>
        {error ? <p className="mt-6 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p> : null}
        {results.length > 0 ? <section className="mt-12"><div className="flex items-center justify-between gap-4"><h2 className="text-3xl font-bold tracking-tight">Tvoje preporuke</h2><div className="flex rounded-full bg-[#f3f6f6] p-1 text-sm font-bold"><button onClick={() => changeLanguage("bs")} className={`rounded-full px-4 py-2 ${language === "bs" ? "bg-[#075b3a] text-white" : "text-[#607080]"}`}>BS</button><button onClick={() => changeLanguage("en")} className={`rounded-full px-4 py-2 ${language === "en" ? "bg-[#075b3a] text-white" : "text-[#607080]"}`}>EN</button></div></div><ol className="mt-6 divide-y divide-[#dce5e2] border-y border-[#dce5e2]">{results.map((result, index) => <li className="grid gap-4 py-6 md:grid-cols-[56px_1fr_2fr_90px] md:items-center" key={result.id}><span className="grid h-11 w-11 place-items-center rounded-full bg-[#075b3a] text-lg font-bold text-white">{index + 1}</span><div><h3 className="text-xl font-bold">{result.name}</h3><p className="text-sm text-[#607080]">{result.region}</p></div><div><p className="font-medium leading-6">{result.reason}</p><div className="mt-2 flex flex-wrap gap-2">{result.tags.map((tag) => <span className="rounded-full bg-[#f3f6f6] px-3 py-1 text-xs text-[#40515d]" key={tag}>{tag}</span>)}</div></div><p className="text-left text-xl font-bold text-[#075b3a] md:text-right">{result.score}</p></li>)}</ol></section> : null}
      </section>
    </main>
  );
}
