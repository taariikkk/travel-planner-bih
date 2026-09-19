import assert from "node:assert/strict";
import test from "node:test";
import { formatTagLabel, text } from "./i18n.ts";
import { formatRecommendationReason } from "./recommendation-reason.ts";

function reasonFor(reasons, language) {
  return formatRecommendationReason(
    reasons,
    text[language].rec.recommendationReason,
    (tag) => formatTagLabel(tag, language),
  );
}

test("formats one matching criterion in Bosnian", () => {
  assert.equal(
    reasonFor({ matchingTags: ["planina"], matchesSeason: false, matchesBudget: false, matchesDuration: false }, "bs"),
    "Odgovara tvojim interesovanjima (planina).",
  );
});

test("formats two matching interests in English", () => {
  assert.equal(
    reasonFor({ matchingTags: ["planina", "priroda"], matchesSeason: false, matchesBudget: false, matchesDuration: false }, "en"),
    "It suits your interests (mountain and nature).",
  );
});

test("formats three or more criteria with localized punctuation", () => {
  assert.equal(
    reasonFor({ matchingTags: ["planina", "priroda"], matchesSeason: true, matchesBudget: true, matchesDuration: true }, "bs"),
    "Odgovara tvojim interesovanjima (planina i priroda), odabranoj sezoni, budžetu i trajanju putovanja.",
  );
  assert.equal(
    reasonFor({ matchingTags: ["planina", "priroda"], matchesSeason: true, matchesBudget: true, matchesDuration: true }, "en"),
    "It suits your interests (mountain and nature), the selected season, your budget, and your trip length.",
  );
});

test("mentions only the criteria that matched and has a no-match fallback", () => {
  assert.equal(
    reasonFor({ matchingTags: [], matchesSeason: true, matchesBudget: false, matchesDuration: true }, "bs"),
    "Odgovara odabranoj sezoni i trajanju putovanja.",
  );
  assert.equal(
    reasonFor({ matchingTags: [], matchesSeason: false, matchesBudget: false, matchesDuration: false }, "en"),
    "A curated starting suggestion for your trip.",
  );
});
