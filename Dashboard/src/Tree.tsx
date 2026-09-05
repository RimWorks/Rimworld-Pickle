import { useEffect, useRef, useState } from "react";
import type { Feature, Scenario, Selection } from "./types";
import { countOutcomes, formatMs, isFlaky, outcomeDot } from "./types";
import { post } from "./Toolbar";

export function Tree({
  features,
  selected,
  activeScenario,
  controllable,
  readOnly = false,
  onSelect,
}: Readonly<{
  features: Feature[];
  selected: Selection | null;
  activeScenario: Selection | null;
  controllable: boolean;
  readOnly?: boolean;
  onSelect: (selection: Selection) => void;
}>) {
  // Collapse is a local view preference, so it lives here rather than in the snapshot
  // the game polls; a mod key and a feature key never collide.
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(() => new Set());
  const selectedMod = features.find((feature) => feature.path === selected?.path)?.mod;
  useEffect(() => {
    if (!selected) return;
    setCollapsed((previous) => {
      const featureKey = `feature:${selected.path}`;
      const modKey = `mod:${selectedMod}`;
      if (!previous.has(featureKey) && !previous.has(modKey)) return previous;
      const next = new Set(previous);
      next.delete(featureKey);
      next.delete(modKey);
      return next;
    });
  }, [selected, selectedMod]);
  const toggle = (key: string) =>
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (!next.delete(key)) next.add(key);
      return next;
    });

  if (features.length === 0) {
    return <p className="p-4 text-sm text-base-content/50">No features discovered.</p>;
  }

  const byMod = new Map<string, Feature[]>();
  for (const feature of features) {
    const list = byMod.get(feature.mod) ?? [];
    list.push(feature);
    byMod.set(feature.mod, list);
  }

  return (
    <div className="flex flex-col gap-4">
      {[...byMod].map(([mod, modFeatures]) => {
        const modKey = `mod:${mod}`;
        const modOpen = !collapsed.has(modKey);
        const modScenarios = modFeatures.reduce((sum, f) => sum + f.scenarios.length, 0);
        return (
          <section key={mod}>
            <h2 className="flex items-center gap-1">
              {!readOnly && <GroupCheckbox scenarios={modFeatures.flatMap((feature) => feature.scenarios)} disabled={!controllable} label={`Select ${mod}`} path={`/select?mod=${encodeURIComponent(mod)}`} />}
              <button
                type="button"
                className="flex w-full items-center gap-1 px-2 pb-1 text-left text-xs uppercase tracking-widest text-base-content/40 hover:text-base-content/70"
                aria-expanded={modOpen}
                onClick={() => toggle(modKey)}
              >
                <Chevron open={modOpen} />
                <span className="truncate grow">{mod}</span>
                {!modOpen && <span className="font-mono normal-case">{modScenarios}</span>}
              </button>
            </h2>
            {modOpen &&
              modFeatures.map((feature) => {
                const featureKey = `feature:${feature.path}`;
                const featureOpen = !collapsed.has(featureKey);
                const counts = feature.counts ?? countOutcomes(feature.scenarios);
                const ran = counts.total - counts.notRun;
                const summary = counts.failed > 0 ? `${counts.failed}/${counts.total} failed` : ran > 0 ? `${ran}/${counts.total}` : "not run";
                return (
                  <div key={feature.path} className="mb-2">
                    <h3 className="flex items-center gap-1">
                      {!readOnly && <GroupCheckbox scenarios={feature.scenarios} disabled={!controllable} label={`Select ${feature.name}`} path={`/select?path=${encodeURIComponent(feature.path)}`} />}
                      <button
                        type="button"
                        className="flex w-full items-center gap-1 rounded-btn px-2 py-1 text-left text-sm font-medium hover:bg-base-200"
                        aria-expanded={featureOpen}
                        onClick={() => toggle(featureKey)}
                      >
                        <Chevron open={featureOpen} />
                        <span aria-hidden="true" className={`status shrink-0 ${counts.failed > 0 ? "status-error" : ran > 0 ? "status-success" : "status-neutral"}`} />
                        <span className="truncate grow">{feature.name}</span>
                        <span className={`text-xs font-mono shrink-0 ${counts.failed > 0 ? "text-error" : "text-base-content/60"}`}>{summary}</span>
                      </button>
                    </h3>
                    {featureOpen && (
                      <ul className="flex flex-col">
                        {feature.scenarios.map((scenario) => {
                          const isSelected =
                            selected?.path === feature.path && selected?.index === scenario.index;
                          const isActive = activeScenario?.path === feature.path && activeScenario?.index === scenario.index;
                          return (
                            <li
                              key={scenario.index}
                              data-pickle-selected={isSelected ? "true" : undefined}
                              className={`flex items-center gap-2 rounded-btn px-2 py-1 ${
                                isSelected ? "bg-base-300" : "hover:bg-base-200"
                              }`}
                            >
                              {!readOnly && (
                              <input
                                type="checkbox"
                                className="checkbox checkbox-xs"
                                aria-label={`Select ${scenario.name}`}
                                checked={scenario.selected}
                                disabled={!controllable}
                                onChange={(e) =>
                                  post(
                                    `/select?path=${encodeURIComponent(feature.path)}&index=${scenario.index}&on=${e.target.checked}`,
                                  )
                                }
                              />
                              )}
                              <button
                                type="button"
                                className="flex items-center gap-2 grow min-w-0 text-left text-sm"
                                onClick={() => onSelect({ path: feature.path, index: scenario.index })}
                              >
                                <span
                                  className={`status ${outcomeDot[scenario.outcome]} ${
                                    isActive ? "ring-2 ring-info" : ""
                                  }`}
                                />
                                <span className="truncate grow">{scenario.name}</span>
                                {isFlaky(scenario) && (
                                  <span className="badge badge-xs badge-warning shrink-0">flaky</span>
                                )}
                                <span className="text-xs text-base-content/40 font-mono">
                                  {formatMs(scenario.durationMs)}
                                </span>
                              </button>
                            </li>
                          );
                        })}
                      </ul>
                    )}
                  </div>
                );
              })}
          </section>
        );
      })}
    </div>
  );
}

function Chevron({ open }: Readonly<{ open: boolean }>) {
  return (
    <svg
      viewBox="0 0 16 16"
      aria-hidden="true"
      className={`size-3 shrink-0 fill-current ${open ? "" : "-rotate-90"}`}
    >
      <path d="M3 5.5h10L8 11.5z" />
    </svg>
  );
}

function GroupCheckbox({ scenarios, disabled, label, path }: Readonly<{ scenarios: Scenario[]; disabled: boolean; label: string; path: string }>) {
  const input = useRef<HTMLInputElement>(null);
  const any = scenarios.some((scenario) => scenario.selected);
  const all = scenarios.every((scenario) => scenario.selected);
  useEffect(() => {
    if (input.current) input.current.indeterminate = any && !all;
  }, [any, all]);
  return <input ref={input} type="checkbox" className="checkbox checkbox-xs shrink-0" aria-label={label} checked={all} disabled={disabled} onChange={() => post(`${path}&on=${!any}`)} />;
}
