import { execFile } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);

// build plumbing rather than anything the mod runs against
const NOT_INTERESTING = [
  /^SonarAnalyzer\./,
  /^StyleCop\./,
  /^Concord\.Analyzers$/,
  /^Microsoft\.CodeAnalysis\./,
  /^Microsoft\.NETFramework\.ReferenceAssemblies$/,
  /^Microsoft\.NET\.Test\.Sdk$/,
  /^NETStandard\.Library$/,
  /^xunit/,
];

const NOT_SHIPPED = /Tests|Analyzers|CodeFixes/;

const LABELS = { 'Krafs.Rimworld.Ref': 'RimWorld', 'Lib.Harmony': 'Harmony', 'Concord.Ref': 'Concord' };

// Krafs ships one package per game build and its version IS the game version, so name the
// package rather than printing the same number twice under two headings.
const VIA = { 'Krafs.Rimworld.Ref': 'Krafs.Rimworld.Ref' };

/** Resolved versions of the packages the shipped mod is built against. */
export async function shippedPackages(solution = 'Pickle.slnx') {
  let out;
  try {
    ({ stdout: out } = await execFileAsync('dotnet', ['list', solution, 'package']));
  } catch {
    return [];
  }

  const found = new Map();
  let shipped = false;

  for (const line of out.split('\n')) {
    const project = line.match(/^Project '(.+?)'/);
    if (project) {
      shipped = !NOT_SHIPPED.test(project[1]);
      continue;
    }
    if (!shipped) continue;

    const row = line.match(/^\s*>\s+(\S+)\s+(?:\(A\)\s+)?(\S+)\s+(\S+)\s*$/);
    if (!row) continue;

    const [, name, requested, resolved] = row;
    if (NOT_INTERESTING.some((skip) => skip.test(name))) continue;
    found.set(name, { name, label: LABELS[name] ?? name, requested, resolved });
  }

  return [...found.values()].sort((a, b) => a.label.localeCompare(b.label));
}

/** Writes About/PublishStamp.txt and returns its contents. Never committed; see .gitignore. */
export async function writeStamp(modPath = process.cwd()) {
  const lines = [`verified ${new Date().toISOString()}`];

  const commit = process.env.VERIFIED_COMMIT;
  const tests = process.env.VERIFIED_TESTS;
  if (commit) lines.push(`commit   ${commit.slice(0, 7)}`);
  if (tests) lines.push(`tests    ${tests} passed`);

  const packages = await shippedPackages();
  if (packages.length > 0) {
    const nameWidth = Math.max(...packages.map((p) => p.label.length));
    const versionWidth = Math.max(...packages.map((p) => p.resolved.length));
    lines.push('', 'built and tested against');
    for (const { name, label, requested, resolved } of packages) {
      // a floating request is where a new game or Harmony release enters
      const via = VIA[name];
      let note = '';
      if (via) note = `  (${via} ${requested})`;
      else if (requested.includes('*')) note = `  (requested ${requested})`;
      lines.push(`  ${label.padEnd(nameWidth)}  ${resolved.padEnd(note ? versionWidth : 0)}${note}`);
    }
  }

  const target = join(modPath, 'About', 'PublishStamp.txt');
  await mkdir(dirname(target), { recursive: true });
  const body = `${lines.join('\n')}\n`;
  await writeFile(target, body);
  return body;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  process.stdout.write(await writeStamp());
}
