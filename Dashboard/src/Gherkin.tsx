import { useEffect, useMemo, useRef, useState } from "react";
import { formatMs, statusTone } from "./types";
import { fold } from "./runEvents";
import type { Attempt, Folded } from "./runEvents";

type Catalogued = { pattern: string; kind: string; source: string };

const SUGGESTION_LIMIT = 8;
const POLL_MS = 400;
const KEYWORDS = /^\s*(Given|When|Then|And|But)\s+/i;

const SAMPLE = `Feature: a one off check
  Scenario: the def database is loaded
    Then def "Human" exists
`;

function currentLine(value: string, caret: number): { start: number; end: number; text: string } {
  const start = value.lastIndexOf("\n", caret - 1) + 1;
  const breakAt = value.indexOf("\n", caret);
  const end = breakAt < 0 ? value.length : breakAt;
  return { start, end, text: value.slice(start, end) };
}

function suggestionsFor(line: string, steps: Catalogued[]): Catalogued[] {
  const body = line.replace(KEYWORDS, "").trim().toLowerCase();
  if (!KEYWORDS.test(line) || body.length < 2) return [];
  return steps.filter((step) => step.pattern.toLowerCase().includes(body)).slice(0, SUGGESTION_LIMIT);
}

function Outcome({ view }: Readonly<{ view: Folded }>) {
  return (
    <>
      {view.error && <pre role="alert" className="text-error text-sm my-2 whitespace-pre-wrap break-words">{view.error}</pre>}
      {view.scenarios.map((scenario) => (
        <article key={scenario.index} className="mb-3">
          <div className="flex flex-wrap items-baseline gap-3">
            <span className="font-semibold">{scenario.name}</span>
            {scenario.outcome && <span className="text-sm">{scenario.outcome}</span>}
            {scenario.durationMs > 0 && <span className="text-sm">{formatMs(scenario.durationMs)}</span>}
          </div>
          <ol className="mt-1">
            {scenario.steps.map((step, position) => (
              <li key={`${String(position)}-${step.text}`} className="flex flex-wrap items-baseline gap-3 py-1">
                <span className={`font-mono text-xs w-16 shrink-0 ${statusTone[step.status]}`}>{step.status}</span>
                <code className="grow break-words">{step.keyword} {step.text}</code>
                <span className="text-sm">{formatMs(step.durationMs)}</span>
              </li>
            ))}
          </ol>
          {scenario.failureMessage && <pre className="text-sm mt-2 whitespace-pre-wrap break-all">{scenario.failureMessage}</pre>}
        </article>
      ))}
    </>
  );
}

