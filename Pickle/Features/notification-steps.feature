Feature: notification steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a raid produces a letter that can be waited on and dismissed
    When incident "RaidEnemy" fires
    And I wait for letter "raid"
    When I dismiss letter "raid"
    Then the engine is alive

  Scenario: dismissing the last letter empties the stack
    When I dismiss letter "monolith"
    Then no letters are pending

  Scenario: a colony with no crisis has no matching alert
    Then alert "Zzz never an alert" is not active
