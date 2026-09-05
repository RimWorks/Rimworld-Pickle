import assert from "node:assert/strict";
import test from "node:test";
import { countOutcomes } from "../src/types.ts";

test("completed, skipped, and active scenarios match the in-game counts", () => {
  assert.deepEqual(countOutcomes([]), { total: 0, passed: 0, failed: 0, skipped: 0, notRun: 0 });
  assert.deepEqual(countOutcomes([
    { outcome: "Passed" }, { outcome: "Failed" }, { outcome: "Skipped" },
    { outcome: "Pending" }, { outcome: "Running" },
  ]), { total: 5, passed: 1, failed: 1, skipped: 1, notRun: 2 });
});