export function Gherkin({ running, onClose }: Readonly<{ running: boolean; onClose: () => void }>) {
  const [steps, setSteps] = useState<Catalogued[]>([]);
  const [text, setText] = useState(SAMPLE);
  const [caret, setCaret] = useState(0);
  const [attempts, setAttempts] = useState<Attempt[]>([]);
  const [pending, setPending] = useState(false);
  const [failure, setFailure] = useState("");
  const editor = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    let alive = true;
    fetch("/steps", { cache: "no-store" })
      .then(async (response) => (await response.json()) as { steps: Catalogued[] })
      .then((body) => { if (alive) setSteps(body.steps); })
      .catch((problem: Error) => { if (alive) setFailure(problem.message); });
    return () => { alive = false; };
  }, []);

  useEffect(() => {
    let alive = true;
    let timer: number;

    const poll = async () => {
      try {
        const response = await fetch("/gherkin/runs", { cache: "no-store" });
        const body = (await response.json()) as { active: boolean; runs: Attempt[] };
        if (alive) setAttempts(body.runs);
      } catch { /* the game may be loading, the next tick retries */ }
      if (alive) timer = window.setTimeout(poll, POLL_MS);
    };

    void poll();
    return () => { alive = false; window.clearTimeout(timer); };
  }, []);

  const folded = useMemo(() => attempts.map((attempt) => ({ attempt, view: fold(attempt.events) })), [attempts]);
  const line = currentLine(text, caret);
  const suggestions = pending ? [] : suggestionsFor(line.text, steps);

  const complete = (pattern: string) => {
    const keyword = KEYWORDS.exec(line.text)?.[1] ?? "Given";
    const replacement = `    ${keyword} ${pattern}`;
    setText(text.slice(0, line.start) + replacement + text.slice(line.end));
    const at = line.start + replacement.length;
    setCaret(at);
    requestAnimationFrame(() => {
      editor.current?.focus();
      editor.current?.setSelectionRange(at, at);
    });
  };

  const run = async () => {
    setPending(true);
    setFailure("");

    try {
      const response = await fetch(new URL("/gherkin", window.location.origin), {
        method: "POST",
        headers: { "Content-Type": "text/plain" },
        body: text,
      });
      if (!response.ok) throw new Error(`The run would not start (${String(response.status)})`);
      await response.text();
    } catch (problem) { setFailure(String(problem)); }
    finally { setPending(false); }
  };

  const disabled = running || pending;
  const [newest, ...earlier] = folded;
  const live = newest?.attempt.active ?? false;

  return (
    <section className="max-w-5xl">
      <div className="flex flex-wrap items-center gap-2 mb-4">
        <h1 className="text-xl font-semibold grow">Gherkin</h1>
        <button type="button" className="btn btn-sm btn-ghost" disabled={pending} onClick={onClose}>Back to results</button>
      </div>

      <p className="text-sm mb-3">
        Runs Gherkin you type against the running game, without saving a feature file. Start a line
        with
        <code className="mx-1">Given</code>
        and keep typing to see matching steps. Every run this session is kept below.
      </p>

      {running && <p role="alert" className="text-error mb-3">A run owns the game. This is off until it finishes.</p>}
      {failure && <p role="alert" className="text-error mb-3">{failure}</p>}

      <form className="mb-6" onSubmit={(event) => { event.preventDefault(); void run(); }}>
        <label className="flex flex-col gap-1 text-sm">
          Feature
          <textarea
            ref={editor}
            className="textarea textarea-sm font-mono w-full h-64 leading-relaxed"
            aria-label="Gherkin to run"
            spellCheck={false}
            value={text}
            disabled={disabled}
            onChange={(event) => { setText(event.target.value); setCaret(event.target.selectionStart); }}
            onKeyUp={(event) => { setCaret(event.currentTarget.selectionStart); }}
            onClick={(event) => { setCaret(event.currentTarget.selectionStart); }}
            onKeyDown={(event) => {
              if (event.key === "Tab" && suggestions.length > 0) {
                event.preventDefault();
                complete(suggestions[0].pattern);
              }
            }}
          />
        </label>

        {suggestions.length > 0 && (
          <ul className="mt-1 border border-base-content/20 rounded divide-y divide-base-content/10">
            {suggestions.map((step, position) => (
              <li key={step.pattern}>
                <button
                  type="button"
                  className="w-full text-left px-3 py-1 font-mono text-sm hover:bg-base-200"
                  onClick={() => { complete(step.pattern); }}
                >
                  {step.pattern}
                  {position === 0 && <span className="ml-2 text-xs opacity-60">Tab</span>}
                </button>
              </li>
            ))}
          </ul>
        )}

        <div className="flex flex-wrap items-center gap-3 mt-3">
          <button type="submit" className="btn btn-sm btn-primary" disabled={disabled || !text.trim()}>Run</button>
          <span className="text-sm">{steps.length} steps registered</span>
          <output className="text-sm">{live ? "Running..." : newest?.view.tally}</output>
        </div>
      </form>

      {newest && (
        <section className="mb-6">
          <div className="flex flex-wrap items-baseline gap-3 mb-1">
            <h2 className="text-lg font-semibold">{newest.view.feature || "Latest run"}</h2>
            <span className="text-sm">{newest.attempt.startedAt}</span>
            {live && <span className="text-sm">live</span>}
          </div>
          <Outcome view={newest.view} />
        </section>
      )}

      {earlier.length > 0 && <h2 className="text-lg font-semibold mb-2">Earlier this session</h2>}
      {earlier.map(({ attempt, view }) => (
        <details key={attempt.id} className="mb-2 border-b border-base-content/10 pb-2">
          <summary className="text-sm cursor-pointer flex flex-wrap items-baseline gap-3">
            <span className="font-semibold">{view.feature || "unnamed"}</span>
            <span>{attempt.startedAt}</span>
            <span>{view.tally || view.error || "no result"}</span>
          </summary>
          <Outcome view={view} />
          <button
            type="button"
            className="btn btn-xs mt-2"
            disabled={disabled}
            onClick={() => { setText(attempt.source); setCaret(0); editor.current?.focus(); }}
          >
            Load into the editor
          </button>
        </details>
      ))}
    </section>
  );
}
