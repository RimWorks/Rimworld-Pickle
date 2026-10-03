import type { ReactNode } from "react";
import { formatMs } from "./types";
import type { StepArg, StepStatus } from "./types";

type Listed = {
  keyword: string;
  text: string;
  status: StepStatus;
  durationMs: number;
  failureMessage?: string | null;
  args?: StepArg[];
};

const dotTone: Record<StepStatus, string> = {
  Passed: "bg-success",
  Failed: "bg-error",
  Undefined: "bg-error",
  Ambiguous: "bg-error",
  Skipped: "bg-base-content/25",
  Pending: "bg-base-content/25",
};

const barTone: Record<StepStatus, string> = {
  Passed: "bg-success",
  Failed: "bg-error",
  Undefined: "bg-error",
  Ambiguous: "bg-error",
  Skipped: "bg-base-content/15",
  Pending: "bg-base-content/15",
};

function continuesTheClauseAbove(keyword: string): boolean {
  return keyword === "And" || keyword === "But";
}

function withArgumentsMarked(step: Listed): ReactNode {
  const spans = [...(step.args ?? [])].filter((arg) => arg.length > 0).sort((a, b) => a.start - b.start);
  if (spans.length === 0) return step.text;

  const out: ReactNode[] = [];
  let at = 0;

  for (const span of spans) {
    if (span.start < at || span.start + span.length > step.text.length) continue;
    if (span.start > at) out.push(step.text.slice(at, span.start));
    out.push(
      <span key={span.start} className="step-arg">
        {step.text.slice(span.start, span.start + span.length)}
      </span>,
    );
    at = span.start + span.length;
  }

  if (at < step.text.length) out.push(step.text.slice(at));
  return out;
}

export function StepList({ steps }: Readonly<{ steps: readonly Listed[] }>) {
  return (
    <ol className="py-1">
      {steps.map((step, i) => {
        const tucked = continuesTheClauseAbove(step.keyword);
        return (
          <li
            key={`${String(i)}-${step.keyword}-${step.text}`}
            className={`grid grid-cols-[52px_7px_1fr_auto] items-baseline gap-2.5 px-4 hover:bg-base-200 ${
              tucked ? "pt-px pb-1.5" : "py-1.5 not-first:border-t not-first:border-base-content/10"
            }`}
          >
            <span className={`font-mono text-xs text-right text-base-content/55 ${tucked ? "opacity-45" : "font-semibold"}`}>
              {step.keyword}
            </span>
            <span className={`w-[7px] h-[7px] rounded-full relative -top-px ${dotTone[step.status]}`} />
            <span className={`text-sm break-words ${step.status === "Skipped" || step.status === "Pending" ? "opacity-55" : ""}`}>
              {withArgumentsMarked(step)}
            </span>
            <span className="font-mono text-xs tabular-nums text-base-content/50 min-w-12 text-right">
              {formatMs(step.durationMs)}
            </span>
            {step.failureMessage && (
              <pre className="col-start-3 col-span-2 my-1 rounded-box bg-error/10 p-2 text-xs text-error whitespace-pre-wrap break-words">
                {step.failureMessage}
              </pre>
            )}
          </li>
        );
      })}
    </ol>
  );
}

export function StepProgress({ steps }: Readonly<{ steps: readonly Listed[] }>) {
  return (
    <div className="flex gap-[3px] mt-2.5">
      {steps.map((step, i) => (
        <span key={`${String(i)}-${step.keyword}`} className={`block w-4 h-[3px] rounded-sm ${barTone[step.status]}`} />
      ))}
    </div>
  );
}

export function StepCard({ steps, children }: Readonly<{ steps: readonly Listed[]; children: ReactNode }>) {
  return (
    <article className="rounded-box border border-base-content/15 bg-base-100 overflow-hidden">
      <div className="p-4 bg-base-200 border-b border-base-content/15">
        {children}
        <StepProgress steps={steps} />
      </div>
      <StepList steps={steps} />
    </article>
  );
}
