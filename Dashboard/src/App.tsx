import { useEffect, useMemo, useRef, useState } from "react";
import { countOutcomes, translator } from "./types";
import { initialTheme, writeTheme, THEMES } from "./theme";
import { readHash, toHash } from "./hash";
import type { Feature, Scenario, Selection, Snapshot } from "./types";
import { Tree } from "./Tree";
import { Detail } from "./Detail";
import { post, Toolbar } from "./Toolbar";
import { Logo } from "./Logo";
import { Fixtures } from "./Fixtures";
import { Console } from "./Console";

const POLL_MS = 400;

export function App() {
  const [snap, setSnap] = useState<Snapshot | null>(null);
  const [selected, setSelected] = useState<Selection | null>(() => readHash());
  const [aborting, setAborting] = useState(false);
  const [reportBlocked, setReportBlocked] = useState(false);
  const [workspace, setWorkspace] = useState("run");
  const [commandError, setCommandError] = useState("");
  const [theme, setTheme] = useState<string>(
    () => initialTheme(),
  );
  const wasRunning = useRef(false);
  const [following, setFollowing] = useState(true);

  useEffect(() => {
    const onError = (event: Event) => setCommandError((event as CustomEvent<string>).detail);
    window.addEventListener("pickle-command-error", onError);
    return () => window.removeEventListener("pickle-command-error", onError);
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    writeTheme(theme);
  }, [theme]);

  useEffect(() => {
    let alive = true;
    let timer: number;

    const poll = async () => {
      try {
        const res = await fetch("/state", { cache: "no-store" });
        if (!res.ok) throw new Error(`State request failed (${res.status})`);
        const next: Snapshot = await res.json();
        if (alive) setSnap(next);
      } catch {
        if (alive) setSnap(null);
      }
      if (alive) timer = window.setTimeout(poll, POLL_MS);
    };

    poll();
    return () => {
      alive = false;
      window.clearTimeout(timer);
    };
  }, []);

  // The hash is the address of a scenario, so a link into one failure survives a reload
  // and the back button walks the scenarios you looked at.
  useEffect(() => {
    const onHash = () => setSelected(readHash());
    window.addEventListener("hashchange", onHash);
    return () => window.removeEventListener("hashchange", onHash);
  }, []);

  useEffect(() => {
    const next = selected ? toHash(selected) : "";
    if (next && window.location.hash !== next) {
      window.history.replaceState(null, "", next);
    }
  }, [selected]);

  // The view follows the run rather than making you chase the highlight down the
  // sidebar. Clicking a scenario is a deliberate look elsewhere, so it stops following.
  useEffect(() => {
    if (!following || (snap?.status !== "running" && snap?.status !== "paused")) return;

    const next = findRunning(snap);
    if (!next) return;
    if (next.path === selected?.path && next.index === selected?.index) return;

    setSelected(next);
  }, [snap, selected, following]);

  // A new run is a fresh reason to watch, so following comes back on by itself.
  useEffect(() => {
    if ((snap?.status === "running" || snap?.status === "paused") && !wasRunning.current) {
      setFollowing(true);
      setReportBlocked(false);
    }
  }, [snap]);

  // Keep the followed row on screen; the sidebar is taller than the viewport.
  useEffect(() => {
    if (!selected) return;
    document
      .querySelector('[data-pickle-selected="true"]')
      ?.scrollIntoView({ block: "nearest" });
  }, [selected]);

  // Jump to the first failure the moment a run ends, matching the in-game runner.
  useEffect(() => {
    if (!snap) return;
    const running = snap?.status === "running" || snap?.status === "paused";
    if (wasRunning.current && !running && snap) {
      const failure = findFirstFailure(snap.features);
      if (failure && following) setSelected(failure);
      setAborting(false);

      // A poll is not a user gesture, so a browser is entitled to block this. window.open
      // returns null when it does, which is the only way to know; fall back to a link.
      if (!window.open("/report", "_blank", "noopener")) setReportBlocked(true);
    }
    wasRunning.current = running;
  }, [snap, following]);

  const abort = async () => {
    setAborting(true);
    try {
      const response = await fetch("/abort", { method: "POST", body: "" });
      if (!response.ok) setAborting(false);
    } catch {
      setAborting(false);
    }
  };

  const current = useMemo(() => findScenario(snap?.features, selected), [snap, selected]);
  const visibleFeatures = useMemo(() => snap?.features.map((feature) => ({
    ...feature,
    counts: countOutcomes(feature.scenarios),
    scenarios: feature.scenarios.filter((scenario) => scenario.visible !== false),
  })).filter((feature) => feature.scenarios.length > 0) ?? [], [snap]);
  const running = snap?.status === "running" || snap?.status === "paused";
  const t = translator(snap);

  return (
    <div className="runner-app min-h-dvh md:h-dvh flex flex-col bg-base-200 text-base-content">
      <Header
        snap={snap}
        workspace={workspace}
        onWorkspace={setWorkspace}
        theme={theme}
        onToggleTheme={() => setTheme(theme === THEMES.dark ? THEMES.light : THEMES.dark)}
      />

      {!snap ? (
        <Offline />
      ) : (
        <div className="flex-1 flex flex-col min-h-0">
          <Toolbar
            snap={snap}
            following={following}
            onFollow={setFollowing}
            onAbort={abort}
            aborting={aborting}

          />
          {commandError && <div role="alert" className="runner-notice flex items-center gap-3 px-5 py-2 text-error">
            <span className="grow">Command failed: {commandError}. Try again.</span>
            <button type="button" className="btn btn-sm" onClick={() => setCommandError("")}>Dismiss</button>
          </div>}
          {reportBlocked && (
            <div className="runner-notice alert alert-info py-2 px-5">
              <span className="text-sm">{t("Pickle_ReportReady", "The run finished and the report is ready.")}</span>
              <a className="btn btn-sm" href="/report" target="_blank" rel="noreferrer">
                {t("Pickle_OpenReport", "Open report")}
              </a>
              <button type="button" className="btn btn-sm btn-ghost" onClick={() => setReportBlocked(false)}>
                {t("Pickle_Dismiss", "Dismiss")}
              </button>
            </div>
          )}
          <div className={`runner-progress ${runState(snap)}`} role="progressbar" aria-label="Run progress" aria-valuemin={0} aria-valuemax={snap.runTotal || 1} aria-valuenow={snap.runCompleted}>
            <span style={{ width: `${snap.runTotal > 0 ? Math.min(100, snap.runCompleted / snap.runTotal * 100) : 0}%` }} />
          </div>
          <div id="runner-workspace" role="tabpanel" aria-labelledby={`tab-${workspace}`} className="runner-content flex-1 flex flex-col md:flex-row min-h-0">
            {workspace === "run" && <aside className="w-full md:w-80 lg:w-96 max-h-60 md:max-h-none shrink-0 overflow-y-auto border-r border-base-content/10 bg-base-100 p-3">
              <Tree
                features={visibleFeatures}
                selected={selected}
                activeScenario={running ? findRunning(snap) : null}
                controllable={snap.controllable}
                onSelect={(next) => {
                  setFollowing(false);
                  setSelected(next);
                }}
              />
            </aside>}
            <main className="flex-1 min-w-0 overflow-y-auto p-4 md:p-6">
              {workspace === "run" && <>
                {current?.visible === false && <p className="text-sm mb-3">This scenario is hidden by the current filters.</p>}
                <Detail key={selected ? toHash(selected) : "empty"} scenario={current} live={running ? snap : null} feature={snap.features.find((feature) => feature.path === selected?.path)} onTag={snap.controllable && !running ? (tag, additive) => { void post(`/filter?tag=${encodeURIComponent(tag)}&additive=${additive}`); } : undefined} />
              </>}
              {workspace === "console" && <Console running={running} onClose={() => setWorkspace("run")} />}
              {workspace === "fixtures" && <Fixtures running={running} onClose={() => setWorkspace("run")} />}
              {workspace === "reports" && <section className="reports-workspace">
                <h1 className="text-xl font-semibold">Last run report</h1>
                <p>{snap.lastRunAt ? new Date(snap.lastRunAt).toLocaleString() : "No completed run yet."}</p>
                <div className="report-actions">
                  <a className="btn btn-sm btn-primary" href="/report" target="_blank" rel="noreferrer">{t("Pickle_OpenReport", "Open report")}</a>
                  <details className="runner-menu">
                    <summary className="btn btn-sm">Download reports</summary>
                    <div className="runner-popover">
                      <a href="/reports/junit.xml" download>JUnit XML</a>
                      <a href="/reports/messages.ndjson" download>Cucumber messages</a>
                      <a href="/reports/summary.json" download>Summary JSON</a>
                      <a href="/reports/summary.md" download>Summary Markdown</a>
                    </div>
                  </details>
                </div>
              </section>}

            </main>
          </div>
        </div>
      )}
    </div>
  );
}

