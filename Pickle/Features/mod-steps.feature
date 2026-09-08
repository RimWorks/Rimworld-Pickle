Feature: mod steps

  Scenario: a mod is loaded by name or packageId
    Then mod "Core" is loaded
    And mod "ludeon.rimworld" is loaded
    And mod "Pickle" is loaded

  Scenario: a mod that is not installed reports as not loaded
    Then mod "NoSuchModIsInstalled" is not loaded

  Scenario: load order matches how the game loaded mods
    Then mod "Core" loads before "Pickle"
    And mod "Pickle" loads after "Core"
