#!/usr/bin/env node
import { readdir, readFile } from 'node:fs/promises';

const WORKFLOWS = '.github/workflows';

const loadFolders = await readFile('loadFolders.xml', 'utf8');
const declared = [...new Set([...loadFolders.matchAll(/<v(\d+\.\d+)>/g)].map((m) => m[1]))];
if (declared.length === 0) throw new Error('loadFolders.xml declares no versions');

const releaseConfig = await readFile('release.config.mjs', 'utf8');
const idMatches = releaseConfig.matchAll(/(?:workshopId|WORKSHOP_ID)\s*[:=][^\n]*?'(\d+)'/g);
const workshopIds = [...new Set([...idMatches].map((m) => m[1]))];
if (workshopIds.length === 0) throw new Error('release.config.mjs declares no workshop id');

const problems = [];

for (const name of (await readdir(WORKFLOWS)).filter((f) => /\.ya?ml$/.test(f))) {
  const text = await readFile(`${WORKFLOWS}/${name}`, 'utf8');
  const branchRows = [...text.matchAll(/^\s+(\d+\.\d+)=\S+$/gm)].map((m) => m[1]);
  const stageSteps = [...text.matchAll(/uses:\s*\S*stage-game-refs\S*/g)].length;

  for (const version of declared) {
    if (branchRows.length > 1 && !branchRows.includes(version)) {
      problems.push(`${name}: the game-image branches block has no ${version}= row`);
    }
    const staged = new RegExp(`game[-/]${version.replace('.', '\\.')}`).test(text);
    if (stageSteps > 1 && !staged) {
      problems.push(`${name}: no stage-game-refs step for ${version}`);
    }
  }

  for (const [, key, raw] of text.matchAll(/^\s+([\w-]*workshop-id):\s*(.+)$/gm)) {
    const id = raw.trim().replace(/^['"]|['"]$/g, '');
    if (!workshopIds.includes(id)) {
      problems.push(`${name}: ${key} is ${raw.trim()}, not one of ${workshopIds.join(', ')}`);
    }
  }
}

if (problems.length > 0) {
  for (const problem of problems) process.stderr.write(`::error::${problem}\n`);
  process.exit(1);
}

process.stdout.write(
  `every multi-version workflow covers ${declared.join(', ')}, ` +
    `and every workshop id is one of ${workshopIds.join(', ')}\n`,
);
