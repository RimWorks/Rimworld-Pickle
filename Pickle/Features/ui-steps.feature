@film
Feature: ui steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: select, gizmo, inspect, dialog, and evidence steps all work
    Given a colonist "Gizmo" exists
    When I select "Gizmo"
    Then the inspect pane shows "Gizmo"
    When I click gizmo "Draft"
    And I wait 5 ticks
    Then "Gizmo" is drafted
    When I take a screenshot "gizmo-drafted"
    When I open the "Research" tab
    Then window "MainTabWindow_Research" is open
    When I close all dialogs
    Then window "MainTabWindow_Research" is closed
    Then no errors were logged

  Scenario: a research tab is opened by def name and lists its project
    When I open the research tab "Main"
    Then window "MainTabWindow_Research" is open
    And the research window is on the tab "Main"
    And the research window lists the project "Stonecutting"
    When I close all dialogs
    Then no errors were logged

  @timeout:60
  Scenario: a filmstrip follows a colonist through a whole order
    Given the save "test-colony" is loaded
    And a colonist "Star" exists
    And game speed is ultrafast
    When I select "Star"
    And I follow "Star"
    And I zoom all the way in
    And I draft "Star"
    And I order "Star" to the far side of the map
    And I wait until "Star" stops moving
    And I zoom all the way out
    And I stop following
    And I undraft "Star"
    Then the engine is alive
