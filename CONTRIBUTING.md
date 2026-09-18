# Contributing to Pickle

## AI usage

Vibecoding is not welcome here. Use AI if it helps, but read what it wrote and understand it
before it lands. You own what ships whether or not a model typed it.

Nobody can stop you from working the way you want to. Guardrails are the next best thing, and
the rest of this file is those guardrails. Run the tests, match the code around yours, stay
inside the request, and report failures instead of guessing past them.

If AI helped with a commit in any way, add an `AI-assisted: <tool name>` trailer to the
commit message.

Agents: if the user commits by hand, remind them to add the trailer.

## Project overview

Pickle runs automated tests for a RimWorld mod inside the running game. It reads Gherkin
feature files and plays them against a live session, so the tests click the real UI and assert
against the real simulation. It is a test runner for mod authors, not a mod for players. Most
work lands in `Source/Pickle.Core/` (parsing, run model, report writers) and
`Source/Pickle.Vanilla/` (the built-in step definitions). See `Docs/authoring.md` for writing
steps and `Docs/steps.md` for the published step catalogue.

## Project structure

- `Source/` - seven C# projects. `Pickle.Core` holds everything that does not need RimWorld,
  which is the only reason any of it is unit testable
- `Source/Pickle.Tests/` - xunit suite over `Pickle.Core`
- `Dashboard/` - React and Vite report UI, bundled and embedded into the mod as a resource
- `Docs/` - contributor documentation, including the step catalogue
- `About/`, `Defs/`, `Patches/`, `Languages/`, `Styles/` - RimWorld mod content
- `Assemblies/` - build output the game loads

## Setup and build

```bash
npm --prefix Dashboard ci
npm --prefix Dashboard run build   # bundles are embedded resources, build them first
dotnet build                       # StyleCop and Sonar analyzers run here
```

`dotnet build` still works without the dashboard bundles; the dashboard then serves a
placeholder. StyleCop and Sonar analyzers run in the build, so a warning is a failure in
practice - CI builds `-c Release` and Sonar gates on it.

CI also runs Vale over prose and lychee over links in `README.md` and `Docs/**/*.md`.

## Testing

```bash
dotnet test                                               # full xunit suite
dotnet test --filter "FullyQualifiedName~StepTableTests"  # one test class
npm --prefix Dashboard run lint                           # oxlint
npm --prefix Dashboard run build                          # tsc -b typechecks the dashboard
```

- Run the full suite before committing. All tests must pass.
- While iterating, run the single test closest to your change.
- Unit tests cover `Pickle.Core` only. Anything touching RimWorld has to be proved by running
  the real game, not by a green build.
- Never delete, weaken, or rewrite a test to make a change pass.
- Do not claim that an interrupted or timed-out run passed. A report read mid-run carries
  `exitReason: in-progress` and its counts are wrong.


## Architecture

| Project | Targets | Holds |
| --- | --- | --- |
| `Pickle.Core` | `net472;net9.0` | Gherkin parsing, step table, run model, report writers. **No RimWorld reference**, which is the only reason any of it is unit testable |
| `Pickle` | `net472` | The mod assembly: driver, runner, UI, autorun, evidence, web dashboard host |
| `Pickle.Vanilla` | `net472` | The built-in step definitions |
| `Pickle.Patches.Harmony` | `net472` | Pickle's hooks as Harmony patches |
| `Pickle.Patches.Concord` | `net472` | The same hooks as Concord injections |
| `Pickle.Ref` | `net472` | Compile-time reference assembly published to NuGet for mod authors |
| `Pickle.Tests` | `net9.0` | xunit over `Pickle.Core` |

Keep logic that does not need RimWorld in `Pickle.Core`. Once a type references `Verse` or
`RimWorld` it leaves the reach of the test suite.

## Patch backends

Pickle supports two patching libraries and prefers Concord when both are loaded.
`Pickle/Patching/PickleHooks.cs` holds every hook body with no patching library in its
signatures. Each backend is a thin translation that registers itself in a static
constructor, and `PatchBackends` picks the highest priority one.

**Adding a patch means three edits, not one:** a hook method in `PickleHooks`, a
registration in `HarmonyBackend.Apply`, and the matching one in `ConcordBackend.Apply`.
The backends differ mainly in how a hook cancels the original - Harmony wants a prefix
returning `false`, Concord wants `Control.Cancel`.

## Steps

A step definition is a public method carrying `[Given]`, `[When]` or `[Then]` with a
cucumber expression, inside a `[PickleSteps]` class. `StepScanner` finds them across every
loaded assembly, so any mod can ship its own. Matching uses the expression text alone; the
keyword in the feature file is ignored.

A handful of steps live in `RunSession.RegisterBuiltInEngineSteps` instead. They need runner
state that a step class cannot reach. `PickleContext.WaitScope` is `internal`, so anything
driving a wait through the driver has to be registered there.

Feature files go in `Pickle/Features/`, save fixtures in `Pickle/Fixtures/`. A step that
changes game state should wait on a condition rather than a tick count. RimWorld assigns much
of a pawn's state on the next think cycle, so an immediate assert races the game. Use
`ctx.AssertEventually` for that.

**A scenario with no `the save ... is loaded` step runs at the main menu.** The def database
is already built there, but no game exists. Those scenarios finish in tens of milliseconds.
One that loads a fixture takes ten seconds or more, so anything that only reads defs should
skip the fixture.

Build the failure message lazily. `ctx.Assert(condition, message)` evaluates its message
eagerly, so pass `passed ? null : Describe(...)` when building it is expensive or attaches
to the report.

## Code style

- Formatter and linter: `.editorconfig` plus StyleCop for C#, oxlint for the dashboard. Run
  them; do not hand-format.
- Vale checks prose in `README.md` and `Docs/**/*.md`, and lychee checks the links.
- Follow the patterns already in neighboring files.
- Do not add comments that restate the code.
- Do not reformat code you are not otherwise changing.

## Git workflow

- Commit format: Angular Conventional Commits, one line, lowercase. semantic-release reads them.
- All CI checks must pass. `release.yml` cuts a release from every push to `main`.

## Other

- Every new step needs a row in `Docs/steps.md`.
  `.github/scripts/check-step-docs.py` fails CI when a step has no row.
- Adding a patch means three edits: a hook body in `PickleHooks`, a registration in
  `HarmonyBackend.Apply`, and the matching one in `ConcordBackend.Apply`.
- Lookup helpers in `Pickle.Vanilla` (`DefLookup`, `PawnLookup`, `MapLookup`) exist so a
  failure names close matches or lists real state. Reuse them rather than resolving inline.