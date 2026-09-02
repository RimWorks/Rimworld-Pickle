# Autorun and CI

Autorun runs a whole suite without a person watching. Start RimWorld with
`-pickle-run`. Pickle waits for the main menu, runs every scenario, writes reports, and
exits.

```
./RimWorldLinux -pickle-run -pickle-report-dir=/out/pickle-reports
```

## Flags

| Flag | Effect |
| --- | --- |
| `-pickle-run[=filter]` | Run the suite. See [choosing what runs](#choosing-what-runs) |
| `-pickle-report-dir=PATH` | Write reports here. Defaults to `/out/pickle-reports`, then a temporary directory |
| `-pickle-include-wip` | Include scenarios tagged `@wip` |
| `-pickle-seed=N` | Set the run seed. Pickle logs the seed it used |
| `-pickle-scenario-timeout=N` | Fail a scenario after N seconds |
| `-pickle-run-timeout=N` | Stop the run after N minutes |
| `-pickle-max-film-seconds=N` | Seconds of footage a `@film` scenario keeps. Defaults to 60 |
| `-pickle-config=PATH` | Read these flags from a file |
| `-pickle-http-port=N` | Serve the dashboard on port N instead of 27750 |
| `-pickle-no-http` | Do not serve the dashboard |
| `-pickle-no-browser` | Do not open the dashboard in a browser |

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

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Every scenario passed |
| 1 | At least one scenario failed |
| 2 | Pickle itself failed |

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
