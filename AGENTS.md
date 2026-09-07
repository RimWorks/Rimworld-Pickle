# AGENTS.md

## AI usage

We don't vibecode here. Use AI if it helps, but read what it wrote and understand it before
it lands. You own what ships, whether or not a model typed it.

We can't stop anyone from working the way they want to. We can set guardrails so what lands
is as good as it can be. The rest of this file is those guardrails. Run the tests, match the
code around yours, stay inside the request, and report failures instead of guessing past them.

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

## Setup & build

```bash
npm --prefix Dashboard ci
npm --prefix Dashboard run build   # bundles are embedded resources, build them first
dotnet build                       # StyleCop and Sonar analyzers run here
```

`dotnet build` works without the dashboard bundles, but the dashboard then serves a
placeholder. CI builds `-c Release` and Sonar gates on it, so treat a warning as a failure.

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

## Code style

- Formatter and linter: `.editorconfig` plus StyleCop for C#, oxlint for the dashboard. Run
  them; do not hand-format.
- Vale checks prose in `README.md` and `Docs/**/*.md`, and lychee checks the links.
- Follow the patterns already in neighboring files.
- Do not add comments that restate the code.
- Do not reformat code you are not otherwise changing.

## Git workflow

- Work on `main`. This repo has no feature branches and no pull requests.
- Commit format: Conventional Commits, one line, lowercase. semantic-release reads them.
- Never commit, push, or open a PR unless asked.
- All CI checks must pass. `release.yml` cuts a release from every push to `main`.

## Boundaries

- Do not modify unrelated files or widen scope beyond the request.
- Do not add dependencies without asking.
- Never commit secrets, API keys, or .env files.
- Every new step needs a row in `Docs/steps.md`.
  `.github/scripts/check-step-docs.py` fails CI when a step has no row.
- Adding a patch means three edits: a hook body in `PickleHooks`, a registration in
  `HarmonyBackend.Apply`, and the matching one in `ConcordBackend.Apply`.
- If a command fails, report the failure. Do not guess or present assumptions as confirmed
  results.
