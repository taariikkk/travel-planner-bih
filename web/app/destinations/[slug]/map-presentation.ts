import type { Place } from "./types";

// Local SVG resources shared by WebGL sprites, popup icons and the legend.
export const MAP_ICON_PATHS = {
  restaurants: "M5 3v6a3 3 0 0 0 6 0V3M8 3v18M18 3c-3 3-3 8 0 9h2M20 3v18",
  cafes: "M4 5h12v8a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5ZM16 6h2a3 3 0 0 1 0 6h-2M3 21h15",
  museums: "m3 8 9-5 9 5ZM5 10v8M10 10v8M14 10v8M19 10v8M3 21h18",
  attractions: "M5 21V8l4 2V4l6 2v5l4-2v12M3 21h18M9 21v-6h6v6",
  viewpoints: "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12ZM15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0",
  nature: "m2 20 8-15 5 9 3-5 4 11ZM7 11l3 3 3-3",
  other: "M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0ZM15 10a3 3 0 1 1-6 0 3 3 0 0 1 6 0",
  destination: "m3 20 7-14 5 9 3-5 4 10M3 20h19M10 6V2h7l-2 2 2 2Z",
} as const;
export type MapIconKind = keyof typeof MAP_ICON_PATHS;
export type MapFocusRequest = { placeId: string; sequence: number };

export function markerSvg(kind: MapIconKind, selected = false): string {
  const surface = selected ? "#1f4d3a" : "#f5f0e6";
  const ink = selected ? "#f5f0e6" : "#1f4d3a";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="80" height="88" viewBox="0 0 40 44"><path d="M20 43 14 35H26Z" fill="#142b20" opacity=".25"/><circle cx="20" cy="22" r="17" fill="#142b20" opacity=".25"/><path d="M20 41 14 33H26Z" fill="${surface}" stroke="${ink}" stroke-width="1.3"/><circle cx="20" cy="19" r="17" fill="${surface}" stroke="#f5f0e6" stroke-width="3"/><circle cx="20" cy="19" r="15.5" fill="none" stroke="${ink}" stroke-width="1.3"/><path d="${MAP_ICON_PATHS[kind]}" transform="translate(10 9) scale(.83)" fill="none" stroke="${ink}" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
}

export function validMapPlace(place: Place): boolean {
  return Number.isFinite(place.latitude) && Math.abs(place.latitude) <= 90
    && Number.isFinite(place.longitude) && Math.abs(place.longitude) <= 180;
}

export function selectedMapPlace(places: readonly Place[], id: string | null): Place | undefined {
  return places.find(place => place.id === id && validMapPlace(place));
}

export function shouldCloseMapPopup(popupId: string | null, selectedId: string | null, places: readonly Place[]): boolean {
  return popupId !== null && (popupId !== selectedId || !selectedMapPlace(places, popupId));
}

export function mapFitPadding(width: number, height: number) {
  // Leave room for the right-hand control rail without collapsing a small map.
  return { top: Math.min(72, height / 5), bottom: Math.min(72, height / 5), left: Math.min(56, width / 6), right: Math.min(100, width / 4) };
}

export function mapAnimationDuration(reducedMotion: boolean) { return reducedMotion ? 0 : 450; }

// Only explicit requests move the camera. Repeated data/selection synchronization
// never focuses again, including after the user has manually panned the map.
export function createMapFocusTracker() {
  let handledSequence: number | undefined;
  return (request: MapFocusRequest | null, places: readonly Place[]) => {
    if (!request || request.sequence === handledSequence) return undefined;
    const place = selectedMapPlace(places, request.placeId);
    if (place) handledSequence = request.sequence;
    return place;
  };
}

export function observeMapSize(element: HTMLElement, resize: () => void, Observer: typeof ResizeObserver = ResizeObserver) {
  let width = 0;
  let height = 0;
  const observer = new Observer(entries => {
    const size = entries[0]?.contentRect;
    if (!size) return;
    const changed = width !== size.width || height !== size.height;
    width = size.width;
    height = size.height;
    if (changed && width > 0 && height > 0) resize();
  });
  observer.observe(element);
  return () => observer.disconnect();
}
