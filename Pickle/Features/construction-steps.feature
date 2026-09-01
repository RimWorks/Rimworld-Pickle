Feature: construction steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a blueprint can be placed and read back
    When I designate a "Wall" from (140, 150) to (142, 150)
    Then a blueprint for "Wall" is at (141, 150)

  Scenario: a stockpile can be created and covers its cells
    When I create a stockpile from (150, 160) to (152, 162)
    Then a stockpile covers (151, 161)

  Scenario: the build designator places a blueprint
    When I use the build designator for "Wall" at (143, 150)
    Then a blueprint for "Wall" is at (143, 150)

  @slow
  Scenario: a colonist builds what was designated
    Given a colonist "Builder" exists
    And "Builder" has childhood "ShopKid36"
    And "Builder" has backstory "Blacksmith7"
    Then "Builder" can do "Construction"
    Given "Builder" skill "Construction" is set to level 12
    And 100 "WoodLog" is spawned at the stockpile
    When I set "Builder" priority "Construction" to 1
    And I designate a "Wall" from (145, 150) to (145, 150)
    Then a blueprint for "Wall" is at (145, 150)
    Given game speed is ultrafast
    When I wait for the "Wall" at (145, 150) to be built
    Then a "Wall" is at (145, 150)
