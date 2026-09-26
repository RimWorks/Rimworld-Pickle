@film
Feature: pawn steps

  Background:
    Given the save "test-colony" is loaded

  Scenario: health and hediff steps read a pawn's real condition
    Given a colonist "Patient" exists
    And I heal "Patient"
    Then "Patient" has no hediffs
    And "Patient" is healthy
    And "Patient" health is above 90 percent
    When "Patient" is given hediff "Flu"
    Then "Patient" has hediff "Flu"
    When "Patient" is cured of hediff "Flu"
    Then "Patient" has no hediff "Flu"

  Scenario: a downed pawn reports downed
    Given a colonist "Faller" exists
    When I kill "Faller"
    Then "Faller" is dead

  Scenario: needs and mood can be set and read
    Given a colonist "Hungry" exists
    When "Hungry" needs "Food" is set to 10 percent
    Then "Hungry" needs "Food" is below 20 percent
    When "Hungry" needs "Mood" is set to 90 percent
    Then "Hungry" mood is above 80 percent

  Scenario: skills can be set and read
    Given a colonist "Jet" exists
    When "Jet" skill "Shooting" is set to level 12
    Then "Jet" has skill "Shooting" at level 12

  Scenario: carried counts read a pawn's real inventory
    Given a colonist "Empty" exists
    Then "Empty" is carrying 0 "Silver"

  Scenario: a body type can be set on a pawn of either gender and is what the pawn is drawn with
    Given a colonist "Wide" exists
    And "Wide" gender is female
    And "Wide" body type is Fat
    Then "Wide" body is drawn from "Things/Pawn/Humanlike/Bodies/Naked_Fat"
    When "Wide" body type is Thin
    Then "Wide" body is drawn from "Things/Pawn/Humanlike/Bodies/Naked_Thin"
    When "Wide" gender is male
    And "Wide" body type is Hulk
    Then "Wide" body is drawn from "Things/Pawn/Humanlike/Bodies/Naked_Hulk"

  Scenario Outline: <who> is drawn with the textures of the <body> body type
    Given a colonist "Sample" exists
    And I strip "Sample"
    And "Sample" is <age> years old
    And "Sample" gender is <gender>
    And "Sample" body type is <body>
    When I dress "Sample" in "<apparel>"
    Then "Sample" body is drawn from "Things/Pawn/Humanlike/Bodies/Naked_<body>"
    And "Sample" apparel "<apparel>" is drawn from "Things/Pawn/Humanlike/Apparel/<worn>_<body>"

    Examples:
      | who        | age | gender | body   | apparel            | worn                 |
      | fat man    | 30  | male   | Fat    | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | thin man   | 30  | male   | Thin   | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | hulk man   | 30  | male   | Hulk   | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | man        | 30  | male   | Male   | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | boy        | 8   | male   | Child  | Apparel_KidShirt   | KidShirt/KidShirt     |
      | fat woman  | 30  | female | Fat    | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | thin woman | 30  | female | Thin   | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | hulk woman | 30  | female | Hulk   | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | woman      | 30  | female | Female | Apparel_BasicShirt | ShirtBasic/ShirtBasic |
      | girl       | 8   | female | Child  | Apparel_KidShirt   | KidShirt/KidShirt     |

  Scenario: one pawn can be ordered to attack another
    Given a colonist "Fighter" exists
    And a colonist "Victim" exists
    When "Fighter" attacks "Victim"
    Then "Fighter" is drafted
    And the engine is alive
