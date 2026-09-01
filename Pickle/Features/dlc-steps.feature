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

  @requires:Ideology
  Scenario: a ritual can be started
    Given a colonist "Host" exists
    And a "RitualSpot" is built at (138, 168)
    When I start ritual "RoleChange"
    Then a ritual "RoleChange" is running

  @requires:Anomaly
  Scenario: an entity can be held on a platform
    Given a "HoldingPlatform" is built at (137, 168)
    Then the platform at (137, 168) is empty
    When I spawn a "Fingerspike" pawn at (138, 166)
    And I contain "Fingerspike" on the platform at (137, 168)
    Then the platform at (137, 168) holds "Fingerspike"

  @requires:NoSuchModIsInstalled
  Scenario: a scenario for a mod nobody has is skipped
    Then the engine is alive
