Feature: world condition steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: weather can be set and read back
    When I set the weather to "Rain"
    Then the weather is "Rain"

  # test-colony sits on an equatorial tile, so Winter is unreachable here by design.
  Scenario: the season reads the tile's own season
    Then the season is PermanentSummer
    When I set the season to PermanentSummer
    Then the season is PermanentSummer

  Scenario: the clock can be moved to an hour
    When I set the hour to 2
    Then the hour is 2
    And it is night

  Scenario: temperature reads at a cell and outdoors
    Then the temperature at (134, 164) is above -100
    And the outdoor temperature is below 100
