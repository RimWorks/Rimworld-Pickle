import type { StepArg, StepStatus } from "./types";

export type RunEvent = {
  event: string;
  feature?: string;
  scenarios?: number;
  index?: number;
  name?: string;
  steps?: number;
  scenario?: number;
  keyword?: string;
  text?: string;
  status?: StepStatus;
  outcome?: string;
  durationMs?: number;
  failureMessage?: string | null;
  args?: StepArg[];
  passed?: number;
  failed?: number;
  message?: string;
};

export type RanStep = { keyword: string; text: string; status: StepStatus; durationMs: number; failureMessage: string | null; args?: StepArg[] };

export type RanScenario = {
  index: number;
  name: string;
  outcome: string | null;
  durationMs: number;
  failureMessage: string | null;
  steps: RanStep[];
};

export type Attempt = {
  id: number;
  startedAt: string;
  source: string;
  active: boolean;
  events: RunEvent[];
};

export type Folded = {
  feature: string;
  scenarios: RanScenario[];
  tally: string;
  error: string;
};

/**
 * Rebuilds the whole view from the event list. A prefix of a run folds to the state at that
 * moment, which is what lets a replayed poll and a live stream render the same way.
 */
export function fold(events: RunEvent[]): Folded {
  const folded: Folded = { feature: "", scenarios: [], tally: "", error: "" };

  for (const event of events) {
    const at = folded.scenarios.find((scenario) => scenario.index === (event.scenario ?? event.index));

    if (event.event === "run-started") {
      folded.feature = event.feature ?? "";
    } else if (event.event === "scenario-started") {
      folded.scenarios.push({
        index: event.index ?? folded.scenarios.length,
        name: event.name ?? "",
        outcome: null,
        durationMs: 0,
        failureMessage: null,
        steps: [],
      });
    } else if (event.event === "step" && at) {
      at.steps.push({
        keyword: (event.keyword ?? "").trim(),
        text: event.text ?? "",
        status: event.status ?? "Pending",
        durationMs: event.durationMs ?? 0,
        failureMessage: event.failureMessage ?? null,
        args: event.args,
      });
    } else if (event.event === "scenario" && at) {
      at.outcome = event.outcome ?? null;
      at.durationMs = event.durationMs ?? 0;
      at.failureMessage = event.failureMessage ?? null;
    } else if (event.event === "run-finished") {
      folded.tally = `${String(event.passed ?? 0)} passed, ${String(event.failed ?? 0)} failed`;
    } else if (event.event === "error") {
      folded.error = event.message ?? "the run failed";
    }
  }

  return folded;
}
