@film
Feature: pawn steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: health and hediff steps read a pawn's real condition
    Given a colonist "Patient" exists
    And I heal "Patient"
    Then "Patient" has no hediffs
    And "Patient" is healthy
    And "Patient" health is above 90 percent
    When "Patient" is given hediff "Flu"
    Then "Patient" has hediff "Flu"
    When "Patient" is cured of hediff "Flu"
    Then "Patient" has no hediff "Flu"

  Scenario: a downed pawn reports downed
    Given a colonist "Faller" exists
    When I kill "Faller"
    Then "Faller" is dead

  Scenario: needs and mood can be set and read
    Given a colonist "Hungry" exists
    When "Hungry" needs "Food" is set to 10 percent
    Then "Hungry" needs "Food" is below 20 percent
    When "Hungry" needs "Mood" is set to 90 percent
    Then "Hungry" mood is above 80 percent

  Scenario: skills can be set and read
    Given a colonist "Jet" exists
    When "Jet" skill "Shooting" is set to level 12
    Then "Jet" has skill "Shooting" at level 12

  Scenario: carried counts read a pawn's real inventory
    Given a colonist "Empty" exists
    Then "Empty" is carrying 0 "Silver"

  Scenario: one pawn can be ordered to attack another
    Given a colonist "Fighter" exists
    And a colonist "Victim" exists
    When "Fighter" attacks "Victim"
    Then "Fighter" is drafted
    And the engine is alive
