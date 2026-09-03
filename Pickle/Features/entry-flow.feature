Feature: entry flow

  Scenario: the main menu is clickable
    Given the main menu is open
    When I click button "New colony"
    Then window "Page_SelectScenario" is open

  Scenario: a page's bottom buttons drive the stack
    Given the main menu is open
    When I click button "New colony"
    And I click button "Next"
    Then window "Page_SelectStoryteller" is open
    When I click button "Back"
    Then window "Page_SelectScenario" is open
