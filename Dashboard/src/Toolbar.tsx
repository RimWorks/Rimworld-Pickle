import { translator } from "./types";
import type { Snapshot } from "./types";
import { FilterBar } from "./FilterBar";

// Resolved against our own origin so a path out of the snapshot can never aim the
// request at another host.
let commandQueue: Promise<unknown> = Promise.resolve();
export const post = (path: string) =>
  commandQueue = commandQueue.then(() => fetch(new URL(path, window.location.origin), { method: "POST", body: "" }))
    .then(async (response) => {
      if (!response.ok) throw new Error(await response.text());
    })
    .catch((error: Error) => window.dispatchEvent(new CustomEvent("pickle-command-error", { detail: error.message })));

export function Toolbar({ snap, following, onFollow, onAbort, aborting }: Readonly<{
  snap: Snapshot;
  following: boolean;
  onFollow: (value: boolean) => void;
  onAbort: () => void;
  aborting: boolean;
}>) {
  const t = translator(snap);
  const paused = snap.status === "paused";
  const busy = snap.status === "running" || paused;
  const locked = !snap.controllable || busy || snap.fixtureBusy;
  const scenarios = snap.features.flatMap((feature) => feature.scenarios);
  const selected = scenarios.filter((scenario) => scenario.selected).length;
  const failed = scenarios.filter((scenario) => scenario.outcome === "Failed").length;
  const scope = snap.runScope;
  const count = scope === "selected" ? selected : scope === "failed" ? failed : scenarios.length;
  const runLabel = paused ? t("Pickle_ContinueRun", "Continue run") : `Run ${count} scenarios`;
  const stopping = snap.cancelRequested || aborting;
  const pauseLabel = snap.pauseRequested ? "Pausing after current step" : "Pause after current step";

  return (
    <div className="runner-controls">
      <div className="runner-toolbar">
        <label className="control-group"><span className="control-label">Scope</span>
          <select className="select select-sm scope-select" aria-label="Run scope" value={scope} disabled={locked} onChange={(event) => post(`/scope?value=${event.target.value}`)}>
            <option value="all">All {scenarios.length}</option>
            <option value="selected">{selected} selected</option>
            <option value="failed">{failed} failed</option>
          </select>
        </label>
        <div className="control-group behavior-group">
          <span className="control-label">Mode</span>
          <div className="mode-switch" role="group" aria-label="Run speed">
            <button type="button" className="btn btn-sm" aria-pressed={snap.watch} disabled={locked} onClick={() => post("/mode?value=watch")}>{t("Pickle_ModeWatch", "Watch")}</button>
            <button type="button" className="btn btn-sm" aria-pressed={!snap.watch} disabled={locked} onClick={() => post("/mode?value=fast")}>{t("Pickle_ModeFast", "Fast")}</button>
          </div>
          <details className="runner-menu">
            <summary className="btn btn-sm">Options</summary>
            <div className="runner-popover">
              <label><input type="checkbox" checked={snap.breakOnFailure} disabled={locked} onChange={(event) => post(`/break?on=${event.target.checked}`)} />{t("Pickle_PauseOnFailure", "Pause on failure")}</label>
              <label><input type="checkbox" checked={snap.includeWip} disabled={locked} onChange={(event) => post(`/wip?on=${event.target.checked}`)} />{t("Pickle_IncludeWip", "Include @wip")}</label>
              <label><input type="checkbox" checked={snap.showRunPill} disabled={!snap.controllable} onChange={(event) => post(`/pill?on=${event.target.checked}`)} />{t("Pickle_ShowRunPill", "Show run pill")}</label>
            </div>
          </details>
        </div>
        <div className="bulk-selection">
          <span>{selected} selected</span>
          <button type="button" className="btn btn-sm btn-ghost" disabled={locked} onClick={() => post("/select?scope=all")}>All {scenarios.length}</button>
          <button type="button" className="btn btn-sm btn-ghost" disabled={locked} onClick={() => post("/select?scope=none")}>Clear all</button>
        </div>
      </div>
      <FilterBar snap={snap}>
        <label className="follow-control"><input type="checkbox" checked={following} disabled={!busy} onChange={(event) => onFollow(event.target.checked)} />Follow run</label>
        <div className="run-actions" role="group" aria-label="Run actions">
          <div className="transport-actions" role="group" aria-label="Run playback">
            <button type="button" className="btn btn-sm btn-primary transport-action" aria-label={runLabel} title={runLabel} disabled={!snap.controllable || stopping || (paused ? false : locked || count === 0)} onClick={() => post(paused ? "/continue" : `/run?scope=${scope}`)}><RunIcon name={paused ? "continue" : "run"} /></button>
            <button type="button" className="btn btn-sm transport-action" aria-label="Pause run" title={pauseLabel} disabled={!snap.controllable || snap.status !== "running" || stopping || snap.pauseRequested} onClick={() => post("/pause")}><RunIcon name="pause" /></button>
            <button type="button" className="btn btn-sm btn-outline btn-error transport-action" aria-label={stopping ? "Aborting run" : "Abort run"} title={stopping ? "Aborting run" : "Abort run"} disabled={!busy || stopping} onClick={onAbort}><RunIcon name="abort" /></button>
          </div>
        </div>
      </FilterBar>
    </div>
  );
}

function RunIcon({ name }: Readonly<{ name: "run" | "continue" | "pause" | "abort" }>) {
  return <svg viewBox="0 0 20 20" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
    {name === "run" && <path d="m7 4 9 6-9 6z" />}
    {name === "continue" && <path d="m5 5 5 5-5 5m6-10 5 5-5 5" />}
    {name === "pause" && <path d="M7 5v10m6-10v10" />}
    {name === "abort" && <rect x="5" y="5" width="10" height="10" />}
  </svg>;
}
