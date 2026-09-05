# Running tests

There are three ways to run a suite: the in-game runner, a browser, or unattended.

## The in-game runner

Open the runner from the debug actions menu. It has two panes. The tree on the left
lists every mod, feature, and scenario. Each row has a checkbox and a status dot. Click a mod
or feature row to collapse it. The arrow at the left of the row shows whether it is open. The
pane on the right shows the selected scenario's steps, timings, and failure evidence.

| Control | Effect |
| --- | --- |
| Run selected | Run the checked scenarios |
| Rerun failed | Run only the scenarios that failed last time |
| Watch / Fast | Watch runs at normal speed. Fast skips the waiting. An unattended run is always Fast unless `-pickle-mode=watch` says otherwise |
| Break on failure | Pause the game on a failed step, with the broken state intact |
| Include @wip | Run scenarios tagged `@wip` instead of skipping them |
| Save fixture | Save the running game into a mod's `Pickle/Fixtures/` |
| Fixtures | List, load, rename, or delete a saved fixture |

Break on failure is the reason to run in game. The game pauses on the failing step, so
you can inspect the colony that produced the failure.

## A browser

The dashboard starts with the game and opens at `http://localhost:27750` in your
default browser.

It shows the same tree, the current step, and live counts. You can start and stop runs
from it. Use `-pickle-http-port=N` to serve on another port, `-pickle-no-browser` to
skip the browser, or `-pickle-no-http` to turn the dashboard off.

The dashboard reads its state over HTTP, so reloading a save does not disturb it. That
makes it the only way to watch a headless run.

The endpoints it calls are not a stable interface yet. Read them from the network tab if
you want to drive Pickle from your own tooling, but expect them to change.

Every command endpoint is a POST. The game serves them through Mono, which rejects a POST
that carries no `Content-Length`. Send an empty body: `curl -X POST -d '' <url>`. A bare
`curl -X POST` gets a `411 Length Required` and never reaches Pickle.

## The step console

The console runs one step at a time against the running game. Open it from the
**Step console** button in the dashboard toolbar, type a step, and read what comes back.

Use it to learn a real value instead of guessing one. A failing assert prints the actual
value and any state your suite dumps, so one line answers what a whole scenario used to.

```
Then "Soldier" is drafted
```

| Result | Means |
| --- | --- |
| `Passed` | The step ran and every assertion held |
| `Failed` | The step ran and something did not hold. The message carries the actual value |
| `Undefined` | No step definition matches. The console prints the C# to write |
| `Ambiguous` | Two definitions match the same text |

The context is shared between console steps, which is the one place the console differs
from a run. A `Given` you run stays in effect, and state a step writes with `ctx.Set` is
readable by the next one. Select **Reset context** to start clean.

Some limits:

- The console is off while a run is going. It answers `409` instead of queueing.
- An unattended run turns it off for the whole process.
- Pickle scans the step table when you first open the console. A steps DLL you rebuild
  after that needs a game restart, because .NET keeps the assembly it already loaded.

The console is a dashboard feature only. It does not appear in `report.html`, because a
report has no game behind it.

## Unattended

Start RimWorld with `-pickle-run`. Pickle runs every scenario, writes reports, and
exits with a code your CI can read. See [autorun and CI](autorun.md).

## Determinism

Pickle seeds RimWorld's random number generator before each scenario. The same seed
produces the same raid and the same weather.

Every run logs its seed. Pass it back with `-pickle-seed` to reproduce a failure.
