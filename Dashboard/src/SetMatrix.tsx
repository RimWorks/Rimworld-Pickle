import { outcomeDot } from "./types";
import type { Outcome, Scenario, Selection } from "./types";
import type { ReportSet } from "./Report";

type Row = { path: string; feature: string; name: string; cells: (Scenario | null)[] };

// A scenario is the same scenario across sets when its feature and name match. A report
// keys a feature by its name rather than a file path, so that pair is all there is.
function buildRows(sets: ReportSet[]): Row[] {
  const rows = new Map<string, Row>();

  sets.forEach((set, column) => {
    for (const feature of set.features) {
      for (const scenario of feature.scenarios) {
        const key = `${feature.name} ${scenario.name}`;
        let row = rows.get(key);
        if (!row) {
          row = { path: feature.path, feature: feature.name, name: scenario.name, cells: sets.map(() => null) };
          rows.set(key, row);
        }
        row.cells[column] = scenario;
      }
    }
  });

  return [...rows.values()];
}

// The reason this view exists: a scenario that passes under one mod set and fails under
// another. A set that did not run is missing data, not a disagreement.
function disagrees(row: Row): boolean {
  const seen = new Set<Outcome>();
  for (const cell of row.cells) {
    if (cell) seen.add(cell.outcome);
  }
  return seen.size > 1;
}

export function SetMatrix({
  sets,
  onSelect,
}: Readonly<{ sets: ReportSet[]; onSelect: (setIndex: number, selection: Selection) => void }>) {
  const rows = buildRows(sets);
  const conflicts = rows.filter(disagrees).length;

  return (
    <section>
      <div className="flex flex-wrap items-baseline gap-3 mb-4">
        <h1 className="text-xl font-semibold">Mod sets</h1>
        <span className="text-sm text-base-content/60">
          {rows.length} scenarios across {sets.length} sets
        </span>
        {conflicts > 0 && <span className="badge badge-warning badge-sm">{conflicts} disagree</span>}
      </div>

      <div className="overflow-x-auto">
        <table className="table table-sm table-pin-rows">
          <thead>
            <tr>
              <th className="min-w-64">Scenario</th>
              {sets.map((set) => (
                <th key={set.setName} className="text-center">
                  <div>{set.setName}</div>
                  <div className="font-normal text-xs text-base-content/40">
                    {set.features.length === 0 ? "did not run" : set.exitReason}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={`${row.feature}-${row.name}`} className={disagrees(row) ? "bg-warning/10" : ""}>
                <td>
                  <div className="text-xs text-base-content/40">{row.feature}</div>
                  <div className="break-words">{row.name}</div>
                </td>
                {row.cells.map((cell, column) => (
                  <td key={sets[column].setName} className="text-center align-middle">
                    {cell ? (
                      <button
                        type="button"
                        className="btn btn-ghost btn-xs gap-2"
                        aria-label={`${row.name} in ${sets[column].setName}: ${cell.outcome}`}
                        onClick={() => onSelect(column, { path: row.path, index: cell.index })}
                      >
                        <span className={`status ${outcomeDot[cell.outcome]}`} />
                        <span className="text-xs text-base-content/50">{cell.outcome}</span>
                      </button>
                    ) : (
                      <span className="text-xs text-base-content/25">absent</span>
                    )}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
