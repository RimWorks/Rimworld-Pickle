Feature: vanilla smoke

  Background:
    Given the save "test-colony" is loaded

  Scenario: research tab opens and closes
    When I open the "Research" tab
    Then window "MainTabWindow_Research" is open
    When I press key "Escape"
    Then window "MainTabWindow_Research" is closed

  Scenario: a raid arrives and raises the alarm
    When incident "RaidEnemy" fires with 500 points
    And I wait 120 ticks
    Then a letter "Raid" has arrived
