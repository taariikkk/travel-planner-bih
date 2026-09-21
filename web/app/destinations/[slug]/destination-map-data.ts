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
