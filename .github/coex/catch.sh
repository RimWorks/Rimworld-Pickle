#!/usr/bin/env bash
# catch.sh <logfile> <outdir>
# waits for the game process, then freezes it the instant a real segv banner lands and
# takes a core. run as root: the game runs as another uid inside the container.
set -u
LOG="${1:?usage: catch.sh <logfile> <outdir>}"
OUT="${2:?outdir}"
PROC="${CATCH_PROC:-RimWorldLinux}"
mkdir -p "$OUT"
exec >>"$OUT/catch.log" 2>&1

echo "$(date +%s.%N) waiting for $PROC"
P=""
for _ in $(seq 1 1200); do
  P="$(pgrep -x "$PROC" | head -1)"
  [[ -n "$P" ]] && break
  sleep 0.5
done
[[ -n "$P" ]] || { echo "$(date +%s.%N) no $PROC appeared, nothing to catch"; exit 1; }
echo "$(date +%s.%N) pid $P"

# -F so a log that does not exist yet still gets followed once it does
grep -m1 -q 'signo:11' < <(tail -n +1 -F "$LOG")
kill -STOP "$P" 2>/dev/null
echo "$(date +%s.%N) STOPPED $P"
grep -E '^State' "/proc/$P/status" || echo "no /proc/$P/status, the process was already gone"
cp "/proc/$P/maps" "$OUT/maps.txt" 2>/dev/null || true
gcore -o "$OUT/core" "$P"
kill -CONT "$P" 2>/dev/null
kill -KILL "$P" 2>/dev/null
ls -l "$OUT"
if zstd -T0 -3 --rm -q "$OUT"/core.[0-9]*; then
  echo "$(date +%s.%N) compressed"
else
  # 19 GB apparent, and the upload has no use for it uncompressed
  echo "$(date +%s.%N) zstd failed, dropping the raw core"
  rm -f "$OUT"/core.[0-9]*
fi
ls -l "$OUT"
echo "$(date +%s.%N) DONE"
