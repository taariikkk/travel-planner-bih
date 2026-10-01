"use client";

import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { apiFetch } from "../../lib/api";
import { createPlacesCoordinator, type PlacesState } from "./destination-map-data";
import type { Place } from "./types";

type PlacesContextValue = PlacesState & { reload: () => void };
const PlacesContext = createContext<PlacesContextValue | null>(null);

export function DestinationPlacesProvider({ slug, initialPlaces, children }: { slug: string; initialPlaces: Place[]; children: ReactNode }) {
  return <DestinationPlacesSession key={slug} slug={slug} initialPlaces={initialPlaces}>{children}</DestinationPlacesSession>;
}

function DestinationPlacesSession({ slug, initialPlaces, children }: { slug: string; initialPlaces: Place[]; children: ReactNode }) {
  const router = useRouter();
  const [coordinator] = useState(() => createPlacesCoordinator(slug, apiFetch, initialPlaces));
  const [state, setState] = useState<PlacesState>({ places: initialPlaces, status: "loading" });
  useEffect(() => coordinator.mount(setState, () => router.refresh()), [coordinator, router]);
  const reload = useCallback(() => {
    setState((current) => ({ ...current, status: "loading" }));
    apiFetch(`/api/destinations/${encodeURIComponent(slug)}/map-places`, { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error("Places request failed");
        setState({ places: await response.json() as Place[], status: "ready" });
      })
      .catch(() => setState((current) => ({ ...current, status: "error" })));
  }, [slug]);
  return <PlacesContext value={{ ...state, reload }}>{children}</PlacesContext>;
}

export function useDestinationPlaces(): PlacesContextValue {
  const value = useContext(PlacesContext);
  if (!value) throw new Error("useDestinationPlaces must be used inside DestinationPlacesProvider");
  return value;
}
