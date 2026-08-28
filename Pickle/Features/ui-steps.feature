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
