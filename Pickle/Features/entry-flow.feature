Feature: entry flow

  Scenario: the main menu is clickable
    Given the main menu is open
    When I click button "New colony"
    Then window "Page_SelectScenario" is open

  # A tag is recorded in GUI space and multiplied by Prefs.UIScale when the pointer moves, so a
  # click only exercises that conversion where the two spaces differ. At 100% they coincide, and a
  # rect stored half in screen space still lands on its button - which is why a tag store that
  # converted with GUIToScreenRect passed every scenario here for months. At 150% the click lands
  # away from its button and the page never opens - off the bottom of the screen for a widget low in
  # its window, merely in the wrong place for one in the middle of the menu, as this one is.
  Scenario: a button is clickable at another interface scale
    Given the main menu is open
    And the interface scale is 150 percent
    When I click button "New colony"
    Then window "Page_SelectScenario" is open

  Scenario: a page's bottom buttons drive the stack
    Given the main menu is open
    When I click button "New colony"
    And I click button "Next"
    Then window "Page_SelectStoryteller" is open
    When I click button "Back"
    Then window "Page_SelectScenario" is open
