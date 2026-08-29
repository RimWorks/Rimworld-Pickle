# Reports

Every run writes the same set of files to the report directory. Pickle rewrites them
after each scenario, so a run that hangs still leaves a complete record of what
finished.

Set the directory with `-pickle-report-dir=PATH`. Without it Pickle writes to
`PickleReports` beside your saves, because the game folder is often read only.

| File | Use |
| --- | --- |
| `report.html` | Open it in a browser. It needs no server |
| `junit.xml` | For CI systems that read JUnit |
| `messages.ndjson` | [Cucumber messages](https://github.com/cucumber/messages), for Cucumber tooling |
| `summary.json` | Counts and an `exitReason` field |
| `summary.md` | A short summary to paste into a pull request |
| `screenshots/` | A screenshot for each failed scenario, plus any filmstrip frames |

## report.html

The HTML report is the same interface as the live dashboard, with the run's results
embedded in it. It has no external files and needs no server. Attach it to a pull
request or a CI artifact and open it directly.

It opens on the first failing scenario. Failure screenshots are embedded, so the file
travels on its own.

Filmstrip frames are the exception. A `@film` scenario captures a full size frame per
step, and embedding those would push the report past what a browser opens comfortably.
The report links them from `screenshots/film/` instead, so keep that folder next to
`report.html` when you move it.

## Video

A `@film` scenario writes its frames to `screenshots/film/<feature>--<scenario>/`. They
are named `0000.jpg`, `0001.jpg`, and so on. When `ffmpeg` is on the PATH, Pickle also
encodes them into `film.webm` in that folder. The report then shows a player next to the
strip.

RimWorld cannot encode video on its own, so `ffmpeg` is the only way to get a file a
browser will play. Install it and Pickle picks it up:

| System | Command |
| --- | --- |
| Debian or Ubuntu | `sudo apt install ffmpeg` |
| Arch | `sudo pacman -S ffmpeg` |
| macOS | `brew install ffmpeg` |
| Windows | `winget install Gyan.FFmpeg` |

Without `ffmpeg` the run still succeeds and the strip still works. Pickle logs one
warning and moves on.

To encode a folder yourself later:

```sh
ffmpeg -framerate 2 -i 0%03d.jpg -c:v libvpx-vp9 -pix_fmt yuv420p -crf 38 film.webm
```

## summary.json

Read `exitReason` when the exit code alone is not enough.

```json
{ "total": 8, "passed": 8, "failed": 0, "skipped": 0, "exitReason": "passed" }
```

| exitReason | Meaning |
| --- | --- |
| `passed` | Every scenario passed |
| `failed` | At least one scenario failed |
| `infrastructure-error` | Pickle failed before it could finish |
| `in-progress` | The run did not finish. The watchdog or a crash stopped it |

An `in-progress` value with a zero exit code means the process died without reporting.
Treat the file as the record of truth in that case.

## What a failure captures

A failed scenario records more than its message:

- The failing step. Every step before it is listed with its timing.
- A screenshot of the moment it failed.
- The log tail, so you see errors the game wrote around the failure.
- Any state dump the suite declares with `[PickleStateDump]`.

Write failure messages that carry the actual value, not only the expected one. See
[authoring](authoring.md).
