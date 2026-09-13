#!/usr/bin/env bash
# Fetches an ffmpeg for the game container to encode films with.
#   fetch-ffmpeg.sh <destination-file>
# Bundles its own codecs, so it only needs the glibc the game image already has.
# Hosted on GitHub: johnvansickle.com answers a runner IP with a challenge page.
set -euo pipefail

DEST="${1:?usage: fetch-ffmpeg.sh <destination-file>}"

# pinned and verified because this binary runs inside the suite container; hash is the
# line for this file in checksums.sha256 published in that release.
# BtbN keeps dailies for about a month and one end-of-month tag forever, so only ever pin an
# end-of-month tag - a mid-month one is deleted out from under us and CI loses its videos.
TAG=autobuild-2026-08-31-13-27
FILE=ffmpeg-N-126342-gf88b741dbf-linux64-gpl.tar.xz
SHA256=d1cf19f669510448f18a4cffcdbd8fa9592ee7c15c92feb5b96ad7e9ccc30114
URL="https://github.com/BtbN/FFmpeg-Builds/releases/download/${TAG}/${FILE}"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

curl -sSfL --proto '=https' --proto-redir '=https' \
  --retry 3 --retry-delay 2 --retry-all-errors \
  -o "$tmp/$FILE" "$URL"

# a host can answer with a 200 and an error page, which -f cannot catch, so say what arrived
if ! printf '%s  %s\n' "$SHA256" "$tmp/$FILE" | sha256sum --check --status -; then
  echo "error: $URL returned $(wc -c < "$tmp/$FILE") bytes of $(file -b "$tmp/$FILE")" \
       "with sha256 $(sha256sum < "$tmp/$FILE" | cut -d' ' -f1), expected $SHA256" >&2
  exit 1
fi

tar -xf "$tmp/$FILE" -C "$tmp"

found="$(find "$tmp" -type f -name ffmpeg -print -quit)"
[[ -n "$found" ]] || { echo "error: no ffmpeg binary inside $URL" >&2; exit 1; }

mkdir -p "$(dirname "$DEST")"
cp "$found" "$DEST"
chmod +x "$DEST"
"$DEST" -version | head -1
