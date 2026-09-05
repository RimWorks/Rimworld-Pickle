import { useEffect, useMemo, useRef, useState } from "react";
import { countOutcomes, translator } from "./types";
import { initialTheme, writeTheme, THEMES } from "./theme";
import { readHash, toHash } from "./hash";
import type { Feature, Scenario, Selection, Snapshot } from "./types";
import { Tree } from "./Tree";
import { Detail } from "./Detail";
import { FilterBar } from "./FilterBar";
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
  const [fixturesOpen, setFixturesOpen] = useState(false);
  const [consoleOpen, setConsoleOpen] = useState(false);
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
    <div className="min-h-dvh md:h-dvh flex flex-col bg-base-200 text-base-content">
      <Header
        snap={snap}
        aborting={aborting}
        onAbort={abort}
        onOpenResults={() => {
          if (snap) setSelected(findRunning(snap));
          setFollowing(false);
          setFixturesOpen(false);
          setConsoleOpen(false);
          void post("/continue?results=true");
        }}
        theme={theme}
        onToggleTheme={() => setTheme(theme === THEMES.dark ? THEMES.light : THEMES.dark)}
      />

      {!snap ? (
        <Offline />
      ) : (
        <div className="flex-1 flex flex-col min-h-0">
          <Toolbar
            snap={snap}
            onFixtures={() => { setFixturesOpen(true); setConsoleOpen(false); }}
            onConsole={() => { setConsoleOpen(true); setFixturesOpen(false); }}
          />
          <FilterBar snap={snap} />
          {commandError && <div role="alert" className="flex items-center gap-3 px-5 py-2 text-error">
            <span className="grow">Command failed: {commandError}. Try again.</span>
            <button type="button" className="btn btn-sm" onClick={() => setCommandError("")}>Dismiss</button>
          </div>}
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
          <div className="flex-1 flex flex-col md:flex-row min-h-0">
            <aside className="w-full md:w-80 lg:w-96 max-h-60 md:max-h-none shrink-0 overflow-y-auto border-r border-base-content/10 bg-base-100 p-3">
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
            </aside>
            <main className="flex-1 min-w-0 overflow-y-auto p-4 md:p-6">
              {current?.visible === false && <p className="text-sm mb-3">This scenario is hidden by the current filters.</p>}
              {running && !following && (
                <button
                  type="button"
                  className="btn btn-sm btn-primary mb-4"
                  onClick={() => setFollowing(true)}
                >
                  Follow the run
                </button>
              )}
              {consoleOpen && <Console running={running} onClose={() => setConsoleOpen(false)} />}
              {!consoleOpen && (fixturesOpen ? <Fixtures running={running} onClose={() => setFixturesOpen(false)} /> : <Detail key={selected ? toHash(selected) : "empty"} scenario={current} live={running ? snap : null} feature={snap.features.find((feature) => feature.path === selected?.path)} onTag={snap.controllable ? (tag) => { void post(`/filter?tag=${encodeURIComponent(tag)}`); } : undefined} />)}
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

function statusDotClass(snap: Snapshot | null, paused: boolean, running: boolean): string {
  if (!snap || paused) return "status-error";
  if (running) return "status-success";
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
  onOpenResults,
  theme,
  onToggleTheme,
}: Readonly<{
  snap: Snapshot | null;
  aborting: boolean;
  onAbort: () => void;
  onOpenResults: () => void;
  theme: string;
  onToggleTheme: () => void;
}>) {
  const running = snap?.status === "running" || snap?.status === "paused";
  const paused = snap?.status === "paused";
  const counts = countOutcomes(snap?.features.flatMap((feature) => feature.scenarios) ?? []);

  return (
    <header className="shrink-0 flex flex-wrap items-center gap-4 border-b border-base-content/10 bg-base-100 px-5 py-3">
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
      <span className="badge badge-soft">{counts.notRun} not run</span>

      {paused && (
        <>
        <button type="button" className="btn btn-sm btn-primary" disabled={snap?.cancelRequested} onClick={() => post("/continue")}>
          {translator(snap)("Pickle_ContinueRun", "Continue run")}
        </button>
        <button type="button" className="btn btn-sm" disabled={snap?.cancelRequested} onClick={onOpenResults}>
          {translator(snap)("Pickle_OpenInResults", "Open in results")}
        </button>
        </>
      )}

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
