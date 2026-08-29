Feature: stat steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a thing stat reads the value its def declares
    When I spawn a "Steel" at (140, 155)
    Then the "Steel" at (140, 155) stat "MarketValue" is 1.9
    And the "Steel" at (140, 155) stat "Mass" is 0.5

  Scenario: a pawn stat reads the live value, not the def default
    Given a colonist "Runner" exists
    Then "Runner" stat "MoveSpeed" is above 2
    And "Runner" stat "MoveSpeed" is below 10

  Scenario: a whole number matches an exact stat value
    When I spawn a "Silver" at (141, 155)
    Then the "Silver" at (141, 155) stat "MarketValue" is 1
