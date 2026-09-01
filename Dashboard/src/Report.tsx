import { useEffect, useState } from "react";
import type { Feature, Scenario, Selection, Snapshot } from "./types";
import { initialTheme, writeTheme } from "./theme";
import { readHash, toHash } from "./hash";
import { Tree } from "./Tree";
import { Detail } from "./Detail";
import { Logo } from "./Logo";

type ReportSnapshot = Snapshot & { exitReason?: string };

function loadReport(): ReportSnapshot | null {
  const node = document.getElementById("pickle-report");
  if (!node?.textContent) return null;
  try {
    return JSON.parse(node.textContent) as ReportSnapshot;
  } catch {
    return null;
  }
}

export function Report() {
  const [report] = useState(loadReport);
  const [selected, setSelected] = useState<Selection | null>(() => {
    // A hash in the address bar is an explicit request, so it wins over the first failure.
    const fromHash = readHash();
    if (fromHash) return fromHash;

    const data = loadReport();
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

  if (!report) {
    return (
      <div className="min-h-screen grid place-items-center bg-base-200 text-base-content">
        <p className="text-sm opacity-60">This report has no data embedded in it.</p>
      </div>
    );
  }

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

        <span className={`badge badge-soft ${passed > 0 ? "badge-success" : ""}`}>{passed} passed</span>
        <span className={`badge badge-soft ${failed > 0 ? "badge-error" : ""}`}>{failed} failed</span>
        {skipped > 0 && <span className="badge badge-soft">{skipped} skipped</span>}

        <button type="button" className="btn btn-sm btn-ghost" onClick={toggleTheme}>
          {theme === "dim" ? "Light" : "Dark"}
        </button>
      </header>

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
          <Detail scenario={findScenario(report.features, selected)} live={null} />
        </main>
      </div>
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
