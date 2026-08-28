# Pickle

Pickle runs [Gherkin](https://cucumber.io/docs/gherkin/) scenarios inside a live
RimWorld session. Your tests click the real UI, wait on real game state, and assert
against the running simulation.

It is a test runner for mod authors, not a mod for players.

## Example

```gherkin
Feature: drafting
  Scenario: a drafted colonist waits for combat
    Given the save "test-colony" is loaded
    Given a colonist "Soldier" exists
    When I draft "Soldier"
    And I wait 30 ticks
    Then "Soldier" is drafted
    And "Soldier" has job "Wait_Combat"
```

The steps behind those lines are plain C#:

```csharp
[PickleSteps]
public class DraftingSteps {
  [When("I draft {string}")]
  public void Draft(PickleContext ctx, string nickname) {
    PawnLookup.RequireLiving(nickname).drafter!.Drafted = true;
  }
}
```

## Install

Pickle needs RimWorld 1.6 and one patching library. Either
[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) or
[Concord](https://steamcommunity.com/sharedfiles/filedetails/?id=3758333473) works.
Pickle prefers Concord when both are active, and logs which one it used.

1. Subscribe to Pickle on the Steam Workshop, or unzip a
   [release](https://github.com/cryptiklemur/Rimworld-Pickle/releases) into `Mods/`.
2. Enable Pickle and a patching library in the mod list.
3. Start a new mod from the
   [template](https://github.com/cryptiklemur/pickle-template), or add a `Pickle/`
   folder to an existing one.

Open the runner from the debug actions menu.

## Documentation

| Guide | Covers |
| --- | --- |
| [Getting started](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/getting-started.md) | Adding a suite to your mod |
| [Built-in steps](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/steps.md) | Every step Pickle ships |
| [Authoring](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/authoring.md) | Writing your own steps, tags, fixtures, and waits |
| [Running tests](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/running.md) | The runner, the browser dashboard, and seeds |
| [Autorun and CI](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/autorun.md) | Command-line flags and pipelines |
| [Reports](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/reports.md) | What each run writes, and what a failure captures |
| [Releasing](https://github.com/cryptiklemur/Rimworld-Pickle/blob/main/Docs/releasing.md) | How a release is cut and published |

## Building Pickle

```
npm --prefix Dashboard ci && npm --prefix Dashboard run build
dotnet build
```

Pickle embeds the dashboard bundles as resources, so build them first. Pickle still
builds without them, and the dashboard serves a placeholder.

## License

MIT
