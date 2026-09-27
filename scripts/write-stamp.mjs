#!/usr/bin/env node
import { readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

import { declaredVersions, writeStamp } from '@rimworks/mod-ci';

// the stamp names one game version, so it names the newest one the mod claims. numeric collation,
// so 1.10 sorts after 1.5 once that exists
const versions = declaredVersions(readFileSync('loadFolders.xml', 'utf8'));
const newest = [...versions].sort((a, b) => a.localeCompare(b, 'en', { numeric: true })).at(-1);

// CI stages the assemblies and names the dir per version; locally gamecrate already has them
const cache = process.env.XDG_CACHE_HOME || join(homedir(), '.cache');
const refsDir =
    process.env[`GAME_MANAGED_${newest.replace('.', '_')}`] ||
    join(cache, 'gamecrate', 'refs', 'version', 'rimworld', newest);

process.stdout.write(await writeStamp({ solution: 'Pickle.slnx', refsDir }));
