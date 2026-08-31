Feature: bill steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: a bill can be added to a bench and counted
    When I spawn a "TableStonecutter" at (140, 155)
    Then the "TableStonecutter" has 0 bills
    When I add bill "Make_StoneBlocksAny" to the "TableStonecutter"
    Then the "TableStonecutter" has 1 bills

  Scenario: a bill can be added to a bench at a named cell
    When I spawn a "TableStonecutter" at (145, 155)
    And I add bill "Make_StoneBlocksAny" to the "TableStonecutter" at (145, 155)
    Then the "TableStonecutter" has 1 bills

  Scenario: work priorities can be set and read back
    Given a colonist "Worker" exists
    When I set "Worker" priority "Cooking" to 1
    Then "Worker" priority "Cooking" is 1
    When I set "Worker" priority "Cooking" to 4
    Then "Worker" priority "Cooking" is 4

  @slow
  Scenario: a colonist fills a bill and the product appears
    Given a colonist "Cutter" exists
    And "Cutter" has childhood "ShopKid36"
    And "Cutter" has backstory "Blacksmith7"
    Then "Cutter" can do "Crafting"
    Given I spawn a "TableStonecutter" at (150, 155)
    And I spawn a "ChunkSandstone" at (152, 155)
    And "Cutter" skill "Crafting" is set to level 10
    When I set "Cutter" priority "Crafting" to 1
    And I add bill "Make_StoneBlocksSandstone" to the "TableStonecutter" at (150, 155)
    And game speed is ultrafast
    And I wait for bill "Make_StoneBlocksSandstone" to finish
    Then a "BlocksSandstone" exists
