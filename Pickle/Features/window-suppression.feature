# The scenarios run in order, and the third depends on it: it asserts that the suppression the
# second one never lifted was lifted anyway, by [AfterScenario]. Moved or run alone, it passes
# without proving anything.
Feature: window suppression

  Scenario: a cleared screen closes what is open, and drops what opens next
    When a window Pickle does not own opens
    Then window "Dialog_MessageBox" is open
    Given the screen is clear
    Then window "Dialog_MessageBox" is closed
    When a window Pickle does not own opens
    Then window "Dialog_MessageBox" is closed
    And no errors were logged

  Scenario: a window Pickle owns is spared
    Given the screen is clear
    When a window Pickle owns opens
    Then window "TagClickTestWindow" is open
    And no errors were logged

  Scenario: suppression does not outlive the scenario that asked for it
    When a window Pickle does not own opens
    Then window "Dialog_MessageBox" is open
    And no errors were logged

  Scenario: windows are allowed to open again, on demand
    Given the screen is clear
    When windows are allowed to open again
    And a window Pickle does not own opens
    Then window "Dialog_MessageBox" is open
    And no errors were logged
