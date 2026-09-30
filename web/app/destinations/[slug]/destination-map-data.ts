import type { Place } from "./types";

type PointFeature = {
  type: "Feature";
  geometry: { type: "Point"; coordinates: [number, number] };
  properties: { id: string; name: string; category: string };
};

export type PlaceFeatureCollection = {
  type: "FeatureCollection";
  features: PointFeature[];
};

export function createPlaceFeatureCollection(places: readonly Place[]): PlaceFeatureCollection {
  return {
    type: "FeatureCollection",
    features: places.filter(hasValidCoordinates).map((place) => ({
      type: "Feature",
      geometry: { type: "Point", coordinates: [place.longitude, place.latitude] },
      properties: { id: place.id, name: place.name, category: place.category },
    })),
  };
}

export function createClusteredPlaceSource(places: readonly Place[]) {
  return {
    type: "geojson" as const,
    data: createPlaceFeatureCollection(places),
    cluster: true,
    clusterMaxZoom: 14,
    clusterRadius: 50,
  };
}

export function getMapBoundsCoordinates(destination: { latitude: number; longitude: number }, places: readonly Place[]): [number, number][] {
  return [
    [destination.longitude, destination.latitude],
    ...places.filter(hasValidCoordinates).map((place): [number, number] => [place.longitude, place.latitude]),
  ];
}

function hasValidCoordinates(place: Place) {
  return Number.isFinite(place.latitude) && place.latitude >= -90 && place.latitude <= 90
    && Number.isFinite(place.longitude) && place.longitude >= -180 && place.longitude <= 180;
}

export function updatePlaceSource(source: { setData(data: PlaceFeatureCollection): unknown }, places: readonly Place[]) {
  source.setData(createPlaceFeatureCollection(places));
}

type RefreshResult = { status: "refreshed" | "cached" | "deferred"; retryAt: string | null };
export type PlacesState = { places: Place[]; status: "loading" | "ready" | "error" };
type FetchPlaces = (path: string, init: RequestInit) => Promise<Response>;

// Mount-local lifecycle, not a session cache. Reusing the POST promise survives
// Strict Mode's setup/cleanup/setup; each real slug mount gets a new coordinator.
export function createPlacesCoordinator(slug: string, fetcher: FetchPlaces) {
  const path = `/api/destinations/${encodeURIComponent(slug)}`;
  let post: Promise<RefreshResult> | undefined;
  let generation = 0;
  let request = 0;
  let getController: AbortController | undefined;
  let refreshedRoute = false;
  let state: PlacesState = { places: [], status: "loading" };

  return {
    mount(onState: (state: PlacesState) => void, refreshRoute: () => void) {
      const currentGeneration = ++generation;
      const active = () => currentGeneration === generation;
      const getPlaces = async () => {
        const currentRequest = ++request;
        getController?.abort();
        const controller = new AbortController();
        getController = controller;
        try {
          const response = await fetcher(`${path}/map-places`, { cache: "no-store", signal: controller.signal });
          if (!response.ok) throw new Error("Map places request failed");
          const places = await response.json() as Place[];
          if (!active() || controller.signal.aborted || currentRequest !== request) return;
          state = { places, status: "ready" };
          onState(state);
        } catch {
          if (!active() || controller.signal.aborted || currentRequest !== request) return;
          state = { ...state, status: "error" };
          onState(state);
        }
      };
      void getPlaces();
      // Assumption: the backend owns the import timeout independently of client
      // disconnects. Keep this promise for Strict Mode; ignore it after cleanup.
      post ??= fetcher(`${path}/places/refresh`, { method: "POST", cache: "no-store" })
        .then(async response => {
          if (!response.ok) throw new Error("Places refresh failed");
          return await response.json() as RefreshResult;
        });
      void post.then(result => {
        if (!active() || result.status !== "refreshed" || refreshedRoute) return;
        refreshedRoute = true;
        void getPlaces();
        refreshRoute();
      }).catch(() => {
        // Existing places and the destination pin remain usable on import failure.
      });
      return () => {
        if (!active()) return;
        generation++;
        request++;
        getController?.abort();
      };
    },
  };
}
