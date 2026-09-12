#!/usr/bin/env bash
# run-suite.sh <image-ref> <mods-dir> <config-dir> <report-dir>
# Reads SUITE_FILTER, FILM_SECONDS, LIVE_DASHBOARD and SET_NAME from the environment.
# Exits with the game container's status.
#
# no set -e: it would abort on the failing wait below before the status is captured.
set -uo pipefail

IMAGE="${1:?usage: run-suite.sh <image-ref> <mods-dir> <config-dir> <report-dir>}"
MODS_DIR="${2:?mods dir}"
CONFIG_DIR="${3:?config dir}"
REPORT_DIR="${4:?report dir}"

SUITE_FILTER="${SUITE_FILTER:-}"
FILM_SECONDS="${FILM_SECONDS:-0}"
LIVE_DASHBOARD="${LIVE_DASHBOARD:-false}"
SET_NAME="${SET_NAME:-}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TMP="${RUNNER_TEMP:-/tmp}"
CFG="/home/app/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config"

echo "pulling $IMAGE ..."
start=$SECONDS
docker pull -q "$IMAGE" || exit 1
echo "pulled in $((SECONDS - start))s"

# the host and the container do not agree on user ids
mkdir -p "$REPORT_DIR"
chmod 777 "$REPORT_DIR"

run_arg="-pickle-run"
[[ -n "$SUITE_FILTER" ]] && run_arg="-pickle-run=$SUITE_FILTER"
echo "running: $run_arg"

game_args=("$run_arg" "-pickle-max-film-seconds=$FILM_SECONDS")
[[ -n "$SET_NAME" ]] && game_args+=("-pickle-set-name=$SET_NAME")

# a failed download costs the videos, not the run: FilmEncoder keeps the frames
mounts=()
if [[ "$FILM_SECONDS" != "0" ]]; then
  if "$HERE/fetch-ffmpeg.sh" "$TMP/ffmpeg"; then
    mounts+=(-v "$TMP/ffmpeg:/usr/local/bin/ffmpeg:ro")
  else
    echo "::warning title=Pickle film::no ffmpeg, so this run keeps frames and gets no videos"
  fi
fi

ports=()
if [[ "$LIVE_DASHBOARD" == "true" ]]; then
  ports=(-p 27750:27750)
  game_args+=(-pickle-http)
fi

# redirected, not piped, so $! stays the container and its status reaches wait
: > "$TMP/container.log"
docker run --rm --name "pickle-suite${SET_NAME:+-$SET_NAME}" \
  -v "$MODS_DIR:/game/Mods:ro" \
  -v "$CONFIG_DIR:$CFG" \
  -v "$REPORT_DIR:/out" \
  "${ports[@]}" "${mounts[@]}" \
  "$IMAGE" \
  run-headless /game/RimWorldLinux "${game_args[@]}" \
    -pickle-report-dir=/out -logfile /out/Player.log \
  > "$TMP/container.log" 2>&1 &
game=$!
echo "game container started, waiting for its log ..."

tail -n +1 -f "$TMP/container.log" | sed 's/^/[container] /' &
container_follow=$!

( sleep 45
  if [[ ! -f "$REPORT_DIR/Player.log" ]]; then
    echo "no Player.log after 45s; dumping state"
    docker ps -a --filter name=pickle-suite --format '{{.Status}} {{.Image}}'
    ls -la "$REPORT_DIR" || true
  fi ) &

# -o because RimLogging prefixes every line, so the match cannot anchor
( until [[ -f "$REPORT_DIR/Player.log" ]]; do sleep 1; done
  tail -n +1 -f "$REPORT_DIR/Player.log" \
    | grep --line-buffered -oE 'pickle: .*' ) &
follow=$!

dashboard=
if [[ "$LIVE_DASHBOARD" == "true" ]]; then
  "$HERE/expose-dashboard.sh" 27750 &
  dashboard=$!
fi

status=0
wait "$game" || status=$?
sleep 1
kill "$follow" "$container_follow" ${dashboard:-} 2>/dev/null || true
exit "$status"
