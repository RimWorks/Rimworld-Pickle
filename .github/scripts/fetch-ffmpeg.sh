#!/usr/bin/env bash
#   fetch-ffmpeg.sh <destination-file>
set -euo pipefail

DEST="${1:?usage: fetch-ffmpeg.sh <destination-file>}"

TAG=autobuild-2026-08-31-13-27
FILE=ffmpeg-N-126342-gf88b741dbf-linux64-gpl.tar.xz
SHA256=d1cf19f669510448f18a4cffcdbd8fa9592ee7c15c92feb5b96ad7e9ccc30114
URL="https://github.com/BtbN/FFmpeg-Builds/releases/download/${TAG}/${FILE}"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

curl -sSfL --proto '=https' --proto-redir '=https' \
  --retry 3 --retry-delay 2 --retry-all-errors \
  -o "$tmp/$FILE" "$URL"

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
