import assert from "node:assert/strict";
import test from "node:test";
import { countPlaceGroups, filterPlaces, getPlaceGroup, normalizePlaceSearch, selectBalancedPlaces, sortPlaces } from "../destinations/[slug]/place-explorer-data.ts";

const place = (id, category, distanceKm, metadata = null) => ({ id, name: id, category, distanceKm, latitude: 43.85, longitude: 18.4, metadata });

test("normalizes Bosnian diacritics and searches name, address, and cuisine", () => {
  const places = [place("Bjelašnica", "peak", 2, { address: "Planinska cesta", cuisine: null }), place("Ćevabdžinica", "restaurant", 1, { address: null, cuisine: "domaća" })];
  assert.equal(normalizePlaceSearch("  BJELAŠNICA "), "bjelasnica");
  assert.deepEqual(filterPlaces(places, "all", "bjelasnica").map(item => item.id), ["Bjelašnica"]);
  assert.deepEqual(filterPlaces(places, "restaurants", "domaca").map(item => item.id), ["Ćevabdžinica"]);
});

test("maps detailed categories into the seven user-facing groups and counts them", () => {
  assert.equal(getPlaceGroup("historic"), "attractions");
  assert.equal(getPlaceGroup("gallery"), "attractions");
  assert.equal(getPlaceGroup("zoo"), "attractions");
  assert.equal(getPlaceGroup("peak"), "nature");
  assert.equal(getPlaceGroup("waterfall"), "nature");
  assert.equal(getPlaceGroup("cave"), "nature");
  const counts = countPlaceGroups([place("r", "restaurant", 1), place("c", "cafe", 2), place("m", "museum", 3), place("v", "viewpoint", 4), place("p", "peak", 5)]);
  assert.deepEqual(counts, { all: 5, restaurants: 1, cafes: 1, attractions: 0, museums: 1, viewpoints: 1, nature: 1 });
});

test("combines category and text filters", () => {
  const places = [place("Muzej Sarajeva", "museum", 1), place("Sarajevo restoran", "restaurant", 2), place("Muzej Mostara", "museum", 3)];
  assert.deepEqual(filterPlaces(places, "museums", "sarajeva").map(item => item.id), ["Muzej Sarajeva"]);
  assert.deepEqual(filterPlaces(places, "cafes", "").map(item => item.id), []);
});

test("sorts by distance with nulls last and balances the six-place preview", () => {
  const places = sortPlaces([place("R2", "restaurant", 4), place("A1", "attraction", 1), place("R1", "restaurant", 2), place("M1", "museum", 3), place("N1", "peak", null), place("C1", "cafe", 2.5), place("V1", "viewpoint", 3.5)]);
  assert.equal(places.at(-1).id, "N1");
  const preview = selectBalancedPlaces(places, 6);
  assert.deepEqual(preview.map(item => getPlaceGroup(item.category)), ["restaurants", "cafes", "attractions", "museums", "viewpoints", "nature"]);
});
