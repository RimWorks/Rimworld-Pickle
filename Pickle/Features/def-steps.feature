Feature: def steps

  Scenario: a def is found without loading a save
    Then def "Wall" exists
    And def "Wall" of type "ThingDef" exists
    And no def "Pickle_NotARealDef" exists

  Scenario: a def reports the mod that defined it
    Then def "Wall" is defined by mod "Core"
    And def "Wall" is defined by mod "ludeon.rimworld"

  Scenario: fields read through a dotted path
    Then def "Wall" field "graphicData.texPath" is "Things/Building/Linked/Wall"
    And def "Steel" field "useHitPoints" is "false"

  Scenario: stats read both computed and raw
    Then def "Wall" stat "MaxHitPoints" is 300
    And def "Wall" raw stat "MaxHitPoints" is 300
    And def "Steel" stat "MarketValue" is 1.9

  Scenario: a cost list is counted by def
    Then def "TrapIED_HighExplosive" costs 2 "Shell_HighExplosive"
