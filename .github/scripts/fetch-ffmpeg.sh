#!/usr/bin/env bash
# Fetches an ffmpeg for the game container to encode films with.
#   fetch-ffmpeg.sh <destination-file>
# Bundles its own codecs, so it only needs the glibc the game image already has.
# Hosted on GitHub: johnvansickle.com answers a runner IP with a challenge page.
set -euo pipefail

DEST="${1:?usage: fetch-ffmpeg.sh <destination-file>}"
DEFAULT_URL=https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz
URL="${FFMPEG_URL:-$DEFAULT_URL}"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

# A host can answer with a 200 and an error page, which -f cannot catch, so retry the
# fetch and say what actually arrived when it is not a tarball.
curl -sSfL --proto '=https' --proto-redir '=https' \
  --retry 3 --retry-delay 2 --retry-all-errors \
  -o "$tmp/ffmpeg.tar.xz" "$URL"

if ! tar -xf "$tmp/ffmpeg.tar.xz" -C "$tmp" 2>/dev/null; then
  echo "error: $URL returned $(wc -c < "$tmp/ffmpeg.tar.xz") bytes of" \
       "$(file -b "$tmp/ffmpeg.tar.xz"), not a tarball" >&2
  exit 1
fi

found="$(find "$tmp" -type f -name ffmpeg -print -quit)"
[[ -n "$found" ]] || { echo "error: no ffmpeg binary inside $URL" >&2; exit 1; }

mkdir -p "$(dirname "$DEST")"
cp "$found" "$DEST"
chmod +x "$DEST"
"$DEST" -version | head -1
