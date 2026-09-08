using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Weapons and worn apparel, which the carrying steps cannot see.</summary>
[PickleSteps]
public class GearSteps {
  /// <summary>Equips a pawn with a weapon or other equipment, made from its default stuff if it needs one.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to equip.</param>
  /// <param name="defName">The equipment def to make and equip.</param>
  [When("I equip {string} with {string}")]
  public void Equip(PickleContext ctx, string nickname, string defName) {
    EquipWith(ctx, nickname, defName, null);
  }

  /// <summary>Equips a pawn with a weapon or other equipment made from a specific stuff.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to equip.</param>
  /// <param name="defName">The equipment def to make and equip.</param>
  /// <param name="stuffDefName">The stuff to make it from.</param>
  [When("I equip {string} with {string} made of {string}")]
  public void EquipMadeOf(PickleContext ctx, string nickname, string defName, string stuffDefName) {
    EquipWith(ctx, nickname, defName, stuffDefName);
  }

  /// <summary>Dresses a pawn in apparel, made from its default stuff if it needs one.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to dress.</param>
  /// <param name="defName">The apparel def to make and wear.</param>
  [When("I dress {string} in {string}")]
  public void Dress(PickleContext ctx, string nickname, string defName) {
    DressIn(ctx, nickname, defName, null);
  }

  /// <summary>Dresses a pawn in apparel made from a specific stuff.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to dress.</param>
  /// <param name="defName">The apparel def to make and wear.</param>
  /// <param name="stuffDefName">The stuff to make it from.</param>
  [When("I dress {string} in {string} made of {string}")]
  public void DressMadeOf(PickleContext ctx, string nickname, string defName, string stuffDefName) {
    DressIn(ctx, nickname, defName, stuffDefName);
  }

  /// <summary>Drops all of a pawn's equipment and worn apparel onto the map. Requires the pawn to be spawned.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to strip.</param>
  [When("I strip {string}")]
  public void Strip(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(
        pawn.Spawned,
        $"pawn '{nickname}' is not on a map, so it has nowhere to drop gear; " +
        "use 'I destroy the gear of' instead");

    pawn.equipment?.DropAllEquipment(pawn.Position, forbid: false);
    pawn.apparel?.DropAll(pawn.Position, forbid: false);
  }

  /// <summary>Destroys all of a pawn's equipment and worn apparel, unlike stripping it does not need the pawn on a map.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn whose gear is destroyed.</param>
  [When("I destroy the gear of {string}")]
  public void DestroyGear(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    pawn.equipment?.DestroyAllEquipment();
    pawn.apparel?.DestroyAll();
  }

  /// <summary>Asserts a pawn's primary equipment is a specific def.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  /// <param name="defName">The equipment expected to be wielded.</param>
  [Then("{string} is wielding {string}")]
  public void AssertWielding(PickleContext ctx, string nickname, string defName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThingDef def = DefLookup.Require<ThingDef>(defName);

    ctx.Assert(
        pawn.equipment?.Primary?.def == def,
        $"pawn '{nickname}' should be wielding '{defName}'; {DescribeEquipment(pawn)}");
  }

  /// <summary>Asserts a pawn has no primary equipment.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  [Then("{string} is wielding nothing")]
  public void AssertWieldingNothing(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    ctx.Assert(
        pawn.equipment?.Primary == null,
        $"pawn '{nickname}' should be wielding nothing; {DescribeEquipment(pawn)}");
  }

  /// <summary>Asserts a pawn is wearing a specific apparel def.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  /// <param name="defName">The apparel expected to be worn.</param>
  [Then("{string} is wearing {string}")]
  public void AssertWearing(PickleContext ctx, string nickname, string defName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThingDef def = DefLookup.Require<ThingDef>(defName);

    ctx.Assert(
        pawn.apparel?.WornApparel.Any(a => a.def == def) == true,
        $"pawn '{nickname}' should be wearing '{defName}'; {DescribeWorn(pawn)}");
  }

  /// <summary>Asserts a pawn's worn apparel covers a body part group.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  /// <param name="groupDefName">The body part group expected to be covered.</param>
  [Then("{string} apparel covers {string}")]
  public void AssertCovers(PickleContext ctx, string nickname, string groupDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    BodyPartGroupDef group = DefLookup.Require<BodyPartGroupDef>(groupDefName);

    ctx.Assert(
        pawn.apparel?.BodyPartGroupIsCovered(group, null) == true,
        $"pawn '{nickname}' apparel should cover '{groupDefName}'; {DescribeWorn(pawn)}");
  }

  private static void EquipWith(PickleContext ctx, string nickname, string defName, string? stuffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.equipment != null, $"pawn '{nickname}' has no equipment tracker");

    ThingDef def = DefLookup.Require<ThingDef>(defName);
    ctx.Require(
        def.equipmentType != EquipmentType.None,
        $"'{defName}' is not equipment, so no pawn can hold it");

    Thing made = MakeGear(ctx, def, stuffDefName);
    ctx.Require(made is ThingWithComps, $"'{defName}' was not made as equipment");

    ThingWithComps equipment = (ThingWithComps)made;
    pawn.equipment!.MakeRoomFor(equipment);
    pawn.equipment.AddEquipment(equipment);
  }

  private static void DressIn(PickleContext ctx, string nickname, string defName, string? stuffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.apparel != null, $"pawn '{nickname}' has no apparel tracker");

    ThingDef def = DefLookup.Require<ThingDef>(defName);
    ctx.Require(def.IsApparel, $"'{defName}' is not apparel");
    ctx.Require(
        ApparelUtility.HasPartsToWear(pawn, def),
        $"pawn '{nickname}' has no body part to wear '{defName}' on; {DescribeWorn(pawn)}");

    Thing made = MakeGear(ctx, def, stuffDefName);
    ctx.Require(made is Apparel, $"'{defName}' was not made as apparel");

    // Anything conflicting on the same layer is dropped, which is what the game does.
    pawn.apparel!.Wear((Apparel)made, dropReplacedApparel: true, locked: false);
  }

  private static Thing MakeGear(PickleContext ctx, ThingDef def, string? stuffDefName) {
    ThingDef? stuff = null;

    if (stuffDefName != null) {
      stuff = DefLookup.Require<ThingDef>(stuffDefName);
      ctx.Require(
          def.MadeFromStuff,
          $"'{def.defName}' is not made from stuff, so it cannot be made of '{stuffDefName}'");
      ctx.Require(stuff.IsStuff, $"'{stuffDefName}' is not a stuff, so nothing can be made of it");
    } else if (def.MadeFromStuff) {
      stuff = GenStuff.DefaultStuffFor(def);
    }

    return ThingMaker.MakeThing(def, stuff);
  }

  private static string DescribeEquipment(Pawn pawn) {
    List<string> held = [.. pawn.equipment?.AllEquipmentListForReading.Select(e => e.def.defName) ?? []];
    return held.Count == 0 ? "holding no equipment" : $"holding {string.Join(", ", held)}";
  }

  private static string DescribeWorn(Pawn pawn) {
    List<string> worn = [.. pawn.apparel?.WornApparel.Select(a => a.def.defName) ?? []];
    return worn.Count == 0 ? "wearing nothing" : $"wearing {string.Join(", ", worn)}";
  }
}
