Feature: thought steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a memory carries its own mood offset
    Given a colonist "Tired" exists
    When "Tired" is given thought "SleepDisturbed"
    Then "Tired" has thought "SleepDisturbed"
    And "Tired" thought "SleepDisturbed" mood offset is -1.0
    And "Tired" has no thought "AteWithoutTable"

  Scenario: a social memory moves opinion
    Given a colonist "Ann" exists
    And a colonist "Bob" exists
    When "Bob" is given thought "Insulted" about "Ann"
    Then "Bob" has thought "Insulted"
    And "Bob" opinion of "Ann" is below 0

  Scenario: a relation can be made and read back
    Given a colonist "Wed" exists
    And a colonist "Lock" exists
    And I remember "Wed" opinion of "Lock"
    When I make "Wed" and "Lock" "Spouse"
    Then "Wed" and "Lock" are "Spouse"
    And "Wed" opinion of "Lock" rose
