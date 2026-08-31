#!/usr/bin/env bash
# Puts Pickle's dashboard on a temporary public URL so a CI run can be watched live.
#   expose-dashboard.sh [port]
# The URL is random, unauthenticated and dies with the job, so treat it as throwaway.
set -euo pipefail

PORT="${1:-27750}"
CLOUDFLARED="${RUNNER_TEMP:-/tmp}/cloudflared"
LOG="${RUNNER_TEMP:-/tmp}/cloudflared.log"

curl -sSfL -o "$CLOUDFLARED" \
  https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64
chmod +x "$CLOUDFLARED"

# Wait for the dashboard itself, or the tunnel points at nothing.
for _ in $(seq 1 60); do
  if curl -sS -o /dev/null "http://localhost:${PORT}/" 2>/dev/null; then
    break
  fi
  sleep 2
done

"$CLOUDFLARED" tunnel --url "http://localhost:${PORT}" --no-autoupdate \
  --logfile "$LOG" >/dev/null 2>&1 &

for _ in $(seq 1 45); do
  url="$(grep -ohE 'https://[a-z0-9-]+\.trycloudflare\.com' "$LOG" 2>/dev/null | head -1)"
  if [[ -n "${url:-}" ]]; then
    echo "::notice title=Pickle dashboard::${url}"
    echo "dashboard live at ${url}"
    exit 0
  fi
  sleep 2
done

echo "dashboard tunnel did not come up; the run continues without it" >&2
exit 1
