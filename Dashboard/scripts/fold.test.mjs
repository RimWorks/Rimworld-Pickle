import assert from "node:assert/strict";
import test from "node:test";
import { fold } from "../src/runEvents.ts";

const run = [
  { event: "run-started", feature: "ad hoc", scenarios: 2 },
  { event: "scenario-started", index: 0, name: "first", steps: 1 },
  { event: "step", scenario: 0, index: 0, keyword: "Then ", text: "it passes", status: "Passed", durationMs: 1, failureMessage: null },
  { event: "scenario", index: 0, name: "first", outcome: "Passed", durationMs: 3, failureMessage: null },
  { event: "scenario-started", index: 1, name: "second", steps: 1 },
  { event: "step", scenario: 1, index: 0, keyword: "Then ", text: "it fails", status: "Failed", durationMs: 70, failureMessage: "no def named 'X'" },
  { event: "scenario", index: 1, name: "second", outcome: "Failed", durationMs: 74, failureMessage: "no def named 'X'" },
  { event: "run-finished", passed: 1, failed: 1 },
];

test("a whole run folds to the feature, both scenarios and the tally", () => {
  const view = fold(run);
  assert.equal(view.feature, "ad hoc");
  assert.equal(view.tally, "1 passed, 1 failed");
  assert.equal(view.scenarios.length, 2);
  assert.equal(view.scenarios[1].outcome, "Failed");
  assert.equal(view.scenarios[1].failureMessage, "no def named 'X'");
});

test("a late step lands on the scenario it names, not the newest one", () => {
  const outOfOrder = [
    { event: "scenario-started", index: 0, name: "first", steps: 2 },
    { event: "scenario-started", index: 1, name: "second", steps: 1 },
    { event: "step", scenario: 0, index: 1, keyword: "And", text: "late", status: "Passed", durationMs: 1 },
  ];

  const view = fold(outOfOrder);
  assert.deepEqual(view.scenarios.map((s) => s.steps.map((step) => step.text)), [["late"], []]);
});

test("the keyword loses gherkin's trailing space", () => {
  assert.equal(fold(run).scenarios[0].steps[0].keyword, "Then");
});

test("folding a prefix of the run gives the state at that moment", () => {
  const view = fold(run.slice(0, 3));
  assert.equal(view.tally, "");
  assert.equal(view.scenarios.length, 1);
  assert.equal(view.scenarios[0].outcome, null);
  assert.equal(view.scenarios[0].steps.length, 1);
});

test("folding the same events twice gives the same view", () => {
  assert.deepEqual(fold(run), fold(run));
});

test("an error event surfaces and no scenarios are invented", () => {
  const view = fold([{ event: "error", message: "(2:1): expected something" }]);
  assert.equal(view.error, "(2:1): expected something");
  assert.equal(view.scenarios.length, 0);
});

test("nothing folds to an empty view", () => {
  assert.deepEqual(fold([]), { feature: "", scenarios: [], tally: "", error: "" });
});
