import { useEffect, useRef } from "react";
import type { Snapshot } from "./types";
import { post } from "./Toolbar";

export function FilterBar({ snap }: Readonly<{ snap: Snapshot }>) {
  const search = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (search.current && document.activeElement !== search.current) search.current.value = snap.search ?? "";
  }, [snap.search]);
  const tags = [...new Set(snap.features.flatMap((feature) => feature.scenarios.flatMap((scenario) => scenario.tags)))].sort();
  const mods = [...new Set(snap.features.map((feature) => feature.mod))];

  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-base-content/10 bg-base-100 px-5 py-2">
      <input ref={search} type="search" className="input input-sm w-60" aria-label="Search scenarios" placeholder="Search scenarios or features" defaultValue={snap.search ?? ""} disabled={!snap.controllable} onChange={(event) => post(`/filter?search=${encodeURIComponent(event.target.value)}`)} />
      <select className="select select-sm w-auto" aria-label="Filter by mod" value={snap.modFilter ?? ""} disabled={!snap.controllable} onChange={(event) => post(`/filter?mod=${encodeURIComponent(event.target.value)}`)}>
        <option value="">All mods</option>
        {mods.map((mod) => <option key={mod} value={mod}>{mod}</option>)}
      </select>
      <div className="flex flex-wrap gap-1" role="group" aria-label="Filter and select by tags">
        <span className="text-sm self-center">Filter and select:</span>
        {tags.map((tag) => (
          <button key={tag} type="button" className={`btn btn-xs ${snap.tagFilters?.includes(tag) ? "btn-warning" : "btn-ghost"}`} aria-pressed={snap.tagFilters?.includes(tag) ?? false} disabled={!snap.controllable} onClick={() => post(`/filter?tag=${encodeURIComponent(tag)}`)}>{tag}</button>
        ))}
      </div>
    </div>
  );
}
