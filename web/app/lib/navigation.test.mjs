import assert from "node:assert/strict";
import test from "node:test";
import { getNavigationItems } from "./navigation.ts";

test("returns public navigation for a signed-out visitor", () => {
  assert.deepEqual(getNavigationItems(false), [
    { key: "how", href: "/#kako-funkcionise" },
    { key: "publicExplore", href: "/explore" },
    { key: "login", href: "/login" },
    { key: "register", href: "/register" },
  ]);
});

test("returns application navigation for a signed-in user", () => {
  assert.deepEqual(getNavigationItems(true), [
    { key: "dashboard", href: "/dashboard" },
    { key: "explore", href: "/explore" },
    { key: "recommendations", href: "/recommendations" },
    { key: "trips", href: "/trips" },
    { key: "budget", href: "/budget" },
    { key: "settings", href: "/settings" },
  ]);
});
