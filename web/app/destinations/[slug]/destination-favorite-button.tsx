"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiFetch, getErrorMessage, type SavedPlace } from "../../lib/api";
import { text } from "../../lib/i18n";
import { useAuth } from "../../components/auth-provider";
import { useLanguage } from "../../components/language-provider";
import styles from "./destination.module.css";

type FavoriteButtonProps = { destinationId: string; slug: string };

export default function DestinationFavoriteButton({ destinationId, slug }: FavoriteButtonProps) {
  const { status, token, signOut } = useAuth();
  const language = useLanguage();
  const labels = text[language].destinationExtras;
  const router = useRouter();
  const [savedPlaceId, setSavedPlaceId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (status !== "authenticated" || !token) return;

    const controller = new AbortController();
    queueMicrotask(() => {
      if (controller.signal.aborted) return;
      setIsLoading(true);
      setMessage("");
      apiFetch("/api/saved-places", { signal: controller.signal }, token)
        .then(async (response) => {
          if (response.status === 401 || response.status === 403) {
            signOut();
            return;
          }
          if (!response.ok) throw new Error(await getErrorMessage(response, labels.favoriteLoadError));
          const savedPlaces = (await response.json()) as SavedPlace[];
          setSavedPlaceId(savedPlaces.find((place) => place.destinationId === destinationId)?.id ?? null);
        })
        .catch((error: unknown) => {
          if (error instanceof DOMException && error.name === "AbortError") return;
          setMessage(error instanceof Error ? error.message : labels.favoriteLoadError);
        })
        .finally(() => setIsLoading(false));
    });

    return () => controller.abort();
  }, [destinationId, labels.favoriteLoadError, signOut, status, token]);

  async function toggleFavorite() {
    if (status === "guest") {
      router.push(`/login?returnTo=${encodeURIComponent(`/destinations/${slug}`)}`);
      return;
    }
    if (!token || isLoading || isSaving) return;

    setIsSaving(true);
    setMessage("");
    try {
      if (savedPlaceId) {
        const response = await apiFetch(`/api/saved-places/${savedPlaceId}`, { method: "DELETE" }, token);
        if (response.status === 401 || response.status === 403) {
          signOut();
          router.replace(`/login?returnTo=${encodeURIComponent(`/destinations/${slug}`)}`);
          return;
        }
        if (!response.ok) throw new Error(await getErrorMessage(response, labels.favoriteError));
        setSavedPlaceId(null);
        setMessage(labels.favoriteRemoved);
      } else {
        const response = await apiFetch("/api/saved-places", { method: "POST", body: JSON.stringify({ destinationId }) }, token);
        if (response.status === 401 || response.status === 403) {
          signOut();
          router.replace(`/login?returnTo=${encodeURIComponent(`/destinations/${slug}`)}`);
          return;
        }
        if (!response.ok) throw new Error(await getErrorMessage(response, labels.favoriteError));
        const savedPlace = (await response.json()) as SavedPlace;
        setSavedPlaceId(savedPlace.id);
        setMessage(labels.favoriteAdded);
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : labels.favoriteError);
    } finally {
      setIsSaving(false);
    }
  }

  const isSaved = status === "authenticated" && savedPlaceId !== null;
  const disabled = status === "checking" || isLoading || isSaving;
  const label = status === "guest" ? labels.favoriteLoginRequired : isSaved ? labels.favoriteRemove : labels.favorite;

  return <div className={styles.favoriteWrap}>
    <button type="button" className={`${styles.favorite} ${isSaved ? styles.favoriteSaved : ""}`} disabled={disabled} aria-pressed={isSaved} aria-label={label} title={label} onClick={toggleFavorite}>
      <svg width="22" height="22" viewBox="0 0 24 24" fill={isSaved ? "currentColor" : "none"} aria-hidden="true"><path d="M20.8 4.6a5.4 5.4 0 0 0-7.6 0L12 5.8l-1.2-1.2a5.4 5.4 0 0 0-7.6 7.6L12 21l8.8-8.8a5.4 5.4 0 0 0 0-7.6Z" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
    </button>
    <p className={styles.favoriteStatus} aria-live="polite">{message}</p>
  </div>;
}
