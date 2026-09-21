Feature: warning steps

  Scenario: loading a save proves the capture path end to end
    Given the save "test-colony" is loaded
    Then a warning matching "hidden ritual precept was missing" was logged

  Scenario: a substring nothing warned with reports as absent
    Then no warning matching "pickle-warning-steps-canary" was logged
    And 0 warnings matching "pickle-warning-steps-canary" were logged

  Scenario: a mod nothing warns from reports clean
    Then no warnings from mod "RimLogging"

  # The same mod, named the other way. Every neighbouring step - @requires:, mod ... is loaded -
  # takes a packageId, so a scenario is written with one sooner or later; before the fix this step
  # let it past the requirement and then compared it against an attribution that is always a
  # display name, so it matched nothing and passed whatever the mod had logged.
  Scenario: a packageId names the same mod as its display name does
    Then no warnings from mod "rimworks.rimlogging"
    And no warnings from mod "RimLogging"
