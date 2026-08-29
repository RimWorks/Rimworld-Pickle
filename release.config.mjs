// semantic-release-steam updates an existing item and never creates one, so this
// id comes from the first manual upload. See Docs/releasing.md.
const WORKSHOP_ID = process.env.PICKLE_WORKSHOP_ID ?? '3791648678';

/** @type {import('semantic-release').GlobalConfig} */
export default {
    branches: ['main'],
    plugins: [
        [
            '@semantic-release/commit-analyzer',
            {
                releaseRules: [
                    { type: 'refactor', release: 'patch' },
                    { type: 'style', release: 'patch' },
                    { type: 'ci', release: 'patch' },
                ],
            },
        ],
        '@semantic-release/release-notes-generator',
        [
            '@semantic-release/exec',
            {
                // The dashboard bundles are embedded resources, so they have to exist
                // before the mod is compiled. Harmony/ and Concord/ hold the patching
                // backends that loadFolders.xml selects between; a zip without them
                // installs a Pickle that cannot patch anything.
                prepareCmd: [
                    'npm --prefix Dashboard ci',
                    'npm --prefix Dashboard run build',
                    'dotnet build Pickle.slnx -c Release -p:Version=${nextRelease.version}',
                    'dotnet pack Source/Pickle.Ref/Pickle.Ref.csproj -c Release -p:Version=${nextRelease.version} -o artifacts',
                    'zip -r Pickle-${nextRelease.version}.zip About Assemblies Harmony Concord Languages Pickle loadFolders.xml -x "*.pdb" "About/Preview.xcf" "About/Workshop/*"',
                ].join(' && '),

                // The workflow gets NUGET_API_KEY from trusted publishing. Skipped
                // when it is absent, so a local dry run does not try to push.
                publishCmd:
                    'if [ -n "$NUGET_API_KEY" ]; then dotnet nuget push "artifacts/CryptikLemur.Pickle.Ref.${nextRelease.version}.nupkg" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate; else echo "no NUGET_API_KEY, skipping nuget push"; fi',
            },
        ],
        ...(WORKSHOP_ID
            ? [
                  [
                      'semantic-release-steam',
                      {
                          appId: '294100',
                          branchTargets: { main: 'stable' },
                          // title, previewfile and tags are deliberately unset. The
                          // plugin writes previewfile into the VDF unresolved, and a
                          // relative path breaks SteamCMD. Fields left out here keep
                          // whatever the Workshop page already has.
                          mods: [
                              {
                                  name: 'Pickle',
                                  path: '.',
                                  workshopIds: { stable: WORKSHOP_ID },
                              },
                          ],
                      },
                  ],
              ]
            : []),
        [
            '@semantic-release/github',
            {
                assets: [
                    { path: 'Pickle-*.zip', label: 'Pickle mod' },
                    { path: 'artifacts/CryptikLemur.Pickle.Ref.*.nupkg', label: 'Reference package' },
                ],
            },
        ],
    ],
};
