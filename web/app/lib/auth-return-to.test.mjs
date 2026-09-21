import assert from "node:assert/strict";
import test from "node:test";
import { getAuthModeHref, getSafeReturnTo } from "./auth-return-to.ts";

test("accepts only internal return paths", () => {
  assert.equal(getSafeReturnTo("/destinations/mostar"), "/destinations/mostar");
  assert.equal(getSafeReturnTo("/destinations/mostar?source=favorite"), "/destinations/mostar?source=favorite");
  assert.equal(getSafeReturnTo("https://example.test"), "/recommendations");
  assert.equal(getSafeReturnTo("//example.test"), "/recommendations");
  assert.equal(getSafeReturnTo("/\\example.test"), "/recommendations");
  assert.equal(getSafeReturnTo(null), "/recommendations");
});

test("keeps a safe return path while switching auth modes", () => {
  assert.equal(getAuthModeHref("/register", "/destinations/mostar"), "/register?returnTo=%2Fdestinations%2Fmostar");
  assert.equal(getAuthModeHref("/login", "/recommendations"), "/login");
});
