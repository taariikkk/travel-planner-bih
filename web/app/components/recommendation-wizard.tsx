"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { apiFetch, getErrorMessage, Recommendation, UserProfile } from "../lib/api";
import { formatTag, formatTagLabel, text } from "../lib/i18n";
import { formatRecommendationReason } from "../lib/recommendation-reason";
import { useLanguage } from "./language-provider";
import { useAuth } from "./auth-provider";
import styles from "./recommendation-wizard.module.css";

export function RecommendationWizard() {
  const language = useLanguage();
  const t = text[language].rec;
  const { token, user, profileError, updateUser } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(() => user);
  const [selectedInterests, setSelectedInterests] = useState<string[]>(() => user?.preferences ?? []);
  const [budgetTier, setBudgetTier] = useState("standard");
  const [travelDays, setTravelDays] = useState(3);
  const [season, setSeason] = useState("summer");
  const [results, setResults] = useState<Recommendation[]>([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const previousLanguage = useRef(language);

  useEffect(() => {
    if (previousLanguage.current === language) return;
    previousLanguage.current = language;
    if (!token || !profile || results.length === 0) return;
    apiFetch("/api/destinations/recommend", { method: "POST", body: JSON.stringify({ budgetTier, travelDays, season, language }) }, token)
      .then(async (response) => {
        if (!response.ok) throw new Error(await getErrorMessage(response, t.unavailable));
        setResults((await response.json()) as Recommendation[]);
      })
      .catch((exception: unknown) => setError(exception instanceof Error ? exception.message : text[language].auth.unexpectedError));
  }, [budgetTier, language, profile, results.length, season, t.unavailable, token, travelDays]);

  function toggleInterest(interest: string) {
    setSelectedInterests((current) => current.includes(interest) ? current.filter((item) => item !== interest) : [...current, interest]);
  }

  async function findRecommendations() {
    if (!token || !profile) return;
    setError("");
    setIsLoading(true);
    try {
      const updateProfile = await apiFetch("/api/users/me", { method: "PUT", body: JSON.stringify({ displayName: profile.displayName, preferences: selectedInterests }) }, token);
      if (!updateProfile.ok) throw new Error(await getErrorMessage(updateProfile, t.interestsSaveFailed));
      const updatedUser = (await updateProfile.json()) as UserProfile;
      setProfile(updatedUser);
      updateUser(updatedUser);
      const response = await apiFetch("/api/destinations/recommend", { method: "POST", body: JSON.stringify({ budgetTier, travelDays, season, language }) }, token);
      if (!response.ok) throw new Error(await getErrorMessage(response, t.unavailable));
      setResults((await response.json()) as Recommendation[]);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : text[language].auth.unexpectedError);
    } finally {
      setIsLoading(false);
    }
  }

  if (profileError) return <main className={styles.loading} role="alert">{t.profileUnavailable}</main>;
  if (isLoading && !profile) return <main className={styles.loading} role="status">{t.loadingProfile}</main>;

  return (
    <main className={styles.page}>
      <section className={styles.heading}>
        <div className={styles.headingBackdrop} aria-hidden="true" />
        <div className={styles.headingContent}>
          <h1>{t.title[0]} <em>{t.title[1]}</em></h1>
          <p>{t.description}</p>
        </div>
      </section>
      <section className={styles.content} aria-label="Preferencije i preporuke">
        <div className={styles.wizard}>
          <fieldset className={styles.interests}>
            <legend><span className={styles.number}>01</span> {t.interestsTitle}</legend>
            <p className={styles.hint}>{t.interestsHint}</p>
            <div className={styles.options}>
              {t.interestOptions.map(([value, label]) => (
                <button type="button" key={value} aria-pressed={selectedInterests.includes(value)} onClick={() => toggleInterest(value)} className={styles.option}>
                  <span aria-hidden="true" className={styles.check}>{selectedInterests.includes(value) ? "✓" : "+"}</span>{label}
                </button>
              ))}
            </div>
          </fieldset>
          <div className={styles.details}>
            <fieldset>
              <legend><span className={styles.number}>02</span> {t.budget}</legend>
              <p className={styles.hint}>{t.budgetHint}</p>
              <div className={styles.options}>{t.budgets.map(([value, label]) => <button type="button" key={value} aria-pressed={budgetTier === value} onClick={() => setBudgetTier(value)} className={styles.option}>{label}</button>)}</div>
            </fieldset>
            <div>
              <label htmlFor="travel-days" className={styles.fieldTitle}><span className={styles.number}>03</span> {t.days}</label>
              <p id="travel-days-hint" className={styles.hint}>{t.daysHint}</p>
              <div className={styles.days}><input id="travel-days" aria-describedby="travel-days-hint" min="1" max="14" type="number" value={travelDays} onChange={(event) => setTravelDays(Number(event.target.value))} /><span>{t.daysUnit} <small> / 1–14</small></span></div>
            </div>
            <fieldset>
              <legend><span className={styles.number}>04</span> {t.season}</legend>
              <p className={styles.hint}>{t.seasonHint}</p>
              <div className={styles.options}>{t.seasons.map(([value, label]) => <button type="button" key={value} aria-pressed={season === value} onClick={() => setSeason(value)} className={styles.option}>{label}</button>)}</div>
            </fieldset>
          </div>
          <div className={styles.submitRow}>
            <p>{t.saved[0]}<br />{t.saved[1]}</p>
            <button disabled={isLoading || !profile} onClick={() => void findRecommendations()} className={styles.primary}>
              {isLoading ? t.finding : t.find}
              <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
            </button>
          </div>
        </div>
        {error ? <p role="alert" className={styles.error}>{error}</p> : null}
        <div role="status" className={styles.status}>{isLoading ? t.loading : results.length > 0 ? t.shown(results.length) : ""}</div>
        {results.length > 0 ? (
          <section className={styles.results} aria-busy={isLoading} aria-labelledby="results-title">
            <div className={styles.resultsHeader}>
              <div><h2 id="results-title">{t.resultsTitle}</h2><p>{t.resultsDescription}</p></div>
            </div>
            <ol className={styles.resultList} lang={language}>
              {results.map((result, index) => (
                <li key={result.id} className={styles.result}>
                  <span className={styles.rank} aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>
                  <div className={styles.destination}>
                    <p>{result.region}</p>
                    <h3>{result.name}</h3>
                    <div className={styles.tags}>{result.tags.map((tag) => <span key={tag}>{formatTag(tag, language)}</span>)}</div>
                    <Link className={styles.destinationLink} href={`/destinations/${result.slug}#mapa`}>
                      {t.map} <span aria-hidden="true">↗</span>
                    </Link>
                  </div>
                  <div className={styles.reason}><span className={styles.reasonLabel}>{t.why}</span><p>{formatRecommendationReason(result.reasons, t.recommendationReason, (tag) => formatTagLabel(tag, language))}</p><details><summary>{t.about}</summary><p>{result.description}</p><p><strong>{t.bestTime}</strong>{result.bestTimeToVisit}</p></details></div>
                  <div className={styles.score}><strong>{result.score}</strong><span>{t.points}</span></div>
                </li>
              ))}
            </ol>
          </section>
        ) : !error ? <div className={styles.empty}><span aria-hidden="true">↳</span><p>{t.empty[0]}<br /><strong>{t.empty[1]}</strong></p></div> : null}
      </section>
    </main>
  );
}
