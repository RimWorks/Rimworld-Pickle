#!/usr/bin/env bash
# Stages the real Harmony and Concord mods plus a ModsConfig for one Pickle backend combo.
#   stage-pickle-mods.sh <harmony|concord|both> <mods-dir> <config-dir> [extra-mods]
# Both come from public sources, so nothing here needs Steam credentials.
#
# extra-mods is a comma separated list of owner/repo:AssetPrefix:packageId:tag:sha256,
# staged the same way and loaded after the DLC but before Pickle, so a suite sees them.
# GitHub releases only: a Workshop item needs Steam credentials this script deliberately
# does not take.
set -euo pipefail

BACKENDS="${1:?usage: stage-pickle-mods.sh <harmony|concord|both> <mods-dir> <config-dir>}"
MODS_DIR="${2:?mods dir}"
CONFIG_DIR="${3:?config dir}"
EXTRA_MODS="${4:-}"

# Pinned and verified, because a real game process loads these DLLs, so an unpinned fetch
# is a new binary in CI with no commit behind it. Bumping one is a commit: read the asset's
# sha256 out of the release API's digest field, or sha256sum the zip.
HARMONY_REPO="${HARMONY_REPO:-pardeike/HarmonyRimWorld}"
HARMONY_TAG="${HARMONY_TAG:-v2.4.2.0}"
HARMONY_SHA256="${HARMONY_SHA256:-305d014d3ef893909e28ec48615b886c7ebfa3c71b40a45d45bc20bdd4713af1}"

CONCORD_REPO="${CONCORD_REPO:-ConcordLib/RimWorld}"
CONCORD_TAG="${CONCORD_TAG:-v1.5.9}"
CONCORD_SHA256="${CONCORD_SHA256:-ceed009c77ed7feb34439e59353fda8a508dc8d90a45351ac62fb993bb25a494}"

RIMLOGGING_REPO="${RIMLOGGING_REPO:-RimWorks/rimworld-logging-framework}"
RIMLOGGING_TAG="${RIMLOGGING_TAG:-v2.27.2}"
RIMLOGGING_SHA256="${RIMLOGGING_SHA256:-7c8e7cd82b8c28b3d733eb7896d57542d15c333095fb302830ae57452e3a275b}"

mkdir -p "$MODS_DIR" "$CONFIG_DIR"

# Anonymous API calls allow 60 an hour per IP, which a three way matrix on a shared
# runner address can exhaust. A token raises it and costs nothing in Actions.
# A redirect must stay on https, so a downgraded hop cannot swap what we download.
https_only=(--proto '=https' --proto-redir '=https')

gh_api() {
  local url="$1"

  if [[ -n "${GITHUB_TOKEN:-}" ]]; then
    curl -sSfL "${https_only[@]}" \
      -H "Authorization: Bearer ${GITHUB_TOKEN}" \
      -H "X-GitHub-Api-Version: 2022-11-28" "$url"
  else
    curl -sSfL "${https_only[@]}" "$url"
  fi
}

# A release zip is either the mod folder itself or one directory holding it, and both
# shapes are common. About/About.xml is what identifies the mod root either way.
#
# Every caller passes a tag and the sha256 of that release's asset, so the zip that
# extracts is the one we checked. The traversal check below is the second line, against a
# hostile zip writing outside the temp dir.
stage_release_zip() {
  local repo="$1" prefix="$2" dest="$3" tag="$4" sha="$5" tmp json url inner bad
  tmp="$(mktemp -d)"

  json="$(gh_api "https://api.github.com/repos/${repo}/releases/tags/${tag}")" || {
    echo "error: could not read the ${repo} ${tag} release." \
         "GitHub allows 60 anonymous calls an hour; set GITHUB_TOKEN to raise it." >&2
    exit 1
  }

  url="$(printf '%s' "$json" | ASSET_PREFIX="$prefix" python3 -c 'import json, os, sys
prefix = os.environ["ASSET_PREFIX"]
assets = json.load(sys.stdin).get("assets", [])
match = [a for a in assets if a["name"].startswith(prefix) and a["name"].endswith(".zip")]
if not match:
    sys.exit(1)
print(match[0]["browser_download_url"])')" || {
    echo "error: the ${repo} ${tag} release has no ${prefix}*.zip asset" >&2
    exit 1
  }

  curl -sSfL "${https_only[@]}" "$url" -o "$tmp/mod.zip"

  # a host can answer with a 200 and an error page, which -f cannot catch, so say what arrived
  if ! printf '%s  %s\n' "$sha" "$tmp/mod.zip" | sha256sum --check --status -; then
    echo "error: ${repo} ${tag} ${prefix}*.zip is $(wc -c < "$tmp/mod.zip") bytes with sha256" \
         "$(sha256sum < "$tmp/mod.zip" | cut -d' ' -f1), expected $sha" >&2
    exit 1
  fi

  # these DLLs get loaded by a real game process, so an entry that escapes the temp dir
  # could overwrite anything the runner can write
  # grep -q would SIGPIPE unzip and the pipeline status would fail the check open
  bad="$(unzip -Z1 "$tmp/mod.zip" | grep -E '(^|/)\.\./|^/' || true)"
  if [[ -n "$bad" ]]; then
    echo "error: the ${repo} zip contains a path traversal or absolute entry, refusing to extract" >&2
    exit 1
  fi

  # a symlink entry escapes the same way without a ../ in any name: Assemblies -> /home/runner
  # and unzip writes every later member through it, so the long listing is the check
  bad="$(unzip -Z "$tmp/mod.zip" | grep -E '^l' || true)"
  if [[ -n "$bad" ]]; then
    echo "error: the ${repo} zip contains a symlink entry, refusing to extract" >&2
    exit 1
  fi

  unzip -qo "$tmp/mod.zip" -d "$tmp/x"

  inner="$(dirname "$(dirname "$(find "$tmp/x" -mindepth 2 -maxdepth 3 -path '*/About/About.xml' -print -quit)")")"
  [[ -d "$inner" && "$inner" != "." ]] || {
    echo "error: no About/About.xml inside the ${repo} zip, so its mod folder cannot be found" >&2
    exit 1
  }

  rm -rf "$dest"
  mv "$inner" "$dest"
  rm -rf "$tmp"
}

