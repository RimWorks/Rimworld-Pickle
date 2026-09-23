@film
Feature: world camera steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: the world camera jumps to a tile and reports it
    When I open the world view
    And I move the world camera to tile 0
    Then the world camera is looking at tile 0
    When I close the world view

  Scenario: zoom survives the frames after it is set
    When I open the world view
    And I zoom the world camera all the way in
    Then the world camera is zoomed all the way in
    When I zoom the world camera all the way out
    Then the world camera is zoomed all the way out
    When I close the world view

  Scenario: north up leaves the camera on the same tile
    When I open the world view
    And I move the world camera to tile 0
    And I turn the world camera north up
    Then the world camera is looking at tile 0
    When I close the world view
