import type { Place } from "./types";

export const PLACE_GROUPS = ["all", "restaurants", "cafes", "attractions", "museums", "viewpoints", "nature"] as const;
export type PlaceGroup = (typeof PLACE_GROUPS)[number];
const BALANCED_GROUPS: Exclude<PlaceGroup, "all">[] = ["restaurants", "cafes", "attractions", "museums", "viewpoints", "nature"];

const CATEGORY_GROUP: Record<string, Exclude<PlaceGroup, "all">> = {
  restaurant: "restaurants",
  cafe: "cafes",
  museum: "museums",
  viewpoint: "viewpoints",
  peak: "nature",
  waterfall: "nature",
  cave: "nature",
  attraction: "attractions",
  historic: "attractions",
  gallery: "attractions",
  zoo: "attractions",
};

export function getPlaceGroup(category: string): Exclude<PlaceGroup, "all"> {
  return CATEGORY_GROUP[category.toLocaleLowerCase("bs")] ?? "attractions";
}

export function normalizePlaceSearch(value: string): string {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("bs").trim();
}

export function sortPlaces(places: readonly Place[], language: "bs" | "en" = "bs"): Place[] {
  return [...places].sort((left, right) => {
    const leftDistance = left.distanceKm ?? Number.POSITIVE_INFINITY;
    const rightDistance = right.distanceKm ?? Number.POSITIVE_INFINITY;
    return leftDistance - rightDistance || left.name.localeCompare(right.name, language);
  });
}

export function filterPlaces(places: readonly Place[], group: PlaceGroup, query: string): Place[] {
  const normalizedQuery = normalizePlaceSearch(query);
  return places.filter((place) => {
    if (group !== "all" && getPlaceGroup(place.category) !== group) return false;
    if (!normalizedQuery) return true;
    const metadata = place.metadata;
    return [place.name, metadata?.address, metadata?.cuisine]
      .some((value) => value && normalizePlaceSearch(value).includes(normalizedQuery));
  });
}

export function countPlaceGroups(places: readonly Place[]): Record<PlaceGroup, number> {
  const counts: Record<PlaceGroup, number> = { all: places.length, restaurants: 0, cafes: 0, attractions: 0, museums: 0, viewpoints: 0, nature: 0 };
  for (const place of places) counts[getPlaceGroup(place.category)]++;
  return counts;
}

export function selectBalancedPlaces(places: readonly Place[], limit = 6): Place[] {
  if (limit <= 0) return [];
  const sorted = sortPlaces(places);
  const buckets = new Map<Exclude<PlaceGroup, "all">, Place[]>();
  for (const group of BALANCED_GROUPS) buckets.set(group, []);
  for (const place of sorted) buckets.get(getPlaceGroup(place.category))!.push(place);

  const selected: Place[] = [];
  let offset = 0;
  while (selected.length < limit) {
    let added = false;
    for (const group of BALANCED_GROUPS) {
      const place = buckets.get(group)![offset];
      if (!place) continue;
      selected.push(place);
      added = true;
      if (selected.length === limit) return selected;
    }
    if (!added) break;
    offset++;
  }
  return selected;
}
