Feature: surgery steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a cut takes the part off
    Given a colonist "Stump" exists
    When I amputate "left arm" from "Stump"
    Then "Stump" is missing "left arm"
    And "Stump" is not missing "right arm"

  Scenario: a hediff lands on the part it was given
    Given a colonist "Marked" exists
    When "Marked" is given hediff "SimpleProstheticLeg" on "left leg"
    Then "Marked" has hediff "SimpleProstheticLeg" on "left leg"
    And "Marked" has no hediff "SimpleProstheticLeg" on "right leg"

  Scenario: a prosthetic installs without a doctor
    Given a colonist "Bionic" exists
    When I install "InstallSimpleProstheticArm" on "left shoulder" of "Bionic"
    Then "Bionic" has hediff "SimpleProstheticArm" on "left shoulder"
    And "Bionic" is not missing "left shoulder"

  @slow
  Scenario: a doctor works through a queued surgery
    Given a colonist "Patient" exists
    And a colonist "Doc" exists
    And "Doc" has childhood "ShopKid36"
    And "Doc" has backstory "SpaceNavyDoctor72"
    Then "Doc" can do "Doctor"
    Given "Doc" skill "Medicine" is set to level 20
    # (134, 164) is inside a room. Outdoors the bed reads 0.561 and the surgery fails.
    And a "HospitalBed" is built at (134, 164)
    And 3 "MedicineUltratech" is spawned at the stockpile
    And 3 "MedicineIndustrial" is spawned at the stockpile
    And 1 "SimpleProstheticArm" is spawned at the stockpile
    When I set "Doc" priority "Doctor" to 1
    And I queue surgery "InstallSimpleProstheticArm" on "left shoulder" of "Patient"
    Then "Patient" has 1 surgeries queued
    Given game speed is ultrafast
    When I wait for surgery "InstallSimpleProstheticArm" on "Patient" to finish
    Then "Patient" has hediff "SimpleProstheticArm" on "left shoulder"
