export type StepStatus = "Pending" | "Passed" | "Failed" | "Skipped" | "Undefined" | "Ambiguous";
export type Outcome = "Pending" | "Running" | "Passed" | "Failed" | "Skipped";

export type Step = {
  keyword: string;
  text: string;
  status: StepStatus;
  durationMs: number;
  failureMessage: string | null;
};

export type Attachment = { name: string; content: string };
export type FailedAttempt = { attempt: number; message: string | null };
export type TickCost = { ticks: number; meanMs: number; maxMs: number };
export type StateDump = { source: string; content: string };

export type Scenario = {
  name: string;
  index: number;
  selected: boolean;
  visible?: boolean;
  line: number;
  tags: string[];
  outcome: Outcome;
  durationMs: number;
  attempts?: number;
  failedAttempts?: FailedAttempt[];
  tickCost?: TickCost | null;
  failureMessage: string | null;
  logTail: string[];
  attachments: Attachment[];
  stateDumps: StateDump[];
  steps: Step[];
};

export type Feature = {
  name: string;
  mod: string;
  path: string;
  tags: string[];
  scenarios: Scenario[];
  counts?: ReturnType<typeof countOutcomes>;
};

export type Snapshot = {
  strings?: Record<string, string>;
  status: "idle" | "running" | "paused";
  feature: string;
  scenario: string;
  step: string;
  passed: number;
  failed: number;
  cancelRequested: boolean;
  pauseRequested: boolean;
  runScope: "all" | "selected" | "failed";
  runTotal: number;
  runCompleted: number;
  fixtureBusy: boolean;
  watch: boolean;
  breakOnFailure: boolean;
  includeWip: boolean;
  showRunPill: boolean;
  controllable: boolean;
  search?: string;
  modFilter?: string | null;
  tagFilters?: string[];
  lastRunAt?: string | null;
  features: Feature[];
};

export type Selection = { path: string; index: number };

export function countOutcomes(scenarios: readonly Pick<Scenario, "outcome">[]) {
  const counts = { total: scenarios.length, passed: 0, failed: 0, skipped: 0, notRun: 0 };
  for (const scenario of scenarios) {
    switch (scenario.outcome) {
      case "Passed": counts.passed++; break;
      case "Failed": counts.failed++; break;
      case "Skipped": counts.skipped++; break;
      default: counts.notRun++;
    }
  }
  return counts;
}

export const statusTone: Record<StepStatus, string> = {
  Passed: "text-success",
  Failed: "text-error",
  Undefined: "text-error",
  Ambiguous: "text-error",
  Skipped: "text-base-content/40",
  Pending: "text-base-content/40",
};

export const outcomeDot: Record<Outcome, string> = {
  Running: "status-info",
  Passed: "status-success",
  Failed: "status-error",
  Skipped: "status-neutral",
  Pending: "status-neutral",
};

// Passed, but not first time. One definition, matching RunOutcomes.IsFlaky in the game.
export function isFlaky(scenario: { outcome: Outcome; attempts?: number }): boolean {
  return scenario.outcome === "Passed" && (scenario.attempts ?? 1) > 1;
}

export function formatMs(ms: number): string {
  if (ms <= 0) return "";
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

// The game sends the active language from Languages/<lang>/Keyed/Pickle.xml. A missing
// or empty value falls back to the English text passed at the call site.
export function translator(snap: Snapshot | null) {
  return (key: string, fallback: string): string => snap?.strings?.[key] || fallback;
}
