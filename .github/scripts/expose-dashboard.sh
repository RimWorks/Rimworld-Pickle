#!/usr/bin/env bash
# Puts Pickle's dashboard on a temporary public URL so a CI run can be watched live.
#   expose-dashboard.sh [port]
# The URL is random, unauthenticated and dies with the job, so treat it as throwaway.
set -uo pipefail

PORT="${1:-27750}"
CLOUDFLARED="${RUNNER_TEMP:-/tmp}/cloudflared"
LOG="${RUNNER_TEMP:-/tmp}/cloudflared.log"

if ! curl -sSfL -o "$CLOUDFLARED" \
  https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64; then
  echo "::warning title=Pickle dashboard::could not download cloudflared"
  exit 1
fi
chmod +x "$CLOUDFLARED"

# Wait for the dashboard itself, or the tunnel points at nothing.
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
    echo "dashboard live at ${url}"
    exit 0
  fi
  if (( i % 10 == 0 )); then
    echo "still waiting for the tunnel (${i}0s) ..."
  fi
  sleep 2
done

echo "::warning title=Pickle dashboard::tunnel never reported a URL; last log line:"
tail -2 "$LOG" 2>/dev/null || true
exit 1
