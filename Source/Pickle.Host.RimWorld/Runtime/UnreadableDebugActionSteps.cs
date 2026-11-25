using System;
using System.Reflection;
using LudeonTK;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>A debug action whose attribute throws when read, so a scenario can check the debug action scan skips it.</summary>
[PickleSteps]
public static class UnreadableDebugActionSteps {
  private static bool armed;
  private static bool readWhileArmed;

  /// <summary>Makes the fixture's attribute throw when read, and makes the next debug action step scan again.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  [Given("a debug action whose attribute cannot be read")]
  public static void Arm(PickleContext ctx) {
    FieldInfo? scanned = ScannedActions();
    ctx.Require(scanned != null, "DevModeSteps has no cachedActions field, so nothing can make it scan again");

    scanned!.SetValue(null, null);
    readWhileArmed = false;
    armed = true;
  }

  /// <summary>Checks a scan read the fixture's attribute while it was throwing.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  [Then("the unreadable debug action was scanned")]
  public static void WasScanned(PickleContext ctx) {
    ctx.Assert(readWhileArmed, "no scan read the fixture's attribute, so the scenario proved nothing");
  }

  /// <summary>Lets the fixture's attribute be read again, and makes the next debug action step scan again.</summary>
  [AfterScenario]
  public static void Disarm() {
    if (!armed) {
      return;
    }

    armed = false;
    ScannedActions()?.SetValue(null, null);
  }

  private static FieldInfo? ScannedActions() {
    return GenTypes.GetTypeInAnyAssembly("RimWorks.Pickle.Vanilla.DevModeSteps")
        ?.GetField("cachedActions", BindingFlags.Static | BindingFlags.NonPublic);
  }

  [Unreadable]
  private static void UnreadableDebugAction() {
    Log.InfoTo(PickleLog.Channel, "this debug action is a fixture for Pickle's own scenarios");
  }

  [AttributeUsage(AttributeTargets.Method)]
  internal sealed class UnreadableAttribute : DebugActionAttribute {
    public UnreadableAttribute()
        : base("Pickle", "unreadable attribute fixture") {
      ThrowWhileArmed();
    }

    private static void ThrowWhileArmed() {
      if (!armed) {
        return;
      }

      readWhileArmed = true;
      throw new TypeLoadException("the unreadable debug action fixture is armed");
    }
  }
}
