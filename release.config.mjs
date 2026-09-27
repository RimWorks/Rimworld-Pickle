import { readFileSync } from 'node:fs';

import { declaredVersions, refsDirFor, releaseConfig } from '@rimworks/mod-ci';

// semantic-release-steam updates an existing item and never creates one, so this
// id comes from the first manual upload. See Docs/releasing.md.
const WORKSHOP_ID = process.env.WORKSHOP_ID || '3791648678';

const versions = declaredVersions(readFileSync('loadFolders.xml', 'utf8'));
const newest = versions.at(-1);

/** @type {import('semantic-release').GlobalConfig} */
export default releaseConfig({
    solution: 'Pickle.slnx',
    versions,
    // the dashboard bundles are embedded resources, so they exist before the compile
    beforeBuild: ['npm --prefix Dashboard ci', 'npm --prefix Dashboard run build'],
    mods: [{ name: 'Pickle', workshopId: WORKSHOP_ID }],
    pack: 'Source/Pickle.Ref/Pickle.Ref.csproj',
    packArgs: `-p:GameVersion=${newest} -p:GameManagedDir=${refsDirFor(newest)}`,
    nupkgGlob: 'artifacts/RimWorks.Pickle.Ref.${nextRelease.version}.nupkg',
    assets: [
        { path: 'dist/Pickle-*.zip', label: 'Pickle mod' },
        { path: 'artifacts/RimWorks.Pickle.Ref.*.nupkg', label: 'Reference package' },
    ],
});
