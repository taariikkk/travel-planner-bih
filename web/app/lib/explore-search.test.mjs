import assert from "node:assert/strict";
import test from "node:test";
import { buildExploreHref, parseExploreSearchParams } from "../explore/search-params.ts";
import { formatDestinationType } from "./i18n.ts";

test("parses the first URL value and trims filters", () => {
  assert.deepEqual(parseExploreSearchParams({ q: ["  Mostar ", "ignored"], type: " grad ", region: undefined }), {
    q: "Mostar", type: "grad", region: "",
  });
});

test("builds shareable explore URLs and drops empty filters", () => {
  assert.equal(buildExploreHref({ q: "Banja Luka", type: "grad", region: "" }), "/explore?q=Banja+Luka&type=grad");
  assert.equal(buildExploreHref({}), "/explore");
});

test("localizes destination type labels", () => {
  assert.equal(formatDestinationType("planina", "bs"), "Planina");
  assert.equal(formatDestinationType("selo", "en"), "Village");
});
