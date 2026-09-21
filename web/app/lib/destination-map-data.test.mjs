import assert from "node:assert/strict";
import test from "node:test";
import { createClusteredPlaceSource, getMapBoundsCoordinates } from "../destinations/[slug]/destination-map-data.ts";

const denseSarajevoPlaces = Array.from({ length: 80 }, (_, index) => ({
  id: `place-${index}`,
  name: `Mjesto ${index}`,
  category: index % 2 ? "restaurant" : "attraction",
  latitude: 43.84 + index * 0.0001,
  longitude: 18.39 + index * 0.0001,
}));

test("builds a clustered GeoJSON source for every place in a dense destination", () => {
  const source = createClusteredPlaceSource(denseSarajevoPlaces);

  assert.equal(source.type, "geojson");
  assert.equal(source.cluster, true);
  assert.equal(source.clusterMaxZoom, 14);
  assert.equal(source.clusterRadius, 50);
  assert.equal(source.data.features.length, 80);
  assert.deepEqual(source.data.features[1].properties, { id: "place-1", name: "Mjesto 1", category: "restaurant" });
});

test("fit bounds coordinates include the destination and all valid places", () => {
  const places = [...denseSarajevoPlaces, { id: "invalid", name: "Invalid", category: "attraction", latitude: 200, longitude: 18.4 }];
  const coordinates = getMapBoundsCoordinates({ latitude: 43.8563, longitude: 18.4131 }, places);

  assert.equal(coordinates.length, 81);
  assert.deepEqual(coordinates[0], [18.4131, 43.8563]);
  assert.deepEqual(coordinates.at(-1), [denseSarajevoPlaces.at(-1).longitude, denseSarajevoPlaces.at(-1).latitude]);
});
