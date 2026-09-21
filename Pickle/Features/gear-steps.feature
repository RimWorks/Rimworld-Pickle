Feature: gear steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a weapon can be equipped and read back
    Given a colonist "Gunner" exists
    When I equip "Gunner" with "Gun_Autopistol"
    Then "Gunner" is wielding "Gun_Autopistol"

  Scenario: apparel can be worn and covers its body part groups
    Given a colonist "Warm" exists
    When I dress "Warm" in "Apparel_Parka" made of "Cloth"
    Then "Warm" is wearing "Apparel_Parka"
    And "Warm" apparel covers "Torso"
    And "Warm" apparel covers "Arms"

  Scenario: stripping leaves the pawn empty handed
    Given a colonist "Dropper" exists
    When I equip "Dropper" with "MeleeWeapon_Knife"
    And I strip "Dropper"
    Then "Dropper" is wielding nothing

  Scenario: gear can be destroyed instead of dropped
    Given a colonist "Vanish" exists
    When I equip "Vanish" with "Gun_Autopistol"
    And I destroy the gear of "Vanish"
    Then "Vanish" is wielding nothing

  Scenario: apparel is drawn from the texture of the body type the pawn was given
    Given a colonist "Tailored" exists
    And "Tailored" gender is female
    And "Tailored" body type is Fat
    When I dress "Tailored" in "Apparel_Pants"
    Then "Tailored" apparel "Apparel_Pants" is drawn from "Things/Pawn/Humanlike/Apparel/Pants/Pants_Fat"
    When "Tailored" body type is Hulk
    Then "Tailored" apparel "Apparel_Pants" is drawn from "Things/Pawn/Humanlike/Apparel/Pants/Pants_Hulk"
