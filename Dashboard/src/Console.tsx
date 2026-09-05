import { useEffect, useRef, useState } from "react";
import { formatMs, statusTone } from "./types";
import type { StepStatus } from "./types";

type Catalogued = { pattern: string; kind: string; source: string };
type Dump = { source: string; content: string };
type Ran = {
  keyword: string;
  text: string;
  status: StepStatus;
  durationMs: number;
  failureMessage: string | null;
  skeleton: string | null;
  stateDumps: Dump[];
};

async function request<T>(path: string, mutate = false): Promise<T> {
  const response = await fetch(new URL(path, window.location.origin), mutate ? { method: "POST", body: "" } : { cache: "no-store" });
  const body = await response.json();
  if (!response.ok) throw new Error(body.error ?? `Request failed (${response.status})`);
  return body as T;
}

export function Console({ running, onClose }: Readonly<{ running: boolean; onClose: () => void }>) {
  const [steps, setSteps] = useState<Catalogued[]>([]);
  const [text, setText] = useState("");
  const [ran, setRan] = useState<Ran[]>([]);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  // -1 is the line being typed. Up walks back through what was already run.
  const [recalled, setRecalled] = useState(-1);
  const input = useRef<HTMLInputElement>(null);

  useEffect(() => {
    let alive = true;
    request<{ steps: Catalogued[] }>("/steps")
      .then((body) => { if (alive) setSteps(body.steps); })
      .catch((failure: Error) => { if (alive) setError(failure.message); });
    return () => { alive = false; };
  }, []);

  useEffect(() => { input.current?.focus(); }, []);

  const run = async () => {
    const typed = text.trim();
    if (!typed) return;
    setPending(true);
    setError("");
    setNotice("");
    try {
      const query = new URLSearchParams({ text: typed });
      setRan([await request<Ran>(`/step?${query}`, true), ...ran]);
      setText("");
      setRecalled(-1);
    } catch (failure) { setError(String(failure)); }
    finally { setPending(false); }
  };

  const reset = async () => {
    setPending(true);
    setError("");
    try {
      await request("/step/reset", true);
      setNotice("Context reset. The next step starts clean.");
    } catch (failure) { setError(String(failure)); }
    finally { setPending(false); }
  };

  // The same assert gets run against different values, so history beats retyping.
  const recall = (delta: number) => {
    const next = Math.min(Math.max(recalled + delta, -1), ran.length - 1);
    setRecalled(next);
    setText(next < 0 ? "" : ran[next].text);
  };

  const disabled = running || pending;

  return (
    <section className="max-w-5xl">
      <div className="flex flex-wrap items-center gap-2 mb-4">
        <h1 className="text-xl font-semibold grow">Step console</h1>
        <button type="button" className="btn btn-sm" disabled={disabled} onClick={reset}>Reset context</button>
        <button type="button" className="btn btn-sm btn-ghost" disabled={pending} onClick={onClose}>Back to results</button>
      </div>

      <p className="text-sm mb-3">
        Runs one step against the running game. The context is shared across steps here, so a
        <code className="mx-1">Given</code>
        you run stays in effect for the next one.
      </p>

      {running && <p role="alert" className="text-error mb-3">A run owns the game. The console is off until it finishes.</p>}
      {error && <p role="alert" className="text-error mb-3">{error}</p>}
      <p role="status" className="text-sm mb-3">{pending ? "Running..." : notice}</p>

      <form
        className="flex flex-wrap items-end gap-2 mb-6"
        onSubmit={(event) => { event.preventDefault(); void run(); }}
      >
        <label className="flex flex-col gap-1 text-sm grow min-w-64">
          Step
          <input
            ref={input}
            className="input input-sm font-mono w-full"
            aria-label="Step to run"
            list="pickle-step-catalogue"
            value={text}
            placeholder='the save "test-colony" is loaded'
            onChange={(event) => { setText(event.target.value); setRecalled(-1); }}
            onKeyDown={(event) => {
              if (event.key !== "ArrowUp" && event.key !== "ArrowDown") return;
              event.preventDefault();
              recall(event.key === "ArrowUp" ? 1 : -1);
            }}
            disabled={disabled}
          />
        </label>
        <datalist id="pickle-step-catalogue">
          {steps.map((step) => <option key={step.pattern} value={step.pattern}>{step.source}</option>)}
        </datalist>
        <button type="submit" className="btn btn-sm btn-primary" disabled={disabled || !text.trim()}>Run step</button>
        <span className="text-sm">{steps.length} steps registered</span>
      </form>

      {ran.length === 0 && <p className="text-sm">Nothing run yet.</p>}
      <ul>
        {ran.map((step, position) => (
          <li key={`${ran.length - position}-${step.text}`} className="py-3 border-b border-base-content/10">
            <div className="flex flex-wrap items-baseline gap-3">
              <span className={`font-semibold ${statusTone[step.status]}`}>{step.status}</span>
              <code className="grow break-all">{step.text}</code>
              <span className="text-sm">{formatMs(step.durationMs)}</span>
            </div>
            {step.failureMessage && <pre className="text-sm mt-2 whitespace-pre-wrap break-all">{step.failureMessage}</pre>}
            {step.skeleton && <>
              <p className="text-sm mt-2">No step matches. Write one:</p>
              <pre className="text-sm mt-1 p-3 rounded bg-base-200 overflow-x-auto"><code>{step.skeleton}</code></pre>
            </>}
            {step.stateDumps.map((dump) => (
              <details key={dump.source} className="mt-2">
                <summary className="text-sm cursor-pointer">{dump.source}</summary>
                <pre className="text-sm mt-1 whitespace-pre-wrap break-all">{dump.content}</pre>
              </details>
            ))}
          </li>
        ))}
      </ul>
    </section>
  );
}
