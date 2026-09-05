# Autorun and CI

Autorun runs a whole suite without a person watching. Start RimWorld with
`-pickle-run`. Pickle waits for the main menu, runs every scenario, writes reports, and
exits.

```
./RimWorldLinux -pickle-run -pickle-report-dir=$PWD/pickle-reports
```

## Flags

| Flag | Effect |
| --- | --- |
| `-pickle-run[=filter]` | Run the suite. See [choosing what runs](#choosing-what-runs) |
| `-pickle-report-dir=PATH` | Write reports here. See [where reports go](#where-reports-go) |
| `-pickle-mode=fast\|watch` | How wait steps spend time. Defaults to `fast` |
| `-pickle-include-wip` | Include scenarios tagged `@wip` |
| `-pickle-seed=N` | Set the run seed. Pickle logs the seed it used |
| `-pickle-scenario-timeout=N` | Fail a scenario after N seconds |
| `-pickle-retry=N` | Give a failed scenario N more attempts. See [flaky scenarios](#flaky-scenarios) |
| `-pickle-set-name=NAME` | Label this run's reports. See [mod sets](#mod-sets) |
| `-pickle-run-timeout=N` | Stop the run after N minutes |
| `-pickle-max-film-seconds=N` | Seconds of footage a `@film` scenario keeps. Defaults to 60. Use `0` to film nothing |
| `-pickle-config=PATH` | Read these flags from a file |
| `-pickle-http-port=N` | Serve the dashboard on port N instead of 27750 |
| `-pickle-no-http` | Do not serve the dashboard |
| `-pickle-no-browser` | Do not open the dashboard in a browser |

`fast` drives the ticks by hand, so a scenario that waits a game hour takes seconds. An
unattended run uses it because nobody is watching. Pass `-pickle-mode=watch` when you
want the film to play back at the speed a person would see. A scenario tagged `@watch`
runs at watch speed either way.

## Where reports go

`-pickle-report-dir` wins when the path is writable. Pickle creates the directory, then
writes a probe file to it. If either step fails, Pickle logs the path it could not use
and falls back.

Without the flag, Pickle uses `/out/pickle-reports` when `/out` exists and is writable,
and the save folder otherwise. `/out` is a mount your harness has to provide. A container
that does not mount it gets the save folder, which is `/data/PickleReports` under
docker-game.

## Choosing what runs

The filter is a comma separated list of terms. A scenario runs when any term picks it,
so you can combine a whole feature with one scenario from another.

| Term | Runs |
| --- | --- |
| `MyMod` | Every scenario in that mod |
| `pawn-steps.feature` | Every scenario in that file |
| `@film` | Every scenario with that tag |
| `pawn-steps.feature::skills` | Scenarios in that file whose name contains `skills` |
| `::skills` | Scenarios in any file whose name contains `skills` |
| `pawn-steps.feature:24` | The scenario declared on line 24 |

```sh
./RimWorldLinux -pickle-run="pawn-steps.feature::skills,@film"
```

Names match on a substring and ignore case, so you rarely need the whole thing. A line
number is exact, which is what you want when two scenarios share a prefix.

A filter that matches nothing is an error, not an empty pass. Pickle logs the terms you
gave and the features it found, then exits 2. This is what stops a renamed feature file
from leaving the pipeline green forever.

## Flaky scenarios

Game tests race the simulation. RimWorld assigns much of a pawn's state on the next think
cycle. A scenario can lose that race on a slow machine, then pass on the next try.

`-pickle-retry=2` gives each failed scenario two more attempts. A scenario that then
passes is **flaky**, not green:

- `summary.json` counts it in `flaky` and gives each scenario its `attempts`
- `junit.xml` writes a passing `<testcase>` holding one `<flakyFailure>` per earlier try,
  which Jenkins and Maven surefire already read
- both the report and the dashboard badge the row and show what each attempt said

A flaky scenario does not fail the run. Read the `flaky` count in `summary.json` when you
want your pipeline to gate on it.

Tag one scenario with `@retry:2` to give it more attempts than the rest of the suite. The
tag wins over the flag.

Every retry reloads the world, even under `@same-world`. A retry into the state the
failure left behind tests the wrong thing.

Retry hides nothing: keep an eye on the count. A scenario that needs a retry every run is
a bug you have not found yet.

## Mod sets

RimWorld builds its def database once at startup, so one process runs one mod set. To find
out whether a patch survives another mod, run the suite once per set and merge the reports.

Name each run, and point each at its own directory:

```sh
./RimWorldLinux -pickle-run -pickle-set-name=vanilla -pickle-report-dir=$PWD/out/vanilla
./RimWorldLinux -pickle-run -pickle-set-name=ve      -pickle-report-dir=$PWD/out/ve
```

Then merge them into one file:

```sh
.github/scripts/merge-reports.py merged.html out/vanilla/report.html out/ve/report.html
```

`merged.html` opens from disk like any report. It gains a **Compare sets** view: scenarios
down, sets across, and a highlight on every row where the sets disagree. That highlight is
the answer you came for.

The merger reads each set's name out of its own report, so the order on the command line
only decides the column order. A single report in still reads as a single report out.

`compat-sets.json` lists the sets `.github/workflows/compat.yml` runs on a schedule. Add a
mod with `owner/repo:AssetPrefix:packageId`, which
[stage-pickle-mods.sh](https://github.com/RimWorks/Rimworld-Pickle/blob/main/.github/scripts/stage-pickle-mods.sh)
pulls from that repository's latest release. Steam Workshop items do not work: staging one
needs credentials the script does not take.

Every set is a full suite run, so keep the list short.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Every scenario passed |
| 1 | At least one scenario failed |
| 2 | Pickle itself failed, or the filter matched no scenarios |

Read `exitReason` in `summary.json` when you need more than the code. A run that the
watchdog stops reports its reason there.

## Determinism

Pickle seeds RimWorld's random number generator before each scenario. The same seed
produces the same raid, the same weather, and the same pawn choices.

Every run logs its seed. To reproduce a failure from CI, pass that seed back with
`-pickle-seed`.

Prefer a seed over an assertion that accepts several outcomes. An assertion that
accepts anything tests nothing.

## Watch a run

The dashboard is on for every run, including autorun. Open `http://localhost:27750` to
see the tree, the current step, and live counts. Autorun never opens a browser itself,
because CI and containers have none.

The dashboard reads its state over HTTP, so a scenario that reloads a save does not
disturb it. This is the only way to watch a headless run.

## Continuous integration

Build the mod, then run the game headless, then read the reports.

```yaml
- name: Run the Pickle suite
  run: ./RimWorldLinux -pickle-run -pickle-report-dir=$PWD/pickle-reports

- name: Publish results
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: pickle-reports
    path: pickle-reports/
```

Upload `report.html` as an artifact. A reviewer can open it without a server and see
each failing step with its screenshot.

RimWorld needs a display, even headless. Run it under Xvfb, or in a container that
provides one. Clicks need a real X server, so use Xvfb rather than a null display when
your scenarios click.
