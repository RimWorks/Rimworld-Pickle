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
  # A warning the suite owns, so attribution is asserted against something that exists rather
  # than against a silence. Both names must find it: the display name RimLogging records, and
  # the packageId every neighbouring step takes.
  Scenario: an attributed warning is found by display name and by packageId
    When Pickle logs a warning for its own tests
    Then a warning from mod "Pickle" was logged
    And a warning from mod "rimworks.pickle" was logged
    And a warning matching "pickle-attribution-canary" was logged
