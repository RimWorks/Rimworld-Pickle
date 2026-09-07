# Pickle: RimWorld mod testing framework

Write automated tests for your RimWorld mod and run them inside the game. Pickle reads Gherkin scenarios and plays them against a live session. Your tests click the real UI, wait on real game state, and check the running simulation. No mocks.

```
Scenario: a drafted colonist waits for combat
  Given the save "test-colony" is loaded
  Given a colonist "Soldier" exists
  When I draft "Soldier"
  And I wait 30 ticks
  Then "Soldier" is drafted
  And "Soldier" has job "Wait_Combat"
```

## Who this is for

Mod authors. Pickle adds nothing to a normal game, so there is no reason to subscribe unless you are writing or testing a mod.

## How is this different from RimTest Redux?

RimTest Redux and the other RimWorld test frameworks run unit tests. You write C# test methods and assert against your own code, which is the right tool when the thing you are checking is a function.

Pickle works a level up. A scenario loads a save, drafts a real colonist, clicks a real button, waits real ticks, and asserts against the colony that came out. That catches bugs unit tests cannot see. A def that fails to load, a patch that conflicts, a job driver that stalls on tick 400.

The two fit together. Unit test your math, run Pickle against your colony.

## How to test a RimWorld mod

Pickle gives you a test runner window in development mode. It lists every mod that ships a suite, runs the scenarios you pick, and shows each step with its timing.

When a step fails, Pickle pauses the game on the broken state so you can look at the colony that caused it. It also captures a screenshot, the log tail, and the state of every colonist.

## Do you need to write C#?

Not to start. Feature files on their own need no build. Pickle ships steps for saves, world setup, colonist stats, surgery, thoughts, jobs, bills, zones, weapons, apparel, alerts, the camera, and the interface. Write your own step definitions in C# when you need something it does not cover.

## Can you run it in CI?

Yes. Runs go unattended and write a JUnit file, Cucumber messages, and a report page you can open straight from disk. A failed scenario turns into an annotation on the pull request instead of a line in a log nobody opens.

You can also watch a run from a browser on any machine. That is the only way to watch a run with no window open.

## Requirements

RimWorld 1.6 and one patching library. Harmony or Concord both work. Pickle prefers Concord when you have both.

## Getting started

Start from the template repository, or add a Pickle folder to a mod you already have. The docs cover steps, tags, fixtures, and waits.

Source and documentation: https://github.com/RimWorks/Rimworld-Pickle

## More modding tools from RimWorks

- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067): boot straight into a configured colony from the dev quicktest menu.
- [RimLogging](https://steamcommunity.com/sharedfiles/filedetails/?id=3733484696): structured log viewer and one-click bug report sharing.
- [RimObs](https://steamcommunity.com/sharedfiles/filedetails/?id=3733585062): performance profiler that finds which mod is eating your TPS.
