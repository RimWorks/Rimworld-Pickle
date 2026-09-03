import { useEffect, useMemo, useRef, useState } from "react";
import { translator } from "./types";
import { initialTheme, writeTheme, THEMES } from "./theme";
import { readHash, toHash } from "./hash";
import type { Feature, Scenario, Selection, Snapshot } from "./types";
import { Tree } from "./Tree";
import { Detail } from "./Detail";
import { Toolbar } from "./Toolbar";
import { Logo } from "./Logo";

const POLL_MS = 400;

export function App() {
  const [snap, setSnap] = useState<Snapshot | null>(null);
  const [selected, setSelected] = useState<Selection | null>(() => readHash());
  const [aborting, setAborting] = useState(false);
  const [reportBlocked, setReportBlocked] = useState(false);
  const [theme, setTheme] = useState<string>(
    () => initialTheme(),
  );
  const wasRunning = useRef(false);
  const [following, setFollowing] = useState(true);

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
    if (!following || snap?.status !== "running") return;

    const next = findRunning(snap);
    if (!next) return;
    if (next.path === selected?.path && next.index === selected?.index) return;

    setSelected(next);
  }, [snap, selected, following]);

  // A new run is a fresh reason to watch, so following comes back on by itself.
  useEffect(() => {
    if (snap?.status === "running" && !wasRunning.current) {
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
    const running = snap?.status === "running" || snap?.status === "paused";
    if (wasRunning.current && !running && snap) {
      // a hash in the address bar is an explicit request, so do not jump away from it
      const failure = findFirstFailure(snap.features);
      if (failure && !window.location.hash) setSelected(failure);
      setAborting(false);

      // A poll is not a user gesture, so a browser is entitled to block this. window.open
      // returns null when it does, which is the only way to know; fall back to a link.
      if (!window.open("/report", "_blank", "noopener")) setReportBlocked(true);
    }
    wasRunning.current = running;
  }, [snap]);

  const abort = async () => {
    setAborting(true);
    try {
      await fetch("/abort", { method: "POST" });
    } catch {
      setAborting(false);
    }
  };

  const current = useMemo(() => findScenario(snap?.features, selected), [snap, selected]);
  const running = snap?.status === "running" || snap?.status === "paused";
  const t = translator(snap);

  return (
    <div className="h-screen flex flex-col bg-base-200 text-base-content">
      <Header
        snap={snap}
        aborting={aborting}
        onAbort={abort}
        theme={theme}
        onToggleTheme={() => setTheme(theme === THEMES.dark ? THEMES.light : THEMES.dark)}
      />

      {!snap ? (
        <Offline />
      ) : (
        <div className="flex-1 flex flex-col min-h-0">
          <Toolbar snap={snap} />
          {reportBlocked && (
            <div className="alert alert-info rounded-none py-2 px-5">
              <span className="text-sm">{t("Pickle_ReportReady", "The run finished and the report is ready.")}</span>
              <a className="btn btn-sm" href="/report" target="_blank" rel="noreferrer">
                {t("Pickle_OpenReport", "Open report")}
              </a>
              <button type="button" className="btn btn-sm btn-ghost" onClick={() => setReportBlocked(false)}>
                {t("Pickle_Dismiss", "Dismiss")}
              </button>
            </div>
          )}
          <div className="flex-1 flex min-h-0">
            <aside className="w-96 shrink-0 overflow-y-auto border-r border-base-content/10 bg-base-100 p-3">
              <Tree
                features={snap.features}
                selected={selected}
                activeScenario={running ? snap.scenario : null}
                controllable={snap.controllable}
                onSelect={(next) => {
                  setFollowing(false);
                  setSelected(next);
                }}
              />
            </aside>
            <main className="flex-1 overflow-y-auto p-6">
              {running && !following && (
                <button
                  type="button"
                  className="btn btn-sm btn-primary mb-4"
                  onClick={() => setFollowing(true)}
                >
                  Follow the run
                </button>
              )}
              <Detail scenario={current} live={running ? snap : null} />
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

  // The outcome only turns Running once a step reports, so fall back to the names the
  // snapshot carries for the scenario it is on right now.
  for (const feature of snap.features) {
    if (feature.name !== snap.feature) continue;
    for (const scenario of feature.scenarios) {
      if (scenario.name === snap.scenario) return { path: feature.path, index: scenario.index };
    }
  }

  return null;
}

function subline(snap: Snapshot | null, running: boolean): string {
  if (running && snap) return snap.step;
  if (snap) return `${snap.features.length} features discovered`;
  return "";
}

function statusDotClass(snap: Snapshot | null, paused: boolean, running: boolean): string {
  if (!snap || paused) return "status-error";
  if (running) return "status-success animate-bounce";
  return "status-neutral";
}

function headline(snap: Snapshot | null, paused: boolean, running: boolean): string {
  const strings = translator(snap);
  if (!snap) return strings("Pickle_WaitingForGame", "Waiting for the game");
  if (paused) return `Paused: ${snap.scenario}`;
  if (running) return snap.scenario || "Running";
  return strings("Pickle_Idle", "Idle");
}

function Header({
  snap,
  aborting,
  onAbort,
  theme,
  onToggleTheme,
}: Readonly<{
  snap: Snapshot | null;
  aborting: boolean;
  onAbort: () => void;
  theme: string;
  onToggleTheme: () => void;
}>) {
  const running = snap?.status === "running" || snap?.status === "paused";
  const paused = snap?.status === "paused";

  return (
    <header className="shrink-0 flex items-center gap-4 border-b border-base-content/10 bg-base-100 px-5 py-3">
      <div className="flex items-center gap-3 min-w-0">
        <Logo />
        <span
          className={`status status-lg ${statusDotClass(snap, paused, running)}`}
        />
        <div className="min-w-0">
          <div className="text-sm font-semibold truncate">
            {headline(snap, paused, running)}
          </div>
          <div className="text-xs text-base-content/50 truncate font-mono">
            {subline(snap, running)}
          </div>
        </div>
      </div>

      <div className="grow" />

      <span className={`badge badge-soft ${(snap?.passed ?? 0) > 0 ? "badge-success" : ""}`}>
        {snap?.passed ?? 0} passed
      </span>
      <span className={`badge badge-soft ${(snap?.failed ?? 0) > 0 ? "badge-error" : ""}`}>
        {snap?.failed ?? 0} failed
      </span>

      <button
        type="button"
        className="btn btn-sm btn-outline btn-error"
        disabled={!running || snap?.cancelRequested || aborting}
        onClick={onAbort}
      >
        {snap?.cancelRequested || aborting ? "Aborting" : "Abort run"}
      </button>

      <button type="button" className="btn btn-sm btn-ghost" onClick={onToggleTheme}>
        {theme === THEMES.dark ? "Light" : "Dark"}
      </button>
    </header>
  );
}

function Offline() {
  return (
    <div className="flex-1 grid place-items-center">
      <div className="text-center">
        <span className="loading loading-ring loading-lg text-base-content/30" />
        <p className="mt-4 text-sm text-base-content/50">
          No response from the game. Launch it with <code className="kbd kbd-sm">-pickle-http</code>.
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
