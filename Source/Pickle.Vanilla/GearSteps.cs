using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>Weapons and worn apparel, which the carrying steps cannot see.</summary>
[PickleSteps]
public class GearSteps {
  [When("I equip {string} with {string}")]
  public void Equip(PickleContext ctx, string nickname, string defName) {
    EquipWith(ctx, nickname, defName, null);
  }

  [When("I equip {string} with {string} made of {string}")]
  public void EquipMadeOf(PickleContext ctx, string nickname, string defName, string stuffDefName) {
    EquipWith(ctx, nickname, defName, stuffDefName);
  }

  [When("I dress {string} in {string}")]
  public void Dress(PickleContext ctx, string nickname, string defName) {
    DressIn(ctx, nickname, defName, null);
  }

  [When("I dress {string} in {string} made of {string}")]
  public void DressMadeOf(PickleContext ctx, string nickname, string defName, string stuffDefName) {
    DressIn(ctx, nickname, defName, stuffDefName);
  }

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

  [When("I destroy the gear of {string}")]
  public void DestroyGear(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    pawn.equipment?.DestroyAllEquipment();
    pawn.apparel?.DestroyAll();
  }

  [Then("{string} is wielding {string}")]
  public void AssertWielding(PickleContext ctx, string nickname, string defName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThingDef def = DefLookup.Require<ThingDef>(defName);

    ctx.Assert(
        pawn.equipment?.Primary?.def == def,
        $"pawn '{nickname}' should be wielding '{defName}'; {DescribeEquipment(pawn)}");
  }

  [Then("{string} is wielding nothing")]
  public void AssertWieldingNothing(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    ctx.Assert(
        pawn.equipment?.Primary == null,
        $"pawn '{nickname}' should be wielding nothing; {DescribeEquipment(pawn)}");
  }

  [Then("{string} is wearing {string}")]
  public void AssertWearing(PickleContext ctx, string nickname, string defName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThingDef def = DefLookup.Require<ThingDef>(defName);

    ctx.Assert(
        pawn.apparel?.WornApparel.Any(a => a.def == def) == true,
        $"pawn '{nickname}' should be wearing '{defName}'; {DescribeWorn(pawn)}");
  }

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
