# Built-in steps

Pickle ships these steps. Write your own for anything your mod does; see the
[authoring guide](authoring.md).

`{string}` takes a quoted value. `{int}` takes a whole number. `{word}` takes a bare
word. The keyword you write in a feature file does not have to match the table below,
because Pickle matches on the expression text alone.

## Defs

| Step | Does |
| --- | --- |
| `def {string} exists` | Finds a def by name in any database |
| `no def {string} exists` | Checks nothing owns that name. Use it on a def a patch removes |
| `def {string} of type {string} exists` | Names the database, for when two share a def name |
| `def {string} field {string} is {string}` | Reads a field or property. Dotted paths walk into nested objects |
| `def {string} costs {int} {string}` | Counts a thing in the def's `costList` |
| `def {string} stat {string} is {float}` | The stat value the game computes, made without stuff |
| `def {string} raw stat {string} is {float}` | The `statBases` entry exactly as the XML sets it |
| `def {string} is defined by mod {string}` | Matches the mod name or its packageId |
| `def {string} was patched by mod {string}` | Names the mod whose XML patch changed the def |
| `def {string} was patched` | Checks any mod patched it |
| `no def {string} was patched` | Checks no mod patched it |

**These need no save.** A scenario with no `the save ... is loaded` step runs at the main
menu, where the def database is already built. The five scenarios in `def-steps.feature`
finish in about 1.4 seconds together, against 10 to 15 seconds for a single scenario that
loads a fixture. An XML-only mod can test everything it ships this way.

`stat` and `raw stat` answer different questions. `raw stat` reads the `statBases` entry
and fails when the def has none. `stat` reads what the game computes, which falls back to
the stat's own default when the def never lists it. Use `raw stat` to prove a patch wrote
a value, and `stat` to prove the game uses it.

A lookup that misses searches every database for close matches, so a typo comes back as
`closest matches: Wall (ThingDef), Walls (DrawStyleCategoryDef)` rather than a bare miss.

RimWorld records that a def was patched but never by whom, and it drops every patch object
once loading ends. Pickle reads the patch tree just before the patches run, matches each
xpath against the document, and keeps only the operations that changed something. The three
`was patched` steps accept a def name held by two databases, because a patch targets a name
rather than a type.

This needs Harmony or Concord loaded before the game applies XML patches. When that did not
happen the steps fail and say so, instead of reporting every def as unpatched.

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
| `game speed is {word}` | Sets the speed: `paused`, `normal`, `fast`, `superfast`, `ultrafast` |

## Shaping a colonist

| Step | Does |
| --- | --- |
| `{string} has backstory {string}` | Forces the adulthood backstory |
| `{string} has childhood {string}` | Forces the childhood backstory |
| `I give {string} the trait {string}` | Adds a trait |
| `I give {string} the trait {string} at degree {int}` | Adds a degreed trait, such as a level of Psychopath |
| `I take the trait {string} from {string}` | Removes a trait |
| `{string} is {int} years old` | Sets biological age |
| `{string} gender is {word}` | `male` or `female` |
| `{string} has {word} passion for {string}` | `none`, `minor` or `major` |
| `{string} can do {string}` | Checks a work type is enabled |
| `{string} cannot do {string}` | Checks a work type is disabled |

A generated colonist is random, so a scenario that needs one to cook, craft or shoot
should say so rather than hope. Backstories and traits both disable work, which is why a
test that only set a skill level could still meet a pawn that refuses the job.

Changing a backstory or trait drops the pawn's disabled-work cache, so the new
capabilities apply straight away.

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

## Stats

| Step | Does |
| --- | --- |
| `{string} stat {string} is {float}` | Checks a pawn's stat value |
| `{string} stat {string} is above {float}` | Checks a pawn's stat is higher |
| `{string} stat {string} is below {float}` | Checks a pawn's stat is lower |
| `the {string} at ({int}, {int}) stat {string} is {float}` | Checks a stat on a thing at a map cell |

A `StatPart` or a `StatWorker` patch changes the number without changing the def, so no
other check can see one. Write the value as a whole number or with decimals; both work.

An exact check allows 0.01, or 0.1% of the expected value when that is larger. A flat
tolerance breaks on `MarketValue` in the thousands, and a relative one breaks near zero.

A failure prints the stat breakdown and attaches the full version to the report, so you
see which part moved the number. When the stat does not apply to the thing you asked
about, the failure says so, because the value you are reading is the def default.

## Weapons and apparel

| Step | Does |
| --- | --- |
| `I equip {string} with {string}` | Makes the weapon and puts it in the pawn's hands |
| `I equip {string} with {string} made of {string}` | Same, with the stuff named |
| `I dress {string} in {string}` | Makes the apparel and wears it |
| `I dress {string} in {string} made of {string}` | Same, with the stuff named |
| `I strip {string}` | Drops every weapon and worn item at the pawn's feet |
| `I destroy the gear of {string}` | Removes the same items without leaving them on the map |
| `{string} is wielding {string}` | Checks the equipped weapon |
| `{string} is wielding nothing` | Checks the pawn holds no weapon |
| `{string} is wearing {string}` | Checks worn apparel for a def |
| `{string} apparel covers {string}` | Checks a body part group is covered |

`{string} is carrying {int} {string}` only counts hands and inventory. A weapon and worn
apparel live in different places, so none of this was reachable through it.

Naming no stuff for something built from stuff picks the default, the same as
`I spawn a {string} at`. Stuff changes apparel stats, so name it when the numbers matter.

A pawn missing the body part fails the step and says so. A clash on the same apparel layer
is not a failure: the item already there is dropped, which is what the game does.

`apparel covers` reads everything the pawn wears, not only what you dressed it in. A
generated colonist arrives in a shirt and pants, so `Legs` is covered before you start.

## Bills and work

| Step | Does |
| --- | --- |
| `I add bill {string} to the {string}` | Adds a recipe to the first bench of that def |
| `I add bill {string} to the {string} at ({int}, {int})` | Adds it to the bench at a cell |
| `the {string} has {int} bills` | Counts the bill stack without waiting for anything |
| `I set {string} priority {string} to {int}` | Sets a work type priority |
| `{string} priority {string} is {int}` | Checks one |
| `I wait for bill {string} to finish` | Waits for the recipe's product to appear |

Setting a priority turns manual priorities on first. The game keeps only 0 or 3 while they
are off, so `to 1` would read back as 3. A work type the pawn cannot do fails the step
instead of quietly staying at 0.

A bill has no finished event. `I wait for bill` reads the product count first and waits for
it to rise, so a map that already holds some still works. It allows 120 seconds, because a
craft needs real work done by a real pawn. A recipe that makes its output through
`specialProducts`, like `Make_StoneBlocksAny`, has no fixed product to count, and the step
says so rather than waiting out the clock.

An end to end craft is worth writing. Pickle seeds the game before each scenario, so the
same colonist makes the same choices every run.

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
