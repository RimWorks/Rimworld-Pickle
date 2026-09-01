#!/usr/bin/env bash
# Stages the real Harmony and Concord mods plus a ModsConfig for one Pickle backend combo.
#   stage-pickle-mods.sh <harmony|concord|both> <mods-dir> <config-dir>
# Both come from public sources, so nothing here needs Steam credentials.
set -euo pipefail

BACKENDS="${1:?usage: stage-pickle-mods.sh <harmony|concord|both> <mods-dir> <config-dir>}"
MODS_DIR="${2:?mods dir}"
CONFIG_DIR="${3:?config dir}"

HARMONY_REPO="${HARMONY_REPO:-pardeike/HarmonyRimWorld}"
CONCORD_REPO="${CONCORD_REPO:-ConcordLib/RimWorld}"

mkdir -p "$MODS_DIR" "$CONFIG_DIR"

# Anonymous API calls allow 60 an hour per IP, which a three way matrix on a shared
# runner address can exhaust. A token raises it and costs nothing in Actions.
gh_api() {
  local url="$1"

  if [[ -n "${GITHUB_TOKEN:-}" ]]; then
    curl -sSfL --proto '=https' --proto-redir '=https' \
      -H "Authorization: Bearer ${GITHUB_TOKEN}" \
      -H "X-GitHub-Api-Version: 2022-11-28" "$url"
  else
    curl -sSfL --proto '=https' --proto-redir '=https' "$url"
  fi
}

# Both mods publish their whole folder as a release zip holding one top level directory.
stage_release_zip() {
  local repo="$1" prefix="$2" dest="$3" tmp json url inner
  tmp="$(mktemp -d)"

  json="$(gh_api "https://api.github.com/repos/${repo}/releases/latest")" || {
    echo "error: could not read the latest ${repo} release." \
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
    echo "error: the latest ${repo} release has no ${prefix}*.zip asset" >&2
    exit 1
  }

  curl -sSfL --proto '=https' --proto-redir '=https' "$url" -o "$tmp/mod.zip"
  unzip -qo "$tmp/mod.zip" -d "$tmp/x"

  inner="$(find "$tmp/x" -mindepth 1 -maxdepth 1 -type d -print -quit)"
  [[ -n "$inner" ]] || { echo "error: no mod folder inside the ${repo} zip" >&2; exit 1; }

  rm -rf "$dest"
  mv "$inner" "$dest"
  rm -rf "$tmp"
}

stage_harmony() {
  stage_release_zip "$HARMONY_REPO" "HarmonyMod" "$MODS_DIR/Harmony"
}

# CONCORD_MOD_DIR points at an already downloaded copy, such as the workshop item.
stage_concord() {
  if [[ -n "${CONCORD_MOD_DIR:-}" ]]; then
    mkdir -p "$MODS_DIR/Concord"
    tar -c --exclude=.git -C "$CONCORD_MOD_DIR" . | tar -x -C "$MODS_DIR/Concord"
    return
  fi

  stage_release_zip "$CONCORD_REPO" "Concord-" "$MODS_DIR/Concord"
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

ACTIVE="${ACTIVE}ludeon.rimworld
ludeon.rimworld.royalty
ludeon.rimworld.ideology
ludeon.rimworld.biotech
ludeon.rimworld.anomaly
ludeon.rimworld.odyssey
rimworks.pickle"

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

echo "staged '$BACKENDS':"
find "$MODS_DIR" -name '*.dll' -o -name 'About.xml' | sort
