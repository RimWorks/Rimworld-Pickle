import type { Feature, Selection } from "./types";
import { formatMs, outcomeDot } from "./types";
import { post } from "./Toolbar";

export function Tree({
  features,
  selected,
  activeScenario,
  controllable,
  readOnly = false,
  onSelect,
}: {
  features: Feature[];
  selected: Selection | null;
  activeScenario: string | null;
  controllable: boolean;
  readOnly?: boolean;
  onSelect: (selection: Selection) => void;
}) {
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
      {[...byMod].map(([mod, modFeatures]) => (
        <section key={mod}>
          <h2 className="px-2 pb-1 text-xs uppercase tracking-widest text-base-content/40">{mod}</h2>
          {modFeatures.map((feature) => (
            <div key={feature.path} className="mb-2">
              <h3 className="px-2 py-1 text-sm font-medium">{feature.name}</h3>
              <ul className="flex flex-col">
                {feature.scenarios.map((scenario) => {
                  const isSelected =
                    selected?.path === feature.path && selected?.index === scenario.index;
                  const isActive = activeScenario === scenario.name;
                  return (
                    <li
                      key={scenario.index}
                      className={`flex items-center gap-2 rounded-btn px-2 py-1 ${
                        isSelected ? "bg-base-300" : "hover:bg-base-200"
                      }`}
                    >
                      {!readOnly && (
                      <input
                        type="checkbox"
                        className="checkbox checkbox-xs"
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
                            isActive ? "animate-bounce" : ""
                          }`}
                        />
                        <span className="truncate grow">{scenario.name}</span>
                        <span className="text-xs text-base-content/40 font-mono">
                          {formatMs(scenario.durationMs)}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </section>
      ))}
    </div>
  );
}
