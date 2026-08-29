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

## Save and reload

| Step | Does |
| --- | --- |
| `I save and reload` | Saves the running game, loads it straight back, and deletes the file |
| `I save and reload as {string}` | Same, into a named save you can open afterwards |
| `the save round trips` | Saves and reloads, then fails if anything hit the error log during the trip |

These catch a broken `ExposeData`, which is the most common way a mod loses state. Put
your normal checks after the reload and they test the saved copy instead of the live one.

`the save round trips` only counts errors logged during the trip itself, so an error from
earlier in the scenario does not fail it. Use `no errors were logged` for that.

A reload replaces every object in the game. Steps that look a pawn up by name work
unchanged, because they resolve the name each time. Anything a step of your own stored
with `ctx.Set<T>()` still points at the old game and needs setting again.

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
| `the engine is alive` | Checks the game is still running and ticking |

The three pawn checks wait for the state to settle before they report. RimWorld assigns
much of a pawn's state on the next think cycle, so a check that fired at once would race
the game.

## Waiting

| Step | Does |
| --- | --- |
| `I wait for letter {string}` | Waits for a letter whose label contains the text |
| `I wait for {string} to have job {string}` | Waits for the pawn to take a job def |
| `I wait for a {string} to exist` | Waits for a thing def to appear on the map |
| `I wait for research {string} to finish` | Waits for a research project to complete |
| `I wait until {string} is drafted` | Waits for the pawn to be drafted |
| `I wait until {string} reaches the stockpile` | Waits for the pawn to stand inside a stockpile zone |
| `I wait until {string} stops moving` | Waits for the pawn to finish walking, however far it is |

Each of these gives up after 30 seconds and fails with the state it found. Prefer them
over `I wait {int} ticks`. A tick count is a guess about how long the game needs, and the
guess breaks on a faster machine.

## Pawn state

| Step | Does |
| --- | --- |
| `{string} is downed` | Checks the pawn is downed |
| `{string} is healthy` | Checks the pawn is neither downed nor in need of tending |
| `{string} has hediff {string}` | Checks the pawn carries a hediff def |
| `{string} health is above {int} percent` | Checks the pawn's summary health |
| `{string} is given hediff {string}` | Adds a hediff to the pawn |
| `{string} attacks {string}` | Drafts the pawn and orders a forced melee attack |
| `I order {string} to ({int}, {int})` | Sends the pawn to a cell and fails if it cannot reach |
| `I order {string} to the stockpile` | Sends the pawn to the first stockpile, without naming a cell |
| `I order {string} to the far side of the map` | Sends the pawn to the furthest cell it can reach |
| `{string} needs {string} is below {int} percent` | Checks a need level, such as `Food` |
| `{string} needs {string} is set to {int} percent` | Sets a need level |
| `{string} mood is above {int} percent` | Checks the pawn's mood |
| `{string} has skill {string} at level {int}` | Checks a skill level |
| `{string} skill {string} is set to level {int}` | Sets a skill level |
| `{string} has trait {string}` | Checks the pawn has a trait def |
| `{string} is carrying {int} {string}` | Counts a thing def across the pawn's hands and inventory |
| `{string} is carrying nothing` | Checks the pawn holds no items. A generated colonist often starts with drugs, so this rarely holds for a fresh one |

When one of these fails it reports what the pawn actually had. A failed skill check names
the real level, and a failed carry check lists everything the pawn holds.

## Map and things

| Step | Does |
| --- | --- |
| `a {string} exists` | Waits for at least one thing of a def to be on the map |
| `no {string} exists` | Checks no thing of a def is on the map |
| `{int} {string} exist` | Counts a thing def across the map, stacks included |
| `a {string} is at ({int}, {int})` | Checks a thing def occupies a cell |
| `no {string} is at ({int}, {int})` | Checks a thing def does not occupy a cell |
| `cell ({int}, {int}) is empty` | Checks a cell holds no things |
| `I spawn a {string} at ({int}, {int})` | Spawns a thing at a cell |
| `I destroy the {string} at ({int}, {int})` | Destroys the first matching thing at a cell |
| `the stockpile holds {int} {string}` | Counts a thing def in the first stockpile zone |

A cell outside the map fails with the map size, so a wrong coordinate does not read as a
missing object.

## Camera

| Step | Does |
| --- | --- |
| `I move the camera to ({int}, {int})` | Jumps the camera to a cell |
| `I move the camera to {string}` | Jumps the camera to a pawn |
| `I pan the camera to ({int}, {int})` | Pans there instead of cutting, which reads better on film |
| `I follow {string}` | Keeps the camera on a pawn every frame until you stop |
| `I stop following` | Releases the camera |
| `I zoom in` | One step closer |
| `I zoom out` | One step further |
| `I zoom all the way in` | Closest view |
| `I zoom all the way out` | Widest view |
| `the camera is looking at ({int}, {int})` | Checks the camera cell |
| `the camera can see {string}` | Checks the pawn is inside the view |

RimWorld has no follow of its own, so Pickle steers the camera once per rendered frame
while a follow is active.

## Alerts and messages

| Step | Does |
| --- | --- |
| `alert {string} is active` | Waits for an alert whose label contains the text |
| `alert {string} is not active` | Checks no active alert matches the text |
| `a message {string} was shown` | Waits for a toast message containing the text |
| `I dismiss letter {string}` | Removes a matching letter from the stack |
| `no letters are pending` | Checks the letter stack is empty |

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

## Dev mode

| Step | Does |
| --- | --- |
| `dev mode is enabled` | Turns on developer mode |
| `god mode is enabled` | Turns on developer mode and god mode |
| `god mode is disabled` | Turns off god mode |
| `I trigger debug action {string}` | Runs a debug action by name |
| `I trigger debug action {string} in category {string}` | Runs a debug action from one category |

A debug action is a static method that carries the `[DebugAction]` attribute. Pickle calls
the method directly, so the debug menu never opens. Any mod's actions work too, because
Pickle scans every loaded assembly.

An action that takes a target, such as a map cell or a pawn, cannot run this way. Pickle
fails that step and names the arguments the action wanted. An unknown name fails with up to
five close matches.

## Tagging your own widgets

`I click {string}` resolves a tag you set while drawing:

```csharp
PickleUI.Tag("my-button", buttonRect);
```

Vanilla buttons need no tagging. Pickle records them by label, so
`I click button "Research"` works out of the box.
