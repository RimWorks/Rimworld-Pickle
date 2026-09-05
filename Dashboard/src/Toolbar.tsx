import { translator } from "./types";
import type { Snapshot } from "./types";

// Resolved against our own origin so a path out of the snapshot can never aim the
// request at another host.
let commandQueue: Promise<unknown> = Promise.resolve();
export const post = (path: string) =>
  commandQueue = commandQueue.then(() => fetch(new URL(path, window.location.origin), { method: "POST", body: "" }))
    .then(async (response) => {
      if (!response.ok) throw new Error(await response.text());
    })
    .catch((error: Error) => window.dispatchEvent(new CustomEvent("pickle-command-error", { detail: error.message })));

export function Toolbar({ snap, onFixtures, onConsole }: Readonly<{ snap: Snapshot; onFixtures: () => void; onConsole: () => void }>) {
  const t = translator(snap);
  const running = snap.status === "running" || snap.status === "paused";
  const anySelected = snap.features.some((f) => f.scenarios.some((s) => s.selected));
  const anyFailed = snap.features.some((f) => f.scenarios.some((s) => s.outcome === "Failed"));
  const allSelected = snap.features.every((f) => f.scenarios.every((s) => s.selected));

  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-base-content/10 bg-base-100 px-5 py-2">
      <button
        type="button"
        className="btn btn-sm btn-primary"
        disabled={running || snap.features.length === 0 || !snap.controllable}
        onClick={() => post("/run?scope=all")}
      >
        {t("Pickle_RunAll", "Run all")}
      </button>
      <button
        type="button"
        className="btn btn-sm"
        disabled={running || !anySelected || !snap.controllable}
        onClick={() => post("/run?scope=selected")}
      >
        {t("Pickle_RunSelected", "Run selected")} ({snap.features.reduce((count, feature) => count + feature.scenarios.filter((scenario) => scenario.selected).length, 0)})
      </button>
      <button
        type="button"
        className="btn btn-sm"
        disabled={running || !anyFailed || !snap.controllable}
        onClick={() => post("/run?scope=failed")}
      >
        {t("Pickle_RerunFailed", "Rerun failed")}
      </button>

      <div className="divider divider-horizontal mx-1" />

      <div className="join">
        <button
          type="button"
          className={`btn btn-sm join-item ${snap.watch ? "btn-active" : ""}`}
          onClick={() => post("/mode?value=watch")}
        >
          {t("Pickle_ModeWatch", "Watch")}
        </button>
        <button
          type="button"
          className={`btn btn-sm join-item ${!snap.watch ? "btn-active" : ""}`}
          onClick={() => post("/mode?value=fast")}
        >
          {t("Pickle_ModeFast", "Fast")}
        </button>
      </div>

      <label className="label cursor-pointer gap-2 ml-2">
        <input
          type="checkbox"
          className="toggle toggle-sm"
          checked={snap.breakOnFailure}
          onChange={(e) => post(`/break?on=${e.target.checked}`)}
        />
        <span className="label-text text-sm">{t("Pickle_BreakOnFailure", "Break on failure")}</span>
      </label>

      <label className="label cursor-pointer gap-2">
        <input
          type="checkbox"
          className="toggle toggle-sm"
          checked={snap.includeWip}
          onChange={(e) => post(`/wip?on=${e.target.checked}`)}
        />
        <span className="label-text text-sm">{t("Pickle_IncludeWip", "Include @wip")}</span>
      </label>

      <label className="label cursor-pointer gap-2">
        <input
          type="checkbox"
          className="toggle toggle-sm"
          checked={snap.showRunPill}
          onChange={(e) => post(`/pill?on=${e.target.checked}`)}
        />
        <span className="label-text text-sm">{t("Pickle_ShowRunPill", "Show run pill")}</span>
      </label>

      <div className="grow" />

      <button type="button" className="btn btn-sm" disabled={!snap.controllable} onClick={onConsole}>
        {t("Pickle_StepConsole", "Step console")}
      </button>
      <button type="button" className="btn btn-sm" disabled={!snap.controllable} onClick={onFixtures}>
        {t("Pickle_Fixtures", "Fixtures")}
      </button>
      <a className="btn btn-sm" href="/report" target="_blank" rel="noreferrer">{t("Pickle_OpenReport", "Open report")}</a>
      <details className="dropdown dropdown-end">
        <summary className="btn btn-sm">Download reports</summary>
        <ul className="menu dropdown-content z-10 bg-base-100 rounded-box shadow-md w-48">
          <li><a href="/reports/junit.xml" download>JUnit XML</a></li>
          <li><a href="/reports/messages.ndjson" download>Cucumber messages</a></li>
          <li><a href="/reports/summary.json" download>Summary JSON</a></li>
          <li><a href="/reports/summary.md" download>Summary Markdown</a></li>
        </ul>
      </details>

      <button
        type="button"
        className="btn btn-sm btn-ghost"
        disabled={!snap.controllable}
        onClick={() => post(`/select?scope=${allSelected ? "none" : "all"}`)}
      >
        {allSelected ? t("Pickle_DeselectAll", "Deselect all") : t("Pickle_SelectAll", "Select all")}
      </button>
    </div>
  );
}
