# Releasing

Releases run on every push to `main`. [semantic-release](https://semantic-release.gitbook.io/)
reads the commit messages, picks the version, and publishes.

## What a release does

1. Builds the dashboard, then the mod.
2. Packs `CryptikLemur.Pickle.Ref` and pushes it to nuget.org.
3. Zips the mod and attaches it to a GitHub release.
4. Updates the Steam Workshop item, once one exists.

The zip holds `About`, `Assemblies`, `Harmony`, `Concord`, `Pickle`, and
`loadFolders.xml`. `Harmony/` and `Concord/` carry the patching backends, so a zip
without them installs a Pickle that cannot patch anything.

## Commit messages set the version

| Prefix | Release |
| --- | --- |
| `fix:` | patch |
| `feat:` | minor |
| `refactor:`, `style:`, `ci:` | patch |
| Any type with `BREAKING CHANGE:` in the body | major |

## Credentials

| Name | Kind | Used for |
| --- | --- | --- |
| `STEAM_USERNAME` | Secret | SteamCMD login |
| `STEAM_CONFIG_VDF_B64` | Secret | SteamCMD session, base64 encoded |

The NuGet username is set in the workflow, because it is public.

NuGet uses trusted publishing, so there is no API key to store. The workflow asks
NuGet for a short-lived key with an OIDC token, which needs `id-token: write`.

## The Workshop item

The item is `3791648678`, set in `release.config.mjs`. `semantic-release-steam` updates
it on each release.

The plugin never creates an item, so a new branch target needs one manual SteamCMD
upload first. Build the mod, write a `workshop.vdf` with `appid` and `contentfolder` and
no `publishedfileid`, run `steamcmd +login $STEAM_USERNAME +workshop_build_item
path/to/workshop.vdf +quit`, then add the id it prints to `workshopIds`.

## What ships to the Workshop

`.steamignore` keeps source, docs, and build files out of the upload. The Workshop
description comes from `README.template.md`, which the plugin converts to BBCode.
`README.md` is for GitHub and does not reach the Workshop.
