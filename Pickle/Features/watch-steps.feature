@film
Feature: watch mode

  Background:
    Given the save "test-colony" is loaded

  @watch @timeout:60
  Scenario: watch mode passes real time instead of driving ticks
    Given a colonist "Walker" exists
    When I select "Walker"
    And I wait 300 ticks
    Then the engine is alive
