# Pickle

Write tests for your RimWorld mod that run inside the game.

Pickle reads Gherkin scenarios and plays them against a live session. Your tests click the real UI, wait on real game state, and check the running simulation. No mocks.

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

## What you get

A test runner window in development mode. It lists every mod that ships a suite, runs the scenarios you pick, and shows each step with its timing.

When a step fails, Pickle pauses the game on the broken state so you can look at the colony that caused it. It also captures a screenshot, the log tail, and the state of every colonist.

You can watch a run from a browser instead, on any machine. That is the only way to watch a run with no window open.

Runs can also go unattended. Pickle writes a JUnit file, Cucumber messages, and a report page you can open straight from disk.

## Requirements

RimWorld 1.6 and one patching library. Harmony or Concord both work. Pickle prefers Concord when you have both.

## Getting started

Start from the template repository, or add a Pickle folder to a mod you already have. The docs cover steps, tags, fixtures, and waits.

Source and documentation: https://github.com/cryptiklemur/Rimworld-Pickle
