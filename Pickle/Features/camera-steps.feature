@film
Feature: camera steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: the camera holds an exact zoom
    When I zoom the camera to 20 cells
    Then the camera is zoomed to 20 cells

  Scenario: the camera zooms closer than the player's own limit
    When I zoom the camera to 3 cells
    Then the camera is zoomed to 3 cells

  Scenario: moving the camera reports the cell it landed on
    When I move the camera to (140, 150)
    Then the camera is looking at (140, 150)

  Scenario: zooming all the way in reaches the tightest zoom
    When I zoom all the way out
    And I zoom all the way in
    Then the camera is zoomed to 3 cells
