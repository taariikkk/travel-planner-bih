"use client";

import { text, type Language } from "../../lib/i18n";
import { useDestinationPlaces } from "./destination-places-context";
import DestinationMap from "./destination-map";
import type { Destination } from "./types";
import styles from "./destination.module.css";

export default function DestinationPlacesRefresh({ destination, token, language }: { destination: Destination; token: string | null; language: Language }) {
  const state = useDestinationPlaces();
  const extra = text[language].destinationExtras;
  return <>
    <DestinationMap destination={destination} token={token} language={language} places={state.places} />
    {state.status !== "ready" ? <p className={styles.legend} role="status">{state.status === "loading" ? extra.mapPlacesLoading : extra.mapPlacesUnavailable}</p> : null}
  </>;
}
