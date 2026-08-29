import { translator } from "./types";
import type { Snapshot } from "./types";

export const post = (url: string) => fetch(url, { method: "POST" }).catch(() => {});

export function Toolbar({ snap }: Readonly<{ snap: Snapshot }>) {
  const t = translator(snap);
  const running = snap.status === "running" || snap.status === "paused";
  const anySelected = snap.features.some((f) => f.scenarios.some((s) => s.selected));
  const anyFailed = snap.features.some((f) => f.scenarios.some((s) => s.outcome === "Failed"));
  const allSelected = snap.features.every((f) => f.scenarios.every((s) => s.selected));

  return (
    <div className="flex items-center gap-2 border-b border-base-content/10 bg-base-100 px-5 py-2">
      <button
        type="button"
        className="btn btn-sm btn-primary"
        disabled={running || !snap.controllable}
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
        {t("Pickle_RunSelected", "Run selected")}
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
        <span className="label-text text-sm">Break on failure</span>
      </label>

      <div className="grow" />

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
