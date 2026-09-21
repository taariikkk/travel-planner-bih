import test from "node:test";
import assert from "node:assert/strict";
import { shouldRenderNeutralShell } from "../components/app-shell-state.ts";

test("the first client render stays neutral until hydration completes", () => {
  assert.equal(shouldRenderNeutralShell(false, "guest", false), true);
  assert.equal(shouldRenderNeutralShell(false, "authenticated", false), true);
  assert.equal(shouldRenderNeutralShell(true, "guest", false), false);
  assert.equal(shouldRenderNeutralShell(true, "guest", true), true);
});
