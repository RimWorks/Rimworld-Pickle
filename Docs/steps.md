# Built-in steps

Pickle ships these steps. Write your own for anything your mod does; see the
[authoring guide](authoring.md).

`{string}` takes a quoted value. `{int}` takes a whole number. `{word}` takes a bare
word. The keyword you write in a feature file does not have to match the table below,
because Pickle matches on the expression text alone.

## Fixtures

| Step | Does |
| --- | --- |
| `the save {string} is loaded` | Loads a fixture from any mod's `Pickle/Fixtures/`. Tag the scenario `@same-world` to reuse the world the previous scenario left |

Most scenarios start here. Loading a save reloads the Unity scene, so read game state
again afterwards.

## World setup

| Step | Does |
| --- | --- |
| `a colonist {string} exists` | Finds the named colonist, or spawns one |
| `{int} {string} is spawned at the stockpile` | Spawns items into the first stockpile |
| `a {string} is built at ({int}, {int})` | Places a finished building at a map cell |
| `research {string} is finished` | Marks a research project complete |
| `game speed is {word}` | Sets the speed: `paused`, `normal`, `fast`, `superfast` |

## Simulation

| Step | Does |
| --- | --- |
| `I wait {int} ticks` | Advances the game. Fast mode drives ticks directly, so a long wait costs no real time |
| `I draft {string}` | Drafts a colonist |
| `I undraft {string}` | Undrafts a colonist |
| `I kill {string}` | Kills a pawn |
| `incident {string} fires` | Fires an incident with default points |
| `incident {string} fires with {int} points` | Fires an incident at a set threat level |
| `{string} is drafted` | Checks the pawn is drafted |
| `{string} has job {string}` | Checks the pawn's current job def |
| `{string} is dead` | Checks the pawn is dead |
| `a letter {string} has arrived` | Checks a letter whose label contains the text |

The three pawn checks wait for the state to settle before they report. RimWorld assigns
much of a pawn's state on the next think cycle, so a check that fired at once would race
the game.

## Interface

| Step | Does |
| --- | --- |
| `I click {string}` | Clicks a tagged widget |
| `I click button {string}` | Clicks a vanilla button by label. Pickle tags those for you |
| `I click gizmo {string}` | Runs a gizmo on the current selection |
| `I hover {string}` | Moves the pointer onto a tagged widget |
| `I press key {string}` | Sends a key. Accepts `Escape`, `Return`, `Space`, `Tab`, `Delete`, `Backspace`, a letter, or a digit |
| `I select {string}` | Selects a pawn or thing by name |
| `I open the {string} tab` | Opens a main tab by def name or label |
| `I close all dialogs` | Closes every open window |
| `window {string} is open` | Checks a window type is open |
| `window {string} is closed` | Checks a window type is closed |
| `the inspect pane shows {string}` | Checks the selected thing's label |
| `no errors were logged` | Fails if the game logged an error during the scenario |
| `I take a screenshot {string}` | Captures a screenshot and attaches it to the report |

Clicks need a real X display, because RimWorld drops synthetic pointer events. Key
presses work without one. See [running tests](running.md).

## Tagging your own widgets

`I click {string}` resolves a tag you set while drawing:

```csharp
PickleUI.Tag("my-button", buttonRect);
```

Vanilla buttons need no tagging. Pickle records them by label, so
`I click button "Research"` works out of the box.
