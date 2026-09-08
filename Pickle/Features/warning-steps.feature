Feature: warning steps

  Scenario: loading a save proves the capture path end to end
    Given the save "test-colony" is loaded
    Then a warning matching "hidden ritual precept was missing" was logged

  Scenario: a substring nothing warned with reports as absent
    Then no warning matching "pickle-warning-steps-canary" was logged
    And 0 warnings matching "pickle-warning-steps-canary" were logged

  Scenario: a mod nothing warns from reports clean
    Then no warnings from mod "RimLogging"
