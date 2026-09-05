import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import type { Snapshot } from "./types";
import { post } from "./Toolbar";

export function FilterBar({ snap, children }: Readonly<{ snap: Snapshot; children: ReactNode }>) {
  const search = useRef<HTMLInputElement>(null);
  const menu = useRef<HTMLDetailsElement>(null);
  const [multiple, setMultiple] = useState(false);
  useEffect(() => {
    if (search.current && document.activeElement !== search.current) search.current.value = snap.search ?? "";
  }, [snap.search]);
  const tags = [...new Set(snap.features.flatMap((feature) => feature.scenarios.flatMap((scenario) => scenario.tags)))].sort();
  const mods = [...new Set(snap.features.map((feature) => feature.mod))];
  const active = snap.tagFilters ?? [];
  const locked = !snap.controllable || snap.status !== "idle" || snap.fixtureBusy;
  const chooseTag = (tag: string, additive: boolean) => {
    void post(`/filter?tag=${encodeURIComponent(tag)}&additive=${additive}`);
    if (!additive && menu.current) menu.current.open = false;
  };

  return (
    <div className="runner-filters">
      <div className="filter-fields">
        <input ref={search} type="search" className="input input-sm" aria-label="Search scenarios" placeholder="Search scenarios or features" defaultValue={snap.search ?? ""} disabled={!snap.controllable} onChange={(event) => post(`/filter?search=${encodeURIComponent(event.target.value)}`)} />
        <select className="select select-sm mod-select" aria-label="Filter by mod" value={snap.modFilter ?? ""} disabled={!snap.controllable} onChange={(event) => post(`/filter?mod=${encodeURIComponent(event.target.value)}`)}>
          <option value="">All mods</option>
          {mods.map((mod) => <option key={mod} value={mod}>{mod}</option>)}
        </select>
        <details ref={menu} className="runner-menu tag-menu" onKeyDown={(event) => { if (event.key === "Escape" && menu.current) menu.current.open = false; }}>
          <summary className="btn btn-sm">{active.length ? `${active.length} tags · match any` : "Select by tag"}</summary>
          <div className="runner-popover">
            <strong>Select scenarios by tag</strong>
            <p>Click for one tag. Shift-click to add or remove tags. Matches any selected tag and replaces scenario selection.</p>
            <label><input type="checkbox" checked={multiple} disabled={locked} onChange={(event) => setMultiple(event.target.checked)} />Select multiple tags</label>
            <div className="tag-options">
              {tags.map((tag) => <button key={tag} type="button" className="btn btn-sm" aria-pressed={active.includes(tag)} disabled={locked} onClick={(event) => chooseTag(tag, multiple || event.shiftKey)}>{tag}</button>)}
              {tags.length === 0 && <p>No tags in this suite.</p>}
            </div>
            <button type="button" className="btn btn-sm btn-ghost" disabled={!active.length || locked} onClick={() => post("/filter?clearTags=true")}>Clear tag filters</button>
          </div>
        </details>
        {active.length > 0 && <div className="active-tags" role="group" aria-label="Active tags, match any">
          <span>Match any</span>
          {active.map((tag) => <button key={tag} type="button" className="btn btn-sm" aria-label={`Remove ${tag} tag`} disabled={locked} onClick={() => chooseTag(tag, true)}>{tag}<svg viewBox="0 0 20 20" aria-hidden="true"><path d="m6 6 8 8m0-8-8 8" /></svg></button>)}
        </div>}
      </div>
      <div className="bottom-actions">{children}</div>
    </div>
  );
}
