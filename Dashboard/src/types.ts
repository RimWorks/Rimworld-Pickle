export type StepStatus = "Pending" | "Passed" | "Failed" | "Skipped";
export type Outcome = "Pending" | "Running" | "Passed" | "Failed" | "Skipped";

export type Step = {
  keyword: string;
  text: string;
  status: StepStatus;
  durationMs: number;
  failureMessage: string | null;
};

export type Attachment = { name: string; content: string };
export type StateDump = { source: string; content: string };

export type Scenario = {
  name: string;
  index: number;
  selected: boolean;
  line: number;
  tags: string[];
  outcome: Outcome;
  durationMs: number;
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
  watch: boolean;
  breakOnFailure: boolean;
  includeWip: boolean;
  controllable: boolean;
  features: Feature[];
};

export type Selection = { path: string; index: number };

export const statusTone: Record<StepStatus, string> = {
  Passed: "text-success",
  Failed: "text-error",
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
