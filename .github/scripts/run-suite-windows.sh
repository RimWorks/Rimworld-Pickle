#!/usr/bin/env bash
# run-suite-windows.sh <image-ref> <mods-dir> <config-dir> <report-dir>
# Exits with the game container's status.
#
# no set -e: it would abort on the failing wait below before the status is captured.
set -uo pipefail

IMAGE="${1:?usage: run-suite-windows.sh <image-ref> <mods-dir> <config-dir> <report-dir>}"
MODS_DIR="${2:?mods dir}"
CONFIG_DIR="${3:?config dir}"
REPORT_DIR="${4:?report dir}"

TMP="${RUNNER_TEMP:-/tmp}"

echo "pulling $IMAGE ..."
docker pull -q "$IMAGE" || exit 1

mkdir -p "$REPORT_DIR"
chmod 777 "$REPORT_DIR"

# paths handed to the exe are wine paths; Z: is the container root
: > "$TMP/container.log"
docker run --rm --name pickle-suite-win \
  -v "$MODS_DIR:/game/Mods:ro" \
  -v "$CONFIG_DIR:/config" \
  -v "$REPORT_DIR:/out" \
  "$IMAGE" \
  run-headless-windows 'Z:\game\RimWorldWin64.exe' \
    '-savedatafolder=Z:\config' '-pickle-run' \
    '-pickle-max-film-seconds=0' \
    '-pickle-report-dir=Z:\out' '-logfile' 'Z:\out\Player.log' \
  > "$TMP/container.log" 2>&1 &
game=$!

( until [[ -f "$REPORT_DIR/Player.log" ]]; do sleep 2; done
  tail -n +1 -f "$REPORT_DIR/Player.log" \
    | grep --line-buffered -oE 'pickle: .*' ) &
follow=$!

status=0
wait "$game" || status=$?
sleep 1
kill "$follow" 2>/dev/null || true
exit "$status"
