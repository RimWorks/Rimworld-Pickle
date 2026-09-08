Feature: storyteller steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: the storyteller can be swapped and read back
    When I set the storyteller to "Randy"
    Then the storyteller is "Randy"
    When I set the storyteller to "Cassandra"
    Then the storyteller is "Cassandra"

  Scenario: the difficulty reads the fixture's setting
    Then the difficulty is "Rough"

  Scenario: colony wealth reads above zero and breaks out by category
    Then colony wealth is above 0
    And colony wealth is below 1000000
    And colony wealth in items is above 0
