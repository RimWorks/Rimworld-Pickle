# Authoring guide

This guide covers the four things you write: steps, tags, fixtures, and waits.
For what already exists, see [built-in steps](steps.md).

Pickle reads standard [Gherkin](https://cucumber.io/docs/gherkin/reference/). Any
feature file a Cucumber user recognises works here.

## Steps

A step class carries `[PickleSteps]`. Each step method carries `[Given]`, `[When]`, or
`[Then]` with a [Cucumber Expression](https://github.com/cucumber/cucumber-expressions).
Every step method takes a `PickleContext` first.

```csharp
using RimWorks.Pickle;

[PickleSteps]
public class GreeterSteps {
  [When("I greet {string}")]
  public void Greet(PickleContext ctx, string name) {
    ctx.Set(Greeter.Greet(name));
  }

  [Then("the greeting is {string}")]
  public void AssertGreeting(PickleContext ctx, string expected) {
    string actual = ctx.Get<string>();
    ctx.Assert(actual == expected, $"greeting should be '{expected}'; actual: '{actual}'");
  }
}
```

The keyword you use in the feature file does not have to match the attribute. Pickle
matches on the expression text alone.

### Share state between steps

`ctx.Set<T>` and `ctx.Get<T>` hold values for the length of one scenario. Pickle
creates a new context for each scenario, so nothing leaks between them.

### Write good failure messages

The message is the only thing a reader gets when a step fails. Include the actual
value, not only the expected one.

```csharp
ctx.Assert(
    pawn.CurJobDef?.defName == jobDefName,
    $"pawn '{nickname}' should have job '{jobDefName}'; actual state: {PawnState.Describe(pawn)}");
```

Use `ctx.Require` instead when the problem is a broken setup rather than a failed
expectation. A failed `Require` reports the scenario as an error, not a failure.

### Dump state on failure

Mark a method with `[PickleStateDump]` to capture context whenever a scenario in that
suite fails. Pickle calls it and attaches the string to the report.

```csharp
[PickleStateDump]
public string ColonistState() {
  Map? map = Find.CurrentMap;
  return map == null ? "no current map" : PawnState.DescribeColonists(map);
}
```

## Tags

Put tags on the line before a `Feature` or a `Scenario`. A feature tag applies to every scenario in
that feature.

| Tag | Effect |
| --- | --- |
| `@wip` | Skip unless you pass `-pickle-include-wip` |
| `@timeout:60` | Give this scenario 60 seconds instead of the default |
| `@seed:42` | Use this random seed instead of the run seed |
| `@retry:2` | Give this scenario 2 more attempts before it fails. Beats `-pickle-retry` |
| `@same-world` | Skip the fixture reload and keep the previous world |
| `@allow-errors` | Do not fail the scenario when the game logs an error |
| `@film` | Record the scenario and attach the video to the report |
| `@watch` | Make wait steps pass real time instead of driving ticks |
| `@quickstart:Name` | Build the starting world from a Quickstarts quickstart |

A film stops at sixty seconds by default, and Pickle logs when it does. Raise it with
`-pickle-max-film-seconds`. Nothing sets the game's own speed for you, so add
`game speed is fast` when you want the film to show more than real time.

`@watch` matters most with `@film`. A run drives sixty ticks per rendered frame by
default, so `I wait 10000 ticks` finishes in seconds and films almost nothing. Under
`@watch` the same wait takes the real minutes it describes, and the video shows them.

`@film` captures one full size jpeg as each step finishes. The report links them rather
than embedding them, and turns them into a video when `ffmpeg` is installed. Encoding a
frame stalls the frame it lands on, so tag a scenario while you work out why something
looks wrong, then take it off. See [reports](reports.md) for where the files land.

`@same-world` makes a scenario faster, because loading a save takes about ten seconds.
It also couples that scenario to the one before it. Use it only when the coupling is
what you want to test.

## Fixtures

A fixture is a saved game in `Pickle/Fixtures/`. Load one with a built-in step:

```gherkin
Given the save "test-colony" is loaded
```

Pickle looks in your mod first, then in every other mod that ships fixtures. Name
fixtures for the state they capture, not for the test that uses them.

To make a fixture, play until the game is in the state you want. Then open the runner
and select **Save fixture**. Pick the target mod and a name.

To see the fixtures you already have, select **Fixtures**. The list groups them by the mod
that owns them. Each row gives you the size, the date, and the scenario and game version
from the file itself. From there you can load a fixture, rename it, or delete it. Loading one
takes the same path as the `Given the save ... is loaded` step. Check what a recording
holds without writing a scenario for it first.

### Where Pickle writes a recorded fixture

Pickle cannot count on the mod folder being writable. A Workshop install and a Docker
container both mount it read-only. The save then fails, and you lose the session that
produced it.

Pickle tests the mod directory at startup and picks a location from the result:

| Situation | Location |
|---|---|
| `-pickle-fixtures-dir=<path>` given | `<path>/<mod folder name>/` |
| Mod directory is writable | The mod's own `Pickle/Fixtures/`, unchanged |
| Mod directory is not writable | `<save data folder>/PickleFixtures/<mod folder name>/`, and Pickle logs it |

When the fallback is in use, Pickle scans that directory and the mod's own
`Pickle/Fixtures/`. A fixture then works as soon as you record it. If both hold the same name
the recorded one wins, because re-recording is how you replace a fixture. Pickle logs a
warning naming both paths, since a stale recording that beats the committed copy passes on
your machine and fails in CI.

Copy the file into `Pickle/Fixtures/` and commit it when you are happy with it. That is
where it belongs long term, and it is the copy other people get.

### A quickstart instead of a save

If you use [Quickstarts](https://github.com/RimWorks/Rimworld-Quickstarts), a scenario can build
its world from code instead of loading a save:

```gherkin
@quickstart:OnePlanetParityQuickstart
Feature: parity
```

The name is the quickstart class name, the same one `-quickstart=` takes. Pickle builds
the world before the first step runs, so the scenario reads as if the state was there
already. `@same-world` skips the rebuild between scenarios, exactly as it does for a save.

A quickstart is not a save, so nothing goes into git and nothing goes stale when a def
changes. The cost is that it regenerates every run, which takes as long as world
generation does.

A scenario cannot do both. Tag it `@quickstart:` and also run `the save "..." is loaded`
and Pickle rejects that feature at startup. The log names the file, the quickstart, and
the step. The rest of the suite still loads.

Pickle finds Quickstarts by reflection, so it stays optional. Nothing breaks if the mod
is absent until a scenario asks for a quickstart, and that scenario then fails saying so.

Loading a save reloads the Unity scene. Any object you captured before the load is
stale afterwards, so read game state again after a fixture step.

## Waits

RimWorld assigns much of its state on the next think cycle, not on the tick you acted.
A step that asserts immediately after an action races the game.

`ctx.AssertEventually` polls until the condition holds, then reports. It builds the
failure message at the moment it gives up, so the message shows the final state.

```csharp
await ctx.AssertEventually(
    () => pawn.drafter?.Drafted == true,
    () => $"pawn '{nickname}' should be drafted; actual state: {PawnState.Describe(pawn)}");
```

These waits are also available:

| Call | Waits for |
| --- | --- |
| `ctx.WaitTicks(n)` | `n` game ticks |
| `ctx.WaitFrames(n)` | `n` rendered frames |
| `ctx.WaitUntil(cond, seconds)` | `cond` to return true, or a timeout |

Await every wait. Pickle resumes your step on the game's main thread, so you can read
game state directly after the await.

Do not use `Task.Delay` or `Thread.Sleep`. They block the thread the game ticks on, so
the state you wait for never arrives.

### Steps that wait longer than five seconds

The runner fails any step that runs longer than five seconds. A step that waits on the
simulation needs more room, so declare it on the attribute:

```csharp
[When("I wait for the caravan to arrive", TimeoutSeconds = 35f)]
public async Task WaitForCaravan(PickleContext ctx) {
    await ctx.WaitUntil(() => Find.CurrentMap.mapPawns.AnyColonistSpawned, 30f);
}
```

Set the attribute a few seconds longer than the wait inside the step. The runner then reports
your own failure message instead of a bare timeout. `@timeout:60` on a scenario does the
same for every step in it, and the attribute wins where both apply.

## Input

| Call | Action |
| --- | --- |
| `ctx.Click(tag)` | Click a tagged widget |
| `ctx.Hover(tag)` | Move the pointer over a tagged widget |
| `ctx.PressKey(key)` | Send a key press |

Tag a widget with `PickleUI.Tag("my-button", rect)` inside your own drawing code.
Pickle tags vanilla buttons by label, as `btn:Research`.

Clicks need a real X display, because RimWorld drops synthetic pointer events. Key
presses work without one.
