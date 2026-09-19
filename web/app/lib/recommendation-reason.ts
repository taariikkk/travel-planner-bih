import type { RecommendationReasons } from "./api";

export type RecommendationReasonCopy = {
  prefix: string;
  fallback: string;
  interests: (tags: string) => string;
  season: string;
  budget: string;
  duration: string;
  conjunction: string;
  usesSerialComma: boolean;
};

function joinNaturalList(items: string[], conjunction: string, usesSerialComma: boolean) {
  if (items.length === 1) return items[0];
  if (items.length === 2) return `${items[0]} ${conjunction} ${items[1]}`;

  const beginning = items.slice(0, -1).join(", ");
  return `${beginning}${usesSerialComma ? "," : ""} ${conjunction} ${items.at(-1)}`;
}

export function formatRecommendationReason(
  reasons: RecommendationReasons,
  copy: RecommendationReasonCopy,
  formatTagLabel: (tag: string) => string,
) {
  const criteria: string[] = [];

  if (reasons.matchingTags.length > 0) {
    const tags = reasons.matchingTags.map(formatTagLabel);
    criteria.push(copy.interests(joinNaturalList(tags, copy.conjunction, copy.usesSerialComma)));
  }
  if (reasons.matchesSeason) criteria.push(copy.season);
  if (reasons.matchesBudget) criteria.push(copy.budget);
  if (reasons.matchesDuration) criteria.push(copy.duration);

  return criteria.length > 0
    ? `${copy.prefix} ${joinNaturalList(criteria, copy.conjunction, copy.usesSerialComma)}.`
    : copy.fallback;
}