function findRunning(snap: Snapshot): Selection | null {
  for (const feature of snap.features) {
    for (const scenario of feature.scenarios) {
      if (scenario.outcome === "Running") return { path: feature.path, index: scenario.index };
    }
  }

  return null;
}

function subline(snap: Snapshot | null, running: boolean): string {
  if (running && snap) return snap.step;
  if (snap?.lastRunAt) return `${snap.features.length} features · last run ${new Date(snap.lastRunAt).toLocaleTimeString()}`;
  if (snap) return `${snap.features.length} features discovered`;
  return "";
}

function runState(snap: Snapshot | null): string {
  if (!snap) return "offline";
  if (snap.status !== "idle") return snap.status;
  return snap.failed > 0 ? "failed" : "idle";
}

function Header({ snap, workspace, onWorkspace, theme, onToggleTheme }: Readonly<{
  snap: Snapshot | null;
  workspace: string;
  onWorkspace: (value: string) => void;
  theme: string;
  onToggleTheme: () => void;
}>) {
  const busy = snap?.status === "running" || snap?.status === "paused";
  const state = runState(snap);
  const counts = countOutcomes(snap?.features.flatMap((feature) => feature.scenarios) ?? []);
  const tabs = [["run", "Run"], ["fixtures", "Fixtures"], ["reports", "Reports"], ["console", "Step console"]];
  const title = !snap ? "Waiting for the game" : snap.pauseRequested && !busy ? "Idle" : state === "running" && snap.pauseRequested ? "Pausing after current step" : state[0].toUpperCase() + state.slice(1);

  return (
    <header className="runner-header">
      <Logo />
      <div role="tablist" aria-label="Runner workspace" className="workspace-tabs" onKeyDown={(event) => {
        if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
        event.preventDefault();
        const index = tabs.findIndex(([key]) => key === workspace);
        const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 : (index + (event.key === "ArrowRight" ? 1 : tabs.length - 1)) % tabs.length;
        onWorkspace(tabs[next][0]);
        document.getElementById(`tab-${tabs[next][0]}`)?.focus();
      }}>
        {tabs.map(([key, label]) => <button key={key} type="button" id={`tab-${key}`} role="tab" aria-selected={workspace === key} aria-controls="runner-workspace" tabIndex={workspace === key ? 0 : -1} disabled={!snap} onClick={() => onWorkspace(key)}>{label}</button>)}
      </div>
      <div className="workspace-summary">
        <div className={`run-status ${state}`} role="status">
          <span className="run-status-dot" />
          <strong title={title}>{title}</strong>
          <small title={subline(snap, busy)}>{subline(snap, busy)}</small>
        </div>
        <div className="run-scores"><span className="text-success">{counts.passed} passed</span><span className="text-error">{counts.failed} failed</span><span>{counts.skipped} skipped</span><span>{counts.notRun} not run</span></div>
      </div>
      <button type="button" className="btn btn-sm btn-ghost" onClick={onToggleTheme}>{theme === THEMES.dark ? "Light" : "Dark"}</button>
    </header>
  );
}

function Offline() {
  return (
    <div className="flex-1 grid place-items-center">
      <div className="text-center">
        <span className="loading loading-ring loading-lg text-base-content/30" />
        <p className="mt-4 text-sm text-base-content/50">
          No response from the game. Start RimWorld with Pickle enabled.
        </p>
      </div>
    </div>
  );
}

function findScenario(features: Feature[] | undefined, selected: Selection | null): Scenario | null {
  if (!features || !selected) return null;
  for (const feature of features) {
    if (feature.path !== selected.path) continue;
    for (const scenario of feature.scenarios) {
      if (scenario.index === selected.index) return scenario;
    }
  }
  return null;
}

function findFirstFailure(features: Feature[]): Selection | null {
  for (const feature of features) {
    for (const scenario of feature.scenarios) {
      if (scenario.outcome === "Failed") return { path: feature.path, index: scenario.index };
    }
  }
  return null;
}
