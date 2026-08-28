Feature: sim steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: draft, undraft, and job steps work
    Given a colonist "Soldier" exists
    When I draft "Soldier"
    And I wait 30 ticks
    Then "Soldier" is drafted
    And "Soldier" has job "Wait_Combat"
    When I undraft "Soldier"

  Scenario: a killed colonist is dead
    Given a colonist "Victim" exists
    When I kill "Victim"
    Then "Victim" is dead

  Scenario: an incident fires without explicit points
    When incident "RaidEnemy" fires
    And I wait 30 ticks
    Then the engine is alive
