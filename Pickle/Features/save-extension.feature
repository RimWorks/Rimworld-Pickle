Feature: save file extension

  Scenario: fixtures and save files follow the extension a host gives
    Given the save "test-colony" is loaded
    And the save file extension is ".acme"
    When I save fixture "pickle-save-extension" to the saved games folder
    Then fixture "pickle-save-extension" in the saved games folder is a ".acme" file
    And the saved games folder lists fixture "pickle-save-extension" as a ".acme" file
    And save file "pickle-save-extension" resolves to a ".acme" file in the saved games folder
    When the save file "pickle-save-extension" is loaded
    Then the engine is alive
    And no errors were logged

  Scenario: the next scenario is back on .rws
    Then save file "pickle-save-extension" resolves to a ".rws" file in the saved games folder
