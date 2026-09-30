"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiFetch } from "../../lib/api";
import { text, type Language } from "../../lib/i18n";
import { createPlacesCoordinator, type PlacesState } from "./destination-map-data";
import DestinationMap from "./destination-map";
import type { Destination } from "./types";
import styles from "./destination.module.css";

export default function DestinationPlacesRefresh(props: { destination: Destination; token: string | null; language: Language }) {
  // The slug key resets mount-local state on navigation, but survives language
  // changes and router.refresh() merging new server props.
  return <DestinationPlacesSession key={props.destination.slug} {...props} />;
}

function DestinationPlacesSession({ destination, token, language }: { destination: Destination; token: string | null; language: Language }) {
  const router = useRouter();
  const [coordinator] = useState(() => createPlacesCoordinator(destination.slug, apiFetch));
  const [state, setState] = useState<PlacesState>({ places: [], status: "loading" });
  useEffect(() => coordinator.mount(setState, () => router.refresh()), [coordinator, router]);
  const extra = text[language].destinationExtras;

  return <>
    <DestinationMap destination={destination} token={token} language={language} places={state.places} />
    {state.status !== "ready" && <p className={styles.legend} role="status">{state.status === "loading" ? extra.mapPlacesLoading : extra.mapPlacesUnavailable}</p>}
  </>;
}
