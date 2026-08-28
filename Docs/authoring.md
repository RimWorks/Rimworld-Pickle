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
using Pickle;

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
| `@same-world` | Skip the fixture reload and keep the previous world |
| `@allow-errors` | Do not fail the scenario when the game logs an error |

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