stage_harmony() {
  stage_release_zip "$HARMONY_REPO" "HarmonyMod" "$MODS_DIR/Harmony" \
    "$HARMONY_TAG" "$HARMONY_SHA256"
}

# Pickle declares RimLogging in modDependencies, so it is required on every backend combo,
# not just one. LogWatch reads its pipeline, so a run without it records no errors at all.
stage_rimlogging() {
  stage_release_zip "$RIMLOGGING_REPO" "RimLogging-" "$MODS_DIR/RimLogging" \
    "$RIMLOGGING_TAG" "$RIMLOGGING_SHA256"
}

# CONCORD_MOD_DIR points at an already downloaded copy, such as the workshop item.
stage_concord() {
  if [[ -n "${CONCORD_MOD_DIR:-}" ]]; then
    mkdir -p "$MODS_DIR/Concord"
    tar -c --exclude=.git -C "$CONCORD_MOD_DIR" . | tar -x -C "$MODS_DIR/Concord"
    return
  fi

  stage_release_zip "$CONCORD_REPO" "Concord-" "$MODS_DIR/Concord" \
    "$CONCORD_TAG" "$CONCORD_SHA256"
}

ACTIVE=""

# Concord declares loadBefore Ludeon.RimWorld, so it goes ahead of the base game.
if [[ "$BACKENDS" == "concord" || "$BACKENDS" == "both" ]]; then
  stage_concord
  ACTIVE="${ACTIVE}concordlib.concord
"
fi

if [[ "$BACKENDS" == "harmony" || "$BACKENDS" == "both" ]]; then
  stage_harmony
  ACTIVE="${ACTIVE}brrainz.harmony
"
fi

stage_rimlogging

ACTIVE="${ACTIVE}ludeon.rimworld
ludeon.rimworld.royalty
ludeon.rimworld.ideology
ludeon.rimworld.biotech
ludeon.rimworld.anomaly
ludeon.rimworld.odyssey
rimworks.rimlogging"

# After the DLC and before Pickle: a content mod has to be loaded for the suite to see it,
# and Pickle goes last so it scans everything.
if [[ -n "$EXTRA_MODS" ]]; then
  IFS=',' read -ra entries <<< "$EXTRA_MODS"
  for entry in "${entries[@]}"; do
    IFS=':' read -r repo prefix package_id tag sha <<< "$entry"
    [[ -n "$repo" && -n "$prefix" && -n "$package_id" && -n "$tag" && -n "$sha" ]] || {
      echo "error: extra mod '$entry' is not owner/repo:AssetPrefix:packageId:tag:sha256" >&2
      exit 1
    }

    stage_release_zip "$repo" "$prefix" "$MODS_DIR/${repo##*/}" "$tag" "$sha"
    ACTIVE="${ACTIVE}
${package_id}"
  done
fi

ACTIVE="${ACTIVE}
rimworks.pickle"

# the repo root is the mod folder once it is built; source and history do not ship
mkdir -p "$MODS_DIR/Pickle"
tar -c --exclude=.git --exclude=node_modules --exclude=Source \
    --exclude=Dashboard --exclude=pickle-reports . \
  | tar -x -C "$MODS_DIR/Pickle"

cat > "$CONFIG_DIR/ModsConfig.xml" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <version>1.6</version>
  <activeMods>
$(printf '    <li>%s</li>\n' $ACTIVE)
  </activeMods>
  <knownExpansions>
    <li>ludeon.rimworld.royalty</li>
    <li>ludeon.rimworld.ideology</li>
    <li>ludeon.rimworld.biotech</li>
    <li>ludeon.rimworld.anomaly</li>
    <li>ludeon.rimworld.odyssey</li>
  </knownExpansions>
</ModsConfigData>
EOF

# Without this the game starts at the virtual display's 640x480 and logs a resolution
# error, which the runner counts against whichever scenario is going at the time.
cat > "$CONFIG_DIR/Prefs.xml" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<PrefsData>
  <screenWidth>1920</screenWidth>
  <screenHeight>1080</screenHeight>
  <fullscreen>False</fullscreen>
  <volumeGame>0</volumeGame>
  <volumeMusic>0</volumeMusic>
  <volumeAmbient>0</volumeAmbient>
  <devMode>True</devMode>
  <runInBackground>True</runInBackground>
  <resetModsConfigOnCrash>False</resetModsConfigOnCrash>
</PrefsData>
EOF

# the game runs as uid 1000 and rewrites ModsConfig.xml, but the runner owns these
chmod -R 777 "$CONFIG_DIR"

echo "staged '$BACKENDS':"
find "$MODS_DIR" -name '*.dll' -o -name 'About.xml' | sort
