import { useEffect, useState } from "react";

type Fixture = {
  name: string;
  path: string;
  recorded: boolean;
  shadowedPath: string | null;
  sizeBytes: number;
  modified: string;
  gameVersion: string | null;
  scenarioName: string | null;
};
type Suite = { id: string; mod: string; directory: string; fixtures: Fixture[] };
type Catalog = { canSave: boolean; busy: boolean; suites: Suite[] };

async function request(path: string, mutate = false): Promise<Catalog> {
  const response = await fetch(path, mutate ? { method: "POST", body: "" } : { cache: "no-store" });
  const catalog = await response.json();
  if (!response.ok) throw new Error(catalog.error ?? `Request failed (${response.status})`);
  return catalog;
}

export function Fixtures({ running, onClose }: Readonly<{ running: boolean; onClose: () => void }>) {
  const [catalog, setCatalog] = useState<Catalog | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [suiteId, setSuiteId] = useState("");
  const [name, setName] = useState("");
  const [renaming, setRenaming] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const disabled = running || pending || catalog?.busy;
  const selectedSuite = catalog?.suites.find((suite) => suite.id === suiteId) ?? catalog?.suites[0];

  useEffect(() => {
    let alive = true;
    request("/fixtures").then((next) => { if (alive) setCatalog(next); }).catch((failure: Error) => { if (alive) setError(failure.message); });
    return () => { alive = false; };
  }, [running]);

  const refresh = async () => {
    setPending(true);
    setError("");
    try { setCatalog(await request("/fixtures")); } catch (failure) { setError(String(failure)); }
    finally { setPending(false); }
  };

  const act = async (action: string, suite: Suite, fixtureName: string, replacement = "", overwrite = false) => {
    setPending(true);
    setError("");
    setNotice("");
    try {
      const query = new URLSearchParams({ action, suite: suite.id, name: fixtureName, newName: replacement, overwrite: String(overwrite) });
      setCatalog(await request(`/fixture?${query}`, true));
      setRenaming(null);
      setNotice(`${fixtureName}: ${action} finished.`);
    } catch (failure) { setError(String(failure)); }
    finally { setPending(false); }
  };

  return (
    <section className="max-w-5xl">
      <div className="flex flex-wrap items-center gap-2 mb-4">
        <h1 className="text-xl font-semibold grow">Fixtures</h1>
        <button type="button" className="btn btn-sm" disabled={pending} onClick={refresh}>Refresh</button>
        <button type="button" className="btn btn-sm btn-ghost" disabled={pending} onClick={onClose}>Back to results</button>
      </div>
      {error && <p role="alert" className="text-error mb-3">{error}</p>}
      <p role="status" className="text-sm mb-3">{pending ? "Working..." : notice}</p>
      {catalog && <>
        <form className="flex flex-wrap items-end gap-2 mb-6" onSubmit={(event) => {
          event.preventDefault();
          if (!selectedSuite) return;
          const overwrite = selectedSuite.fixtures.some((fixture) => fixture.name === name.trim());
          if (overwrite && !window.confirm(`Overwrite fixture "${name.trim()}" in ${selectedSuite.mod}?`)) return;
          void act("save", selectedSuite, name.trim(), "", overwrite);
        }}>
          <label className="flex flex-col gap-1 text-sm">Mod
            <select className="select select-sm" value={selectedSuite?.id ?? ""} onChange={(event) => setSuiteId(event.target.value)} disabled={disabled}>
              {catalog.suites.map((suite) => <option key={suite.id} value={suite.id}>{suite.mod}</option>)}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">Fixture name
            <input className="input input-sm" value={name} onChange={(event) => setName(event.target.value)} required disabled={disabled} />
          </label>
          <button type="submit" className="btn btn-sm btn-primary" disabled={disabled || !catalog.canSave || !selectedSuite || !name.trim()}>Save fixture</button>
          {!catalog.canSave && <span className="text-sm">Load a game before saving a fixture.</span>}
        </form>
        {catalog.suites.length === 0 && <p>No suites discovered.</p>}
        {catalog.suites.map((suite) => (
          <section key={suite.id} className="mb-6">
            <h2 className="font-semibold border-b border-base-content/20 pb-2">{suite.mod}</h2>
            {suite.fixtures.length === 0 && <p className="py-3 text-sm break-all">No fixtures in {suite.directory}</p>}
            <ul>
              {suite.fixtures.map((fixture) => (
                <li key={fixture.path} className="py-3 border-b border-base-content/10">
                  <div className="flex flex-wrap items-center gap-2">
                    {renaming === fixture.path ? <form className="flex flex-wrap gap-2 grow" onSubmit={(event) => { event.preventDefault(); void act("rename", suite, fixture.name, newName.trim()); }}>
                      <input className="input input-sm" aria-label="New fixture name" value={newName} onChange={(event) => setNewName(event.target.value)} required disabled={disabled} />
                      <button type="submit" className="btn btn-sm" disabled={disabled || !newName.trim()}>Rename</button>
                      <button type="button" className="btn btn-sm btn-ghost" disabled={pending} onClick={() => setRenaming(null)}>Cancel</button>
                    </form> : <>
                      <span className="grow font-medium">{fixture.name}</span>
                      <button type="button" className="btn btn-sm" disabled={disabled} onClick={() => act("load", suite, fixture.name)}>Load</button>
                      <button type="button" className="btn btn-sm" disabled={disabled} onClick={() => { setRenaming(fixture.path); setNewName(fixture.name); }}>Rename</button>
                      <button type="button" className="btn btn-sm btn-outline btn-error" disabled={disabled} onClick={() => {
                        if (window.confirm(`Delete fixture "${fixture.name}"?\n${fixture.path}`)) void act("delete", suite, fixture.name);
                      }}>Delete</button>
                    </>}
                  </div>
                  <p className="text-sm mt-1">{fixture.recorded ? "Recorded" : "Committed"} · {Math.ceil(fixture.sizeBytes / 1024)} KB · {fixture.modified} · {fixture.scenarioName} · {fixture.gameVersion}</p>
                  <p className="text-xs break-all mt-1">{fixture.path}</p>
                  {fixture.shadowedPath && <p className="text-sm mt-1 break-all">Overrides {fixture.shadowedPath}</p>}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </>}
    </section>
  );
}
