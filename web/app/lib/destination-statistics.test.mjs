import assert from "node:assert/strict";
import test from "node:test";
import { formatDestinationStatistics } from "../destinations/[slug]/destination-statistics.ts";

const labels = { population: "Stanovnici", elevation: "Nadmorska visina", distance: "Udaljenost", region: "Regija" };

test("omits unavailable destination statistics", () => {
  assert.deepEqual(formatDestinationStatistics({ region: "Hercegovina", population: null, elevationMeters: null, distanceFromSarajevoKm: null }, "bs", labels), [
    { label: "Regija", value: "Hercegovina" },
  ]);
});

test("formats available population, elevation and distance for the selected language", () => {
  const statistics = formatDestinationStatistics({ region: "Hercegovina", population: 105_797, elevationMeters: 60.5, distanceFromSarajevoKm: 129.75 }, "en", labels);
  assert.deepEqual(statistics, [
    { label: "Stanovnici", value: "105,797" },
    { label: "Nadmorska visina", value: "60.5 m" },
    { label: "Udaljenost", value: "129.8 km" },
    { label: "Regija", value: "Hercegovina" },
  ]);
});
