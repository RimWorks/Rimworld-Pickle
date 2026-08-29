@film
Feature: dev mode steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: god mode toggles on and off
    Given god mode is enabled
    Then the engine is alive
    Given god mode is disabled
    Then the engine is alive

  Scenario: a vanilla debug action can be triggered by name
    Given dev mode is enabled
    When I trigger debug action "FinishAllResearch"
    And I wait for research "Smithing" to finish
    Then the engine is alive

  Scenario: a debug action can be scoped to its category
    Given dev mode is enabled
    When I trigger debug action "FinishAllResearch" in category "General"
    Then the engine is alive
