import { useEffect, useState } from "react";
import type { Feature, Scenario, Selection, Snapshot } from "./types";
import { initialTheme, writeTheme } from "./theme";
import { readHash, toHash } from "./hash";
import { Tree } from "./Tree";
import { Detail } from "./Detail";
import { Logo } from "./Logo";
import { SetMatrix } from "./SetMatrix";

export type ReportSet = Snapshot & { exitReason?: string; setName?: string | null };

// A merged report carries {"sets": [...]}. A single run carries the payload itself, so it
// becomes a one-set report and every existing report renders exactly as it did.
function loadSets(): ReportSet[] | null {
  const node = document.getElementById("pickle-report");
  if (!node?.textContent) return null;
  try {
    const raw = JSON.parse(node.textContent) as ReportSet & { sets?: ReportSet[] };
    const sets = raw.sets ?? [raw];
    return sets.length > 0 ? sets : null;
  } catch {
    return null;
  }
}

export function Report() {
  const [sets] = useState(loadSets);
  const [setIndex, setSetIndex] = useState(0);
  const [comparing, setComparing] = useState(() => (loadSets()?.length ?? 0) > 1);
  const [selected, setSelected] = useState<Selection | null>(() => {
    // A hash in the address bar is an explicit request, so it wins over the first failure.
    const fromHash = readHash();
    if (fromHash) return fromHash;

    const data = loadSets()?.[0];
    return data ? (firstFailure(data.features) ?? firstScenario(data.features)) : null;
  });
  const [theme, setTheme] = useState<string>(
    () => initialTheme(),
  );

  // The document ships with a hardcoded theme, so the chosen one has to be applied.
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

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

  const toggleTheme = () => {
    const next = theme === "dim" ? "winter" : "dim";
    setTheme(next);
    document.documentElement.dataset.theme = next;
    writeTheme(next);
  };

  if (!sets) {
    return (
      <div className="min-h-screen grid place-items-center bg-base-200 text-base-content">
        <p className="text-sm opacity-60">This report has no data embedded in it.</p>
      </div>
    );
  }

  const report = sets[Math.min(setIndex, sets.length - 1)];

  const scenarios = report.features.flatMap((f) => f.scenarios);
  const passed = scenarios.filter((s) => s.outcome === "Passed").length;
  const failed = scenarios.filter((s) => s.outcome === "Failed").length;
  const skipped = scenarios.filter((s) => s.outcome === "Skipped").length;
  const totalMs = scenarios.reduce((sum, s) => sum + s.durationMs, 0);

  return (
    <div className="h-screen flex flex-col bg-base-200 text-base-content">
      <header className="shrink-0 flex items-center gap-4 border-b border-base-content/10 bg-base-100 px-5 py-3">
        <div className="flex items-center gap-3">
          <Logo />
          <span className={`status status-lg ${failed > 0 ? "status-error" : "status-success"}`} />
          <div>
            <div className="text-sm font-semibold">
              Pickle report {report.exitReason ? `- ${report.exitReason}` : ""}
            </div>
            <div className="text-xs text-base-content/50">
              {scenarios.length} scenarios in {(totalMs / 1000).toFixed(1)}s
            </div>
          </div>
        </div>

        <div className="grow" />

        {sets.length > 1 && (
          <>
            <button
              type="button"
              className={`btn btn-sm ${comparing ? "btn-active" : ""}`}
              onClick={() => setComparing(!comparing)}
            >
              Compare sets
            </button>
            <select
              className="select select-sm"
              aria-label="Mod set"
              value={setIndex}
              onChange={(event) => { setSetIndex(Number(event.target.value)); setComparing(false); }}
            >
              {sets.map((set, index) => (
                <option key={set.setName ?? index} value={index}>{set.setName ?? `set ${index + 1}`}</option>
              ))}
            </select>
          </>
        )}

        <span className={`badge badge-soft ${passed > 0 ? "badge-success" : ""}`}>{passed} passed</span>
        <span className={`badge badge-soft ${failed > 0 ? "badge-error" : ""}`}>{failed} failed</span>
        {skipped > 0 && <span className="badge badge-soft">{skipped} skipped</span>}

        <button type="button" className="btn btn-sm btn-ghost" onClick={toggleTheme}>
          {theme === "dim" ? "Light" : "Dark"}
        </button>
      </header>

      {comparing ? (
        <main className="flex-1 overflow-auto p-6">
          <SetMatrix
            sets={sets}
            onSelect={(column, selection) => { setSetIndex(column); setSelected(selection); setComparing(false); }}
          />
        </main>
      ) : (
      <div className="flex-1 flex min-h-0">
        <aside className="w-96 shrink-0 overflow-y-auto border-r border-base-content/10 bg-base-100 p-3">
          <Tree
            features={report.features}
            selected={selected}
            activeScenario={null}
            controllable={false}
            readOnly
            onSelect={setSelected}
          />
        </aside>
        <main className="flex-1 overflow-y-auto p-6">
          <Detail key={selected ? toHash(selected) : "empty"} scenario={findScenario(report.features, selected)} live={null} feature={report.features.find((feature) => feature.path === selected?.path)} />
        </main>
      </div>
      )}
    </div>
  );
}

function firstFailure(features: Feature[]): Selection | null {
  for (const feature of features) {
    for (const scenario of feature.scenarios) {
      if (scenario.outcome === "Failed") return { path: feature.path, index: scenario.index };
    }
  }
  return null;
}

function firstScenario(features: Feature[]): Selection | null {
  for (const feature of features) {
    const scenario = feature.scenarios[0];
    if (scenario) return { path: feature.path, index: scenario.index };
  }
  return null;
}

function findScenario(features: Feature[], selected: Selection | null): Scenario | null {
  if (!selected) return null;
  for (const feature of features) {
    if (feature.path !== selected.path) continue;
    for (const scenario of feature.scenarios) {
      if (scenario.index === selected.index) return scenario;
    }
  }
  return null;
}
