# Switching language is the one thing that empties LanguageDatabase.activeLanguage, and a dashboard
# snapshot published in that window used to throw out of the runner and end the run with
# exitReason "infrastructure-error" - blaming whichever scenario happened to be running. The proof
# that it no longer does is structural rather than an assertion: on the broken code this file never
# reaches its second scenario, and the run dies here.
Feature: language steps

  Scenario: switching language and coming back leaves the game with a language
    When the language is "French"
    Then the language is "French"
    When the language is "English"
    Then the language is "English"

  # Reached only if the switch above did not take the run down with it.
  Scenario: the run survived the switch
    Then the language is "English"
    And no errors were logged
