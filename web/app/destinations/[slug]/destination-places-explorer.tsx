"use client";

import { useRouter } from "next/navigation";
import { useDeferredValue, useEffect, useMemo, useRef, useState } from "react";
import { apiFetch, getErrorMessage, type SavedPlace } from "../../lib/api";
import { useAuth } from "../../components/auth-provider";
import { text, type Language } from "../../lib/i18n";
import { useDestinationPlaces } from "./destination-places-context";
import DestinationMap from "./destination-map";
import type { MapFocusRequest } from "./map-presentation";
import { countPlaceGroups, filterPlaces, PLACE_GROUPS, selectBalancedPlaces, sortPlaces, type PlaceGroup } from "./place-explorer-data";
import type { Destination, Place } from "./types";
import styles from "./destination.module.css";

export default function DestinationPlacesExplorer({ destination, token, language }: { destination: Destination; token: string | null; language: Language }) {
  const { places, status, reload } = useDestinationPlaces();
  const { status: authStatus, token: authToken, signOut } = useAuth();
  const router = useRouter();
  const labels = text[language].destinationExtras;
  const explorer = labels.placesExplorer;
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const deferredQuery = useDeferredValue(query);
  const [group, setGroup] = useState<PlaceGroup>("all");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mapFocus, setMapFocus] = useState<MapFocusRequest | null>(null);
  const [mobileView, setMobileView] = useState<"list" | "map">("list");
  const [savedByPlace, setSavedByPlace] = useState<Record<string, string>>({});
  const [savePending, setSavePending] = useState<string | null>(null);
  const [saveMessage, setSaveMessage] = useState("");

  const orderedPlaces = useMemo(() => sortPlaces(places, language), [language, places]);
  const preview = useMemo(() => selectBalancedPlaces(orderedPlaces), [orderedPlaces]);
  const counts = useMemo(() => countPlaceGroups(orderedPlaces), [orderedPlaces]);
  const filtered = useMemo(() => filterPlaces(orderedPlaces, group, deferredQuery), [deferredQuery, group, orderedPlaces]);
  const selected = filtered.find((place) => place.id === selectedId) ?? filtered[0] ?? null;

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (!isOpen) {
      if (dialog.open) dialog.close();
      triggerRef.current?.focus();
      return;
    }
    if (!dialog.open) dialog.showModal();
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    requestAnimationFrame(() => searchRef.current?.focus());
    return () => { document.body.style.overflow = previousOverflow; };
  }, [isOpen]);

  useEffect(() => {
    if (authStatus !== "authenticated" || !authToken) return;
    const controller = new AbortController();
    apiFetch("/api/saved-places", { signal: controller.signal }, authToken)
      .then(async (response) => {
        if (response.status === 401 || response.status === 403) { signOut(); return; }
        if (!response.ok) throw new Error(await getErrorMessage(response, explorer.saveLoadError));
        const saved = await response.json() as SavedPlace[];
        const next: Record<string, string> = {};
        for (const item of saved) if (item.placeId) next[item.placeId] = item.id;
        setSavedByPlace(next);
      })
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setSaveMessage(error instanceof Error ? error.message : explorer.saveLoadError);
      });
    return () => controller.abort();
  }, [authStatus, authToken, explorer.saveLoadError, signOut]);

  function openExplorer(placeId: string | undefined, trigger: HTMLButtonElement) {
    triggerRef.current = trigger;
    // A preview link must reveal its target even if the previous dialog visit
    // left a different category or search term selected.
    if (placeId) { setQuery(""); setGroup("all"); }
    setSelectedId(placeId ?? preview[0]?.id ?? null);
    setMapFocus(placeId ? { placeId, sequence: 0 } : null);
    setMobileView("list");
    setIsOpen(true);
  }

  function closeExplorer() {
    setIsOpen(false);
  }

  async function toggleSaved(place: Place) {
    if (authStatus === "guest") {
      router.push(`/login?returnTo=${encodeURIComponent(`/destinations/${destination.slug}`)}`);
      return;
    }
    if (!authToken || savePending) return;
    const savedId = savedByPlace[place.id];
    setSavePending(place.id);
    setSaveMessage("");
    try {
      if (savedId) {
        const response = await apiFetch(`/api/saved-places/${savedId}`, { method: "DELETE" }, authToken);
        if (response.status === 401 || response.status === 403) throw new SessionExpiredError();
        if (!response.ok) throw new Error(await getErrorMessage(response, explorer.saveError));
        setSavedByPlace((current) => { const next = { ...current }; delete next[place.id]; return next; });
        setSaveMessage(explorer.removed);
      } else {
        const response = await apiFetch("/api/saved-places", { method: "POST", body: JSON.stringify({ placeId: place.id }) }, authToken);
        if (response.status === 401 || response.status === 403) throw new SessionExpiredError();
        if (!response.ok) throw new Error(await getErrorMessage(response, explorer.saveError));
        const saved = await response.json() as SavedPlace;
        setSavedByPlace((current) => ({ ...current, [place.id]: saved.id }));
        setSaveMessage(explorer.saved);
      }
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        signOut();
        router.replace(`/login?returnTo=${encodeURIComponent(`/destinations/${destination.slug}`)}`);
        return;
      }
      setSaveMessage(error instanceof Error ? error.message : explorer.saveError);
    } finally {
      setSavePending(null);
    }
  }

  return <div className={styles.placesExperience}>
    <div className={styles.placesHeading}>
      <h2 id="places-title">{explorer.title}</h2>
      <p>{explorer.intro}</p>
    </div>
    {preview.length ? <ul className={styles.topPlaces}>
      {preview.map((place) => <li key={place.id}>
        <button type="button" className={styles.previewPlace} onClick={(event) => openExplorer(place.id, event.currentTarget)}>
          <span><strong>{place.name}</strong><small>{categoryLabel(place, labels.categories)}{place.distanceKm != null ? ` · ${explorer.distance(formatDistance(place.distanceKm, language))}` : ""}</small></span>
          <span aria-hidden="true">↗</span>
        </button>
      </li>)}
    </ul> : <p>{labels.topPlacesEmpty}</p>}
    {orderedPlaces.length ? <button type="button" className={styles.exploreAll} onClick={(event) => openExplorer(undefined, event.currentTarget)}>{explorer.viewAll(orderedPlaces.length)} <span aria-hidden="true">→</span></button> : null}
    {status === "loading" ? <p className={styles.placesStatus} role="status">{explorer.loading}</p> : null}
    {status === "error" ? <div className={styles.placesError} role="status"><span>{explorer.unavailable}</span><button type="button" onClick={reload}>{explorer.retry}</button></div> : null}
    {orderedPlaces.some((place) => place.metadata?.externalId) ? <p className={styles.attribution}><a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">{labels.osmAttribution}</a></p> : null}

    <dialog ref={dialogRef} className={styles.placesDialog} aria-labelledby="places-dialog-title" onCancel={(event) => { event.preventDefault(); closeExplorer(); }} onClose={() => setIsOpen(false)} onClick={(event) => { if (event.target === event.currentTarget) closeExplorer(); }}>
      {isOpen ? <div className={styles.dialogSurface}>
        <header className={styles.dialogHeader}>
          <div><span>{destination.name}</span><h2 id="places-dialog-title">{explorer.title}</h2></div>
          <button type="button" className={styles.dialogClose} aria-label={explorer.close} onClick={closeExplorer}>×</button>
        </header>
        <div className={styles.explorerToolbar}>
          <label className={styles.placeSearch}><span>{explorer.searchLabel}</span><input ref={searchRef} type="search" value={query} placeholder={explorer.searchPlaceholder} onChange={(event) => setQuery(event.target.value)} /></label>
          <div className={styles.mobileViewToggle} aria-label={`${explorer.list} / ${explorer.map}`}>
            <button type="button" aria-pressed={mobileView === "list"} onClick={() => setMobileView("list")}>{explorer.list}</button>
            <button type="button" aria-pressed={mobileView === "map"} onClick={() => setMobileView("map")}>{explorer.map}</button>
          </div>
          <div className={styles.placeFilters} aria-label={explorer.searchLabel}>
            {PLACE_GROUPS.map((item) => <button key={item} type="button" aria-pressed={group === item} onClick={() => setGroup(item)}>{explorer.filters[item]} <span>{counts[item]}</span></button>)}
          </div>
        </div>
        <div className={styles.explorerCount} aria-live="polite">{explorer.results(filtered.length)}</div>
        <div className={styles.explorerBody} data-mobile-view={mobileView}>
          <section className={styles.placeResults} aria-label={explorer.list}>
            {filtered.length ? <ul>{filtered.map((place, index) => {
              const isSelected = selected?.id === place.id;
              const isSaved = Boolean(savedByPlace[place.id]);
              return <li id={`place-result-${place.id}`} key={place.id} className={isSelected ? styles.placeResultSelected : ""}>
                <button type="button" className={styles.placeResultSummary} aria-expanded={isSelected} onClick={() => { setSelectedId(place.id); setMapFocus(current => ({ placeId: place.id, sequence: (current?.sequence ?? 0) + 1 })); }}>
                  <span className={styles.resultIndex} aria-hidden="true">{String(index + 1).padStart(2, "0")}</span>
                  <span><small>{categoryLabel(place, labels.categories)}</small><strong>{place.name}</strong>{place.distanceKm != null ? <em>{explorer.distance(formatDistance(place.distanceKm, language))}</em> : null}</span>
                  <span aria-hidden="true">{isSelected ? "−" : "+"}</span>
                </button>
                {isSelected ? <div className={styles.placeDetails}>
                  {placeDescription(place, language) ? <p lang={place.metadata?.descriptionLanguage ?? language}>{placeDescription(place, language)}</p> : null}
                  <dl>
                    {place.metadata?.address ? <div><dt>{explorer.address}</dt><dd>{place.metadata.address}</dd></div> : null}
                    {place.metadata?.cuisine ? <div><dt>{explorer.cuisine}</dt><dd>{place.metadata.cuisine}</dd></div> : null}
                    {place.metadata?.priceLevel ? <div><dt>{explorer.price}</dt><dd>{(explorer.prices as Record<string, string>)[place.metadata.priceLevel] ?? place.metadata.priceLevel}</dd></div> : null}
                  </dl>
                  <div className={styles.placeLinks}>
                    {place.metadata?.website ? <a href={place.metadata.website} target="_blank" rel="noreferrer">{explorer.website} ↗</a> : null}
                    {place.metadata?.phone ? <a href={`tel:${place.metadata.phone}`}>{explorer.phone}</a> : null}
                    {place.metadata?.sourceUrl ? <a href={place.metadata.sourceUrl} target="_blank" rel="noreferrer">{explorer.source} ↗</a> : null}
                  </div>
                  <button type="button" className={styles.savePlace} disabled={authStatus === "checking" || savePending === place.id} aria-pressed={isSaved} onClick={() => void toggleSaved(place)}>{authStatus === "guest" ? explorer.loginToSave : isSaved ? explorer.remove : explorer.save}</button>
                  <p className={styles.saveStatus} aria-live="polite">{saveMessage}</p>
                </div> : null}
              </li>;
            })}</ul> : <p className={styles.explorerEmpty}>{deferredQuery ? explorer.emptySearch : explorer.emptyFilter}</p>}
          </section>
          <section className={styles.explorerMapPane} aria-label={explorer.map}>
            <DestinationMap destination={destination} token={token} language={language} places={filtered} selectedPlaceId={selected?.id ?? null} focusRequest={mapFocus} onPlaceSelect={(placeId) => {
              setSelectedId(placeId);
              requestAnimationFrame(() => document.getElementById(`place-result-${placeId}`)?.scrollIntoView({ block: "nearest" }));
            }} explorer />
          </section>
        </div>
      </div> : null}
    </dialog>
  </div>;
}

class SessionExpiredError extends Error {}

function categoryLabel(place: Place, categories: Record<string, string>): string {
  return categories[place.category] ?? place.category;
}

function placeDescription(place: Place, language: Language): string | null {
  const metadata = place.metadata;
  if (!metadata) return null;
  return (language === "en" ? metadata.descriptionEn : metadata.descriptionBs)
    ?? metadata.description ?? metadata.descriptionBs ?? metadata.descriptionEn;
}

function formatDistance(distance: number, language: Language): string {
  return new Intl.NumberFormat(language, { maximumFractionDigits: distance < 10 ? 1 : 0 }).format(distance);
}
