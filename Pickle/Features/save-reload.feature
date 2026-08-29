Feature: save and reload steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a hediff survives a save and reload
    Given a colonist "Keeper" exists
    And "Keeper" is given hediff "Flu"
    When I save and reload
    Then "Keeper" has hediff "Flu"
    And no errors were logged

  Scenario: the round trip step fails on a scribe error
    Given a colonist "Rider" exists
    And "Rider" skill "Shooting" is set to level 12
    Then the save round trips
    And "Rider" has skill "Shooting" at level 12

  Scenario: a named save is kept for inspection
    Given a colonist "Named" exists
    When I save and reload as "pickle-roundtrip-named"
    Then the engine is alive
