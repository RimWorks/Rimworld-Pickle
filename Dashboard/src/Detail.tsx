import { useEffect, useState } from "react";
import { translator } from "./types";
import type { Attachment, Scenario, Snapshot } from "./types";
import { formatMs, statusTone } from "./types";

export function Detail({ scenario, live }: Readonly<{ scenario: Scenario | null; live: Snapshot | null }>) {
  const [zoomed, setZoomed] = useState<string | null>(null);
  const t = translator(live);

  if (!scenario) {
    return (
      <div className="h-full grid place-items-center text-sm text-base-content/40">
        {live ? `Running ${live.scenario}` : t("Pickle_SelectScenario", "Select a scenario")}
      </div>
    );
  }

  return (
    <div className="max-w-5xl">
      {zoomed && (
        <Lightbox
          src={zoomed}
          label={t("Pickle_CloseImage", "Close image")}
          onClose={() => setZoomed(null)}
        />
      )}
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
          <li key={`${i}-${step.keyword}-${step.text}`} className="rounded-box px-3 py-2 hover:bg-base-100">
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
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">{t("Pickle_Attachments", "Attachments")}</h2>
          <div className="mt-2 flex flex-col gap-3">
            {filmVideo(scenario.attachments) && <FilmVideo src={filmVideo(scenario.attachments)!} />}
            {filmFrames(scenario.attachments).length > 0 && (
              <Filmstrip frames={filmFrames(scenario.attachments)} onOpen={setZoomed} />
            )}
            {otherAttachments(scenario.attachments).map((attachment) => (
              <figure key={attachment.name}>
                <figcaption className="text-xs text-base-content/50 mb-1">
                  {attachment.name}
                </figcaption>
                {isImage(attachment.content) ? (
                  <Zoomable src={attachment.content} alt={attachment.name} onOpen={setZoomed} />
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
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">{t("Pickle_StateAtFailure", "State at failure")}</h2>
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
          <h2 className="text-xs uppercase tracking-widest text-base-content/40">{t("Pickle_LogTail", "Log tail")}</h2>
          <pre className="mt-2 rounded-box bg-base-100 p-3 text-xs whitespace-pre-wrap break-words">
            {scenario.logTail.join("\n")}
          </pre>
        </section>
      )}
    </div>
  );
}

function isImage(content: string): boolean {
  return content.startsWith("data:image/") || /\.(png|jpe?g)$/i.test(content);
}

function filmFrames(attachments: Attachment[]): Attachment[] {
  return attachments.filter((a) => a.name === "film-frames");
}

function filmVideo(attachments: Attachment[]): string | null {
  return attachments.find((a) => a.name === "film-video")?.content ?? null;
}

// Only written when ffmpeg was on the PATH during the run, so the strip below stays as
// the thing that always works.
function FilmVideo({ src }: Readonly<{ src: string }>) {
  return (
    <figure>
      <figcaption className="text-xs text-base-content/50 mb-1">film</figcaption>
      <video src={src} controls preload="metadata" className="rounded-box border border-base-content/10 max-w-full">
        <track kind="captions" />
      </video>
    </figure>
  );
}

function otherAttachments(attachments: Attachment[]): Attachment[] {
  return attachments.filter((a) => !a.name.startsWith("film-"));
}

// Frames are one per second, so a slider reads the run back far better than a column
// of near-identical stills.
function Filmstrip({ frames, onOpen }: Readonly<{ frames: Attachment[]; onOpen: (src: string) => void }>) {
  const [index, setIndex] = useState(0);
  const frame = frames[Math.min(index, frames.length - 1)];

  return (
    <figure>
      <figcaption className="text-xs text-base-content/50 mb-1 flex items-center gap-2">
        <span>filmstrip</span>
        <span className="font-mono">
          {index + 1}/{frames.length}
        </span>
        <span className="text-base-content/30">after step {index}</span>
      </figcaption>
      <Zoomable src={frame.content} alt={frame.name} onOpen={onOpen} />
      <input
        type="range"
        min={0}
        max={frames.length - 1}
        value={Math.min(index, frames.length - 1)}
        onChange={(e) => setIndex(Number(e.target.value))}
        className="range range-xs mt-2 w-full"
        aria-label="filmstrip frame"
      />
    </figure>
  );
}

function Zoomable({
  src,
  alt,
  onOpen,
}: Readonly<{ src: string; alt: string; onOpen: (src: string) => void }>) {
  return (
    <button type="button" onClick={() => onOpen(src)} className="block cursor-zoom-in">
      <img src={src} alt={alt} className="rounded-box border border-base-content/10 max-w-full" />
    </button>
  );
}

// A frame is 1920 wide and the pane is not, so the strip is only useful if a click can
// show the real thing.
function Lightbox({ src, label, onClose }: Readonly<{ src: string; label: string; onClose: () => void }>) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  return (
    <button
      type="button"
      aria-label={label}
      onClick={onClose}
      className="fixed inset-0 z-50 grid place-items-center bg-black/80 p-4 cursor-zoom-out"
    >
      <img src={src} alt="" className="max-h-full max-w-full rounded-box" />
    </button>
  );
}
