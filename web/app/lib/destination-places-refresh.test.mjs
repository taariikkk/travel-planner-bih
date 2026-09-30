import assert from "node:assert/strict";
import test from "node:test";
import { createPlacesCoordinator, updatePlaceSource } from "../destinations/[slug]/destination-map-data.ts";

const place = id => ({ id, name: id, category: "attraction", latitude: 43.85, longitude: 18.4 });
const tick = () => new Promise(resolve => setImmediate(resolve));
function harness(slug = "sarajevo") {
  const calls = [];
  const states = [];
  let refreshes = 0;
  const coordinator = createPlacesCoordinator(slug, (path, init) => new Promise((resolve, reject) => {
    calls.push({ path, init, reject, reply: body => resolve({ ok: true, json: async () => body }) });
  }));
  return { calls, states, coordinator, get refreshes() { return refreshes; },
    mount: () => coordinator.mount(state => states.push(state), () => refreshes++),
    gets: () => calls.filter(call => call.init.method !== "POST"),
    posts: () => calls.filter(call => call.init.method === "POST") };
}

test("one POST per mount; Strict Mode setup/cleanup/setup reuses the pending POST", async () => {
  const h = harness();
  const cleanup = h.mount();
  cleanup();
  const stop = h.mount();
  assert.equal(h.posts().length, 1);
  assert.equal(h.gets()[0].init.signal.aborted, true);
  h.posts()[0].reply({ status: "refreshed", retryAt: null });
  await tick();
  assert.equal(h.refreshes, 1);
  assert.equal(h.gets().length, 3);
  h.gets()[2].reply([place("fresh")]);
  await tick();
  assert.equal(h.states.at(-1).places[0].id, "fresh");
  stop();
  const remount = harness();
  remount.mount()();
  assert.equal(remount.posts().length, 1);
});

test("responses for an old slug cannot publish places or refresh the new route", async () => {
  const old = harness("sarajevo");
  old.mount()();
  const current = harness("mostar");
  const stop = current.mount();
  current.gets()[0].reply([place("mostar")]);
  old.gets()[0].reply([place("sarajevo")]);
  old.posts()[0].reply({ status: "refreshed", retryAt: null });
  await tick();
  assert.equal(old.states.length, 0);
  assert.equal(old.refreshes, 0);
  assert.equal(old.gets().length, 1);
  assert.equal(current.states.at(-1).places[0].id, "mostar");
  stop();
});

test("refreshed updates the same GeoJSON source; a late initial GET cannot overwrite it", async () => {
  const h = harness();
  const source = { data: null, setData(data) { this.data = data; } };
  let refreshes = 0;
  const stop = h.coordinator.mount(state => updatePlaceSource(source, state.places), () => refreshes++);
  h.posts()[0].reply({ status: "refreshed", retryAt: null });
  await tick();
  assert.equal(h.gets()[0].init.signal.aborted, true);
  h.gets()[1].reply([place("new")]);
  await tick();
  h.gets()[0].reply([place("old")]);
  await tick();
  assert.equal(source.data.features[0].properties.id, "new");
  assert.equal(refreshes, 1);
  stop();
});

for (const status of ["cached", "deferred"]) {
  test(`${status} does not refetch, refresh the route, or retry the POST`, async () => {
    const h = harness();
    const stop = h.mount();
    h.posts()[0].reply({ status, retryAt: "2026-10-01T00:00:00Z" });
    h.gets()[0].reply([place("existing")]);
    await tick();
    await tick();
    assert.equal(h.calls.length, 2);
    assert.equal(h.refreshes, 0);
    assert.equal(h.states.at(-1).places[0].id, "existing");
    stop();
  });
}

test("failed import preserves existing places", async () => {
  const h = harness();
  const stop = h.mount();
  h.gets()[0].reply([place("existing")]);
  await tick();
  h.posts()[0].reject(new Error("Network failure"));
  await tick();
  assert.equal(h.states.at(-1).places[0].id, "existing");
  assert.equal(h.refreshes, 0);
  stop();
});

test("failed post-refresh GET preserves existing places and does not repeat import", async () => {
  const h = harness();
  const stop = h.mount();
  h.gets()[0].reply([place("existing")]);
  await tick();
  h.posts()[0].reply({ status: "refreshed", retryAt: null });
  await tick();
  h.gets()[1].reject(new Error("Network failure"));
  await tick();
  assert.equal(h.states.at(-1).places[0].id, "existing");
  assert.equal(h.states.at(-1).status, "error");
  assert.equal(h.posts().length, 1);
  assert.equal(h.refreshes, 1);
  stop();
});

test("cleanup during JSON parsing ignores late data and aborts GET", async () => {
  let resolveBody;
  const states = [];
  let signal;
  const coordinator = createPlacesCoordinator("sarajevo", async (path, init) => {
    if (init.method === "POST") return { ok: true, json: async () => ({ status: "cached", retryAt: null }) };
    signal = init.signal;
    return { ok: true, json: () => new Promise(resolve => { resolveBody = resolve; }) };
  });
  const stop = coordinator.mount(state => states.push(state), () => assert.fail("unexpected refresh"));
  await tick();
  stop();
  resolveBody([place("late")]);
  await tick();
  assert.equal(signal.aborted, true);
  assert.deepEqual(states, []);
});
