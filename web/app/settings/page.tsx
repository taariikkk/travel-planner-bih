"use client";

import { FormEvent, useState } from "react";
import { apiFetch, getErrorMessage } from "../lib/api";
import { text } from "../lib/i18n";
import { useAuth } from "../components/auth-provider";
import { useLanguage } from "../components/language-provider";
import styles from "./settings.module.css";

export default function SettingsPage() {
  const language = useLanguage();
  const copy = text[language].settings;
  const options = text[language].rec.interestOptions;
  const { token, user, profileError, updateUser } = useAuth();
  const [displayName, setDisplayName] = useState(() => user?.displayName ?? "");
  const [preferences, setPreferences] = useState<string[]>(() => user?.preferences ?? []);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState(false);

  function togglePreference(preference: string) {
    setSaved(false);
    setPreferences((current) => current.includes(preference) ? current.filter((item) => item !== preference) : [...current, preference]);
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token) return;
    setError("");
    setSaved(false);
    setIsSaving(true);
    try {
      const response = await apiFetch("/api/users/me", { method: "PUT", body: JSON.stringify({ displayName, preferences }) }, token);
      if (!response.ok) throw new Error(await getErrorMessage(response, copy.saveError));
      updateUser(await response.json());
      setSaved(true);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : copy.saveError);
    } finally {
      setIsSaving(false);
    }
  }

  if (profileError) return <main className={styles.loading}><p role="alert">{copy.loadError}</p></main>;
  if (!user) return <main className={styles.loading}><p role="status">{text[language].navigation.loading}</p></main>;

  return <main className={styles.page}>
    <header className={styles.heading}>
      <p>{copy.number}</p>
      <h1>{copy.title[0]}<br /><em>{copy.title[1]}</em></h1>
      <span>{copy.description}</span>
    </header>
    <form className={styles.form} onSubmit={save}>
      <div className={styles.identity}>
        <label>{copy.name}<input required value={displayName} onChange={(event) => { setDisplayName(event.target.value); setSaved(false); }} /></label>
        <label>{copy.email}<input readOnly value={user.email} /></label>
      </div>
      <fieldset>
        <legend>{copy.interests}</legend>
        <p>{copy.interestsHint}</p>
        <div className={styles.options}>{options.map(([value, label]) => <button key={value} type="button" aria-pressed={preferences.includes(value)} onClick={() => togglePreference(value)}>{preferences.includes(value) ? <span aria-hidden="true">✓</span> : <span aria-hidden="true">+</span>}{label}</button>)}</div>
      </fieldset>
      <div className={styles.actions}>
        <div aria-live="polite">{error ? <p className={styles.error} role="alert">{error}</p> : saved ? <p className={styles.success}>{copy.saved}</p> : null}</div>
        <button className={styles.submit} disabled={isSaving} type="submit">{isSaving ? copy.saving : copy.save}<svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg></button>
      </div>
    </form>
  </main>;
}
