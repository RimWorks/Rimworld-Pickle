Feature: patch attribution

  Scenario: a mod is named as the patcher of a def it changed
    Then def "Pickle_AttributionProbe" was patched by mod "Pickle"
    And def "Pickle_AttributionProbe" was patched

  Scenario: the patch really applied, not just targeted the def
    Then def "Pickle_AttributionProbe" field "label" is "pickle attribution probe (patched)"

  Scenario: a def nothing patches names no patcher
    Then no def "Wall" was patched
