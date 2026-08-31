#!/usr/bin/env bash
# Fetches a static ffmpeg for the game container to encode films with.
#   fetch-ffmpeg.sh <destination-file>
# Static so it can be mounted into any image without matching its libraries.
set -euo pipefail

DEST="${1:?usage: fetch-ffmpeg.sh <destination-file>}"
URL="${FFMPEG_URL:-https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz}"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

curl -sSfL -o "$tmp/ffmpeg.tar.xz" "$URL"
tar -xf "$tmp/ffmpeg.tar.xz" -C "$tmp"

found="$(find "$tmp" -type f -name ffmpeg -print -quit)"
[[ -n "$found" ]] || { echo "error: no ffmpeg binary inside $URL" >&2; exit 1; }

mkdir -p "$(dirname "$DEST")"
cp "$found" "$DEST"
chmod +x "$DEST"
"$DEST" -version | head -1
