@film
Feature: engine smoke

  Scenario: the fixture loads and the engine runs steps
    Given the save "test-colony" is loaded
    When I wait 60 ticks
    Then the engine is alive
