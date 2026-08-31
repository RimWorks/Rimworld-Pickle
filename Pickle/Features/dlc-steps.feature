Feature: dlc steps

  Background:
    Given the save "test-colony" is loaded

  @requires:Ideology
  Scenario: the player ideo reports its precepts
    Then the ideo has precept "Conversion"
    And the ideo has precept "Cannibalism_Horrible"
    And the ideo has no precept "Cannibalism_Preferred"

  @requires:Royalty
  Scenario: a title can be granted and read back
    Given a colonist "Noble" exists
    Then "Noble" psylink level is 0
    When I give "Noble" the title "Freeholder"
    Then "Noble" has title "Freeholder"

  # The monolith in this fixture is not activated, so it reads as never studiable.
  @requires:Anomaly
  Scenario: the monolith reports its study knowledge
    Then the "VoidMonolith" at (191, 117) study knowledge is above -1

  @requires:NoSuchModIsInstalled
  Scenario: a scenario for a mod nobody has is skipped
    Then the engine is alive
