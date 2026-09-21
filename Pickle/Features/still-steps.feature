Feature: waiting for a control to stand still

  # The window opens with its button sliding for 20 frames, a stand-in for a layout that is still
  # settling. Pickle resolves a tag once and clicks where it stood, so a click sent during the
  # slide lands on empty space and IMGUI counts nothing.

  Scenario: a click sent while the button slides is lost
    Given a window whose button drifts for 20 frames
    When I click button "Drifting"
    Then the drifting button was not clicked

  Scenario: waiting for the button to stand still first lets the click count
    Given a window whose button drifts for 20 frames
    When I wait until button "Drifting" stands still
    And I click button "Drifting"
    Then the drifting button was clicked

  Scenario: the same wait, by tag
    Given a window whose button drifts for 20 frames
    When I wait until tag "btn:Drifting" stands still
    And I click "btn:Drifting"
    Then the drifting button was clicked
