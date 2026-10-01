import assert from "node:assert/strict";
import test from "node:test";
import { createMapFocusTracker, mapAnimationDuration, mapFitPadding, markerSvg, observeMapSize, selectedMapPlace, shouldCloseMapPopup, validMapPlace } from "../destinations/[slug]/map-presentation.ts";
import { getPlaceMarkerKind } from "../destinations/[slug]/place-explorer-data.ts";
import { createClusteredPlaceSource, createPlaceFeatureCollection, getMapBoundsCoordinates } from "../destinations/[slug]/destination-map-data.ts";

const place = (id, category = "restaurant", latitude = 43.85, longitude = 18.4) => ({ id, name: id, category, latitude, longitude });

test("category icons follow existing groups, support legacy labels, and distinguish unknown categories", () => {
  for (const [category, kind] of Object.entries({ restaurant: "restaurants", restoran: "restaurants", CAFE: "cafes", museum: "museums", historic: "attractions", gallery: "attractions", zoo: "attractions", atrakcija: "attractions", viewpoint: "viewpoints", peak: "nature", cave: "nature", waterfall: "nature", unknown: "other" })) {
    assert.equal(getPlaceMarkerKind(category), kind);
    assert.match(markerSvg(kind), /^<svg/);
    assert.notEqual(markerSvg(kind), markerSvg(kind, true));
  }
  assert.equal(getPlaceMarkerKind("__proto__"), "other");
});

test("filtering removes stale popups without discarding a popup for a refreshed selected place", () => {
  const points = [place("first"), place("second")];
  assert.equal(shouldCloseMapPopup("first", "first", points), false);
  assert.equal(shouldCloseMapPopup("first", "first", points.map(item => ({ ...item }))), false);
  assert.equal(shouldCloseMapPopup("first", "second", points), true);
  assert.equal(shouldCloseMapPopup("first", "first", [points[1]]), true);
  assert.equal(shouldCloseMapPopup("first", "first", [place("first", "cafe", NaN)]), true);
  assert.equal(shouldCloseMapPopup(null, "first", points), false);
});

test("selected point remains available independently of the clustered source", () => {
  const points = Array.from({ length: 100 }, (_, i) => place(`dense-${i}`));
  const clustered = createClusteredPlaceSource(points);
  const selected = selectedMapPlace(points, "dense-57");
  const overlay = createPlaceFeatureCollection(selected ? [selected] : []);
  assert.equal(clustered.cluster, true);
  assert.equal(clustered.data.features.length, 100);
  assert.equal(overlay.features.length, 1);
  assert.equal(overlay.features[0].properties.id, "dense-57");
  assert.equal(selectedMapPlace(points.filter(item => item.id !== "dense-57"), "dense-57"), undefined);
});

test("invalid coordinates cannot create an overlay or camera focus", () => {
  const points = [place("nan", "cafe", NaN), place("lat", "cafe", 91), place("lng", "cafe", 43, -181), place("infinity", "cafe", Infinity), place("valid")];
  assert.deepEqual(points.filter(validMapPlace).map(item => item.id), ["valid"]);
  assert.equal(selectedMapPlace(points, "nan"), undefined);
  assert.equal(createMapFocusTracker()({ placeId: "lat", sequence: 1 }, points), undefined);
  assert.equal(getMapBoundsCoordinates({ latitude: 43, longitude: 18 }, points).length, 2);
});

test("data refresh and automatic selection changes do not move the camera", () => {
  const focus = createMapFocusTracker();
  const points = [place("first"), place("second")];
  assert.equal(focus(null, points), undefined);
  assert.equal(focus({ placeId: "first", sequence: 1 }, points)?.id, "first");
  assert.equal(focus({ placeId: "first", sequence: 1 }, [...points]), undefined);
  assert.equal(focus({ placeId: "first", sequence: 1 }, [points[1]]), undefined);
  assert.equal(focus({ placeId: "first", sequence: 1 }, points), undefined);
  // Re-clicking the same row is an explicit new focus request.
  assert.equal(focus({ placeId: "first", sequence: 2 }, points)?.id, "first");
});

test("an initial explicit request can focus when asynchronously loaded places arrive", () => {
  const focus = createMapFocusTracker();
  const request = { placeId: "later", sequence: 0 };
  assert.equal(focus(request, []), undefined);
  assert.equal(focus(request, [place("later")])?.id, "later");
  assert.equal(focus(request, [place("later")]), undefined);
});

test("empty map retains destination and padding leaves usable room on small screens", () => {
  assert.deepEqual(getMapBoundsCoordinates({ latitude: 43, longitude: 18 }, []), [[18, 43]]);
  for (const [width, height] of [[300, 420], [720, 560], [240, 180]]) {
    const padding = mapFitPadding(width, height);
    assert.ok(padding.left + padding.right < width);
    assert.ok(padding.top + padding.bottom < height);
    assert.ok(padding.right >= padding.left);
  }
  assert.equal(mapAnimationDuration(true), 0);
  assert.equal(mapAnimationDuration(false), 450);
});

test("map resizes on mobile list-to-map transitions, size changes, and cleans up observer", () => {
  let callback;
  let observed;
  let disconnected = false;
  class Observer {
    constructor(fn) { callback = fn; }
    observe(element) { observed = element; }
    disconnect() { disconnected = true; }
  }
  const element = {};
  let calls = 0;
  const stop = observeMapSize(element, () => calls++, Observer);
  const size = (width, height) => callback([{ contentRect: { width, height } }]);
  assert.equal(observed, element);
  size(0, 0);
  assert.equal(calls, 0);
  size(360, 420);
  size(360, 420);
  assert.equal(calls, 1);
  size(0, 0);
  size(360, 420);
  size(720, 560);
  assert.equal(calls, 3);
  stop();
  assert.equal(disconnected, true);
});
