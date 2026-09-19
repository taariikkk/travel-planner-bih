"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { apiFetch, getErrorMessage, Recommendation, UserProfile } from "../lib/api";
import styles from "./recommendation-wizard.module.css";

const interests = [
  ["historija", "Historija"], ["kultura", "Kultura"], ["priroda", "Priroda"], ["planina", "Planina"],
  ["avantura", "Avantura"], ["hrana", "Hrana"], ["vino", "Vino"], ["rijeka", "Rijeka"],
] as const;

const budgets = [["budget", "Niži"], ["standard", "Srednji"], ["premium", "Viši"]] as const;
const seasons = [["spring", "Proljeće"], ["summer", "Ljeto"], ["autumn", "Jesen"], ["winter", "Zima"]] as const;

function destinationSlug(name: string) {
  return name
    .toLocaleLowerCase("bs")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

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

  if (isLoading && !profile) return <main className={styles.loading} role="status">Učitavamo tvoj profil…</main>;

  return (
    <main className={styles.page}>
      <nav className={styles.nav} aria-label="Glavna navigacija">
        <div className={styles.navInner}>
          <Link className={styles.brand} href="/">Travel Planner <strong>BiH</strong></Link>
          <div className={styles.account}><span>{profile?.displayName}</span><button onClick={logout}>Odjavi se</button></div>
        </div>
      </nav>
      <section className={styles.heading}>
        <div className={styles.headingBackdrop} aria-hidden="true" />
        <div className={styles.headingContent}>
          <h1>Gdje te vodi <em>znatiželja?</em></h1>
          <p>Reci nam šta voliš. Pronađimo tvoj sljedeći kutak Bosne i Hercegovine.</p>
        </div>
      </section>
      <section className={styles.content} aria-label="Preferencije i preporuke">
        <div className={styles.wizard}>
          <fieldset className={styles.interests}>
            <legend><span className={styles.number}>01</span> Šta želiš doživjeti?</legend>
            <p className={styles.hint}>Odaberi sve što te privlači.</p>
            <div className={styles.options}>
              {interests.map(([value, label]) => (
                <button type="button" key={value} aria-pressed={selectedInterests.includes(value)} onClick={() => toggleInterest(value)} className={styles.option}>
                  <span aria-hidden="true" className={styles.check}>{selectedInterests.includes(value) ? "✓" : "+"}</span>{label}
                </button>
              ))}
            </div>
          </fieldset>
          <div className={styles.details}>
            <fieldset>
              <legend><span className={styles.number}>02</span> Tvoj budžet</legend>
              <p className={styles.hint}>Okvirni nivo troška, bez iznosa u KM.</p>
              <div className={styles.options}>{budgets.map(([value, label]) => <button type="button" key={value} aria-pressed={budgetTier === value} onClick={() => setBudgetTier(value)} className={styles.option}>{label}</button>)}</div>
            </fieldset>
            <div>
              <label htmlFor="travel-days" className={styles.fieldTitle}><span className={styles.number}>03</span> Koliko dana imaš?</label>
              <p id="travel-days-hint" className={styles.hint}>Od kratkog predaha do dužeg odmora.</p>
              <div className={styles.days}><input id="travel-days" aria-describedby="travel-days-hint" min="1" max="14" type="number" value={travelDays} onChange={(event) => setTravelDays(Number(event.target.value))} /><span>dana <small> / 1–14</small></span></div>
            </div>
            <fieldset>
              <legend><span className={styles.number}>04</span> Kada putuješ?</legend>
              <p className={styles.hint}>Svako godišnje doba ima svoj doživljaj.</p>
              <div className={styles.options}>{seasons.map(([value, label]) => <button type="button" key={value} aria-pressed={season === value} onClick={() => setSeason(value)} className={styles.option}>{label}</button>)}</div>
            </fieldset>
          </div>
          <div className={styles.submitRow}>
            <p>Interesovanja čuvamo na tvom profilu.<br />Odabir možeš promijeniti kad god poželiš.</p>
            <button disabled={isLoading || !profile} onClick={() => void findRecommendations()} className={styles.primary}>
              {isLoading ? "Tražimo preporuke…" : "Prikaži preporuke"}
              <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
            </button>
          </div>
        </div>
        {error ? <p role="alert" className={styles.error}>{error}</p> : null}
        <div role="status" className={styles.status}>{isLoading ? "Učitavanje preporuka…" : results.length > 0 ? `Prikazano preporuka: ${results.length}.` : ""}</div>
        {results.length > 0 ? (
          <section className={styles.results} aria-busy={isLoading} aria-labelledby="results-title">
            <div className={styles.resultsHeader}>
              <div><h2 id="results-title">Mjesta za tvoj ritam.</h2><p>Tvoje preporuke, poredane prema odabranim preferencijama.</p></div>
              <div className={styles.language} role="group" aria-label="Jezik preporuka">
                {["bs", "en"].map((code) => <button key={code} disabled={isLoading} aria-pressed={language === code} aria-label={code === "bs" ? "Preporuke na bosanskom" : "Recommendations in English"} onClick={() => changeLanguage(code)}>{code.toUpperCase()}</button>)}
              </div>
            </div>
            <ol className={styles.resultList} lang={language}>
              {results.map((result, index) => (
                <li key={result.id} className={styles.result}>
                  <span className={styles.rank} aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>
                  <div className={styles.destination}>
                    <p>{result.region}</p>
                    <h3>{result.name}</h3>
                    <div className={styles.tags}>{result.tags.map((tag) => <span key={tag}>{tag}</span>)}</div>
                    <Link className={styles.destinationLink} href={`/destinations/${destinationSlug(result.name)}#mapa`}>
                      {language === "en" ? "Open map" : "Otvori mapu"} <span aria-hidden="true">↗</span>
                    </Link>
                  </div>
                  <div className={styles.reason}><span className={styles.reasonLabel}>{language === "en" ? "Why it fits" : "Zašto ti odgovara"}</span><p>{result.reason}</p><details><summary>{language === "en" ? "About this destination" : "Više o destinaciji"}</summary><p>{result.description}</p><p><strong>{language === "en" ? "Best time to visit: " : "Najbolje vrijeme za posjetu: "}</strong>{result.bestTimeToVisit}</p></details></div>
                  <div className={styles.score}><strong>{result.score}</strong><span>{language === "en" ? "points" : "bodova"}</span></div>
                </li>
              ))}
            </ol>
          </section>
        ) : !error ? <div className={styles.empty}><span aria-hidden="true">↳</span><p>Prvo tvoja interesovanja.<br /><strong>Zatim mjesta koja vrijedi upoznati.</strong></p></div> : null}
      </section>
    </main>
  );
}
