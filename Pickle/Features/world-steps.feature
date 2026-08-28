Feature: world steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: colonist, stockpile, building, research, and speed steps all work
    Given a colonist "Newbie" exists
    And a colonist "Newbie" exists
    And 50 "Steel" is spawned at the stockpile
    And a "Wall" is built at (140, 155)
    And research "Smithing" is finished
    And game speed is fast
    When I wait 30 ticks
    Then the engine is alive
    When I open the "Research" tab
    Then window "MainTabWindow_Research" is open
    When I press key "Escape"
    Then window "MainTabWindow_Research" is closed
