import type { Scenario, Snapshot } from "./types";
import { formatMs, statusTone } from "./types";

export function Detail({ scenario, live }: { scenario: Scenario | null; live: Snapshot | null }) {
  if (!scenario) {
    return (
      <div className="h-full grid place-items-center text-sm text-base-content/40">
        {live ? `Running ${live.scenario}` : "Select a scenario"}
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <div className="flex items-baseline gap-3 flex-wrap">
        <h1 className="text-xl font-semibold">{scenario.name}</h1>
        <span className="text-xs text-base-content/40 font-mono">
          {formatMs(scenario.durationMs)}
        </span>
        {scenario.tags.map((tag) => (
          <span key={tag} className="badge badge-sm badge-soft badge-warning">
            {tag}
          </span>
        ))}
      </div>

      {scenario.failureMessage && (
        <div role="alert" className="alert alert-error alert-soft mt-4 items-start">
          <pre className="whitespace-pre-wrap break-words text-xs">{scenario.failureMessage}</pre>
        </div>
      )}

      <ol className="mt-5 flex flex-col gap-1">
        {scenario.steps.map((step, i) => (
          <li key={i} className="rounded-box px-3 py-2 hover:bg-base-100">
            <div className="flex items-baseline gap-2">
              <span className={`font-mono text-xs w-14 shrink-0 ${statusTone[step.status]}`}>
                {step.keyword}
              </span>
              <span className="grow font-mono text-sm break-words">{step.text}</span>
              <span className="text-xs text-base-content/30 font-mono shrink-0">
                {formatMs(step.durationMs)}
              </span>
            </div>
            {step.failureMessage && (
              <pre className="mt-2 ml-16 whitespace-pre-wrap break-words text-xs text-error">
                {step.failureMessage}
              </pre>
            )}
          </li>
        ))}
      </ol>

      {scenario.attachments.length > 0 && (
        <section className="mt-6">
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">Attachments</h2>
          <div className="mt-2 flex flex-col gap-3">
            {scenario.attachments.map((attachment) => (
              <figure key={attachment.name}>
                <figcaption className="text-xs text-base-content/50 mb-1">
                  {attachment.name}
                </figcaption>
                {isImage(attachment.content) ? (
                  <img
                    src={attachment.content}
                    alt={attachment.name}
                    className="rounded-box border border-base-content/10 max-w-full"
                  />
                ) : (
                  <pre className="rounded-box bg-base-100 p-3 text-xs whitespace-pre-wrap break-words">
                    {attachment.content}
                  </pre>
                )}
              </figure>
            ))}
          </div>
        </section>
      )}

      {(scenario.stateDumps ?? []).length > 0 && (
        <section className="mt-6">
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">State at failure</h2>
          <div className="mt-2 flex flex-col gap-3">
            {scenario.stateDumps.map((dump) => (
              <div key={dump.source}>
                <div className="text-xs text-base-content/50 mb-1 font-mono">{dump.source}</div>
                <pre className="rounded-box bg-base-100 p-3 text-xs whitespace-pre-wrap break-words">
                  {dump.content}
                </pre>
              </div>
            ))}
          </div>
        </section>
      )}

      {scenario.logTail.length > 0 && (
        <section className="mt-6">
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">Log tail</h2>
          <pre className="mt-2 rounded-box bg-base-100 p-3 text-xs whitespace-pre-wrap break-words">
            {scenario.logTail.join("\n")}
          </pre>
        </section>
      )}
    </div>
  );
}

function isImage(content: string): boolean {
  return content.startsWith("data:image/");
}
