Feature: map steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a spawned thing is found on the map and at its cell
    When I spawn a "Steel" at (142, 155)
    Then a "Steel" exists
    And a "Steel" is at (142, 155)

  Scenario: a destroyed thing leaves its cell
    When I spawn a "WoodLog" at (143, 155)
    Then a "WoodLog" is at (143, 155)
    When I destroy the "WoodLog" at (143, 155)
    Then no "WoodLog" is at (143, 155)

  Scenario: the stockpile counts what was spawned into it
    Then the stockpile holds 0 "Plasteel"
    Given 20 "Plasteel" is spawned at the stockpile
    Then the stockpile holds 20 "Plasteel"
