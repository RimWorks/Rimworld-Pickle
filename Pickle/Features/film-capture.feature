Feature: film capture

  Scenario: a film that ends with a frame still being read back logs no warning
    When a film ends with a frame still being read back
    Then no warning matching "frame readback failed" was logged
