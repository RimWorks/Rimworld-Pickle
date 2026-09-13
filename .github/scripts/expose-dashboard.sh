#!/usr/bin/env bash
#   expose-dashboard.sh [port]
set -uo pipefail

PORT="${1:-27750}"
CLOUDFLARED="${RUNNER_TEMP:-/tmp}/cloudflared"
LOG="${RUNNER_TEMP:-/tmp}/cloudflared.log"

CLOUDFLARED_TAG=2026.9.1
CLOUDFLARED_SHA256=03f1f25d1cc93b9ad6c60569d44060bc4f17ed97075760ed8cfca4b12dcd68cc

if ! curl -sSfL --proto '=https' --proto-redir '=https' -o "$CLOUDFLARED" \
  "https://github.com/cloudflare/cloudflared/releases/download/${CLOUDFLARED_TAG}/cloudflared-linux-amd64"; then
  echo "::warning title=Pickle dashboard::could not download cloudflared"
  exit 1
fi

if ! printf '%s  %s\n' "$CLOUDFLARED_SHA256" "$CLOUDFLARED" | sha256sum --check --status -; then
  echo "::warning title=Pickle dashboard::cloudflared sha256 was" \
    "$(sha256sum < "$CLOUDFLARED" | cut -d' ' -f1), expected ${CLOUDFLARED_SHA256}"
  exit 1
fi
chmod +x "$CLOUDFLARED"

for _ in $(seq 1 90); do
  if curl -sS -o /dev/null "http://localhost:${PORT}/" 2>/dev/null; then
    break
  fi
  sleep 2
done

"$CLOUDFLARED" tunnel --url "http://localhost:${PORT}" --no-autoupdate > "$LOG" 2>&1 &

for i in $(seq 1 90); do
  url="$(grep -ohE 'https://[a-z0-9-]+\.trycloudflare\.com' "$LOG" 2>/dev/null | head -1)"
  if [[ -n "${url:-}" ]]; then
    echo "::notice title=Pickle dashboard::${url}"
    printf '\n========================================\n'
    printf '  DASHBOARD: %s\n' "$url"
    printf '========================================\n\n'

    while sleep 45; do
      echo "--- dashboard: ${url}"
    done
  fi
  if (( i % 10 == 0 )); then
    echo "still waiting for the tunnel (${i}0s) ..."
  fi
  sleep 2
done

echo "::warning title=Pickle dashboard::tunnel never reported a URL; last log line:"
tail -2 "$LOG" 2>/dev/null || true
exit 1
