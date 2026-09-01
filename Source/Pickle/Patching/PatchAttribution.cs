using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Verse;

namespace RimWorks.Pickle.Patching;

/// <summary>Records which mod patched which def, because RimWorld tracks neither.</summary>
public static class PatchAttribution {
  private static readonly List<Entry> Entries = [];

  private static readonly Dictionary<string, HashSet<string>> ModsByDefName =
      new(StringComparer.OrdinalIgnoreCase);

  private static readonly FieldInfo? NeverSucceededField =
      typeof(PatchOperation).GetField("neverSucceeded", BindingFlags.NonPublic | BindingFlags.Instance);

  /// <summary>True once the early hooks are in place; without them every def reads unpatched.</summary>
  public static bool Armed { get; private set; }

  public static int PatchedDefCount => ModsByDefName.Count;

  public static void Arm() {
    Armed = true;
  }

  public static IReadOnlyCollection<string> PatchersOf(string defName) {
    return ModsByDefName.TryGetValue(defName, out HashSet<string>? mods) ? mods : [];
  }

  // Runs before any patch applies, so the document still holds the nodes every xpath was
  // written against. One pass over the tree beats a hook on thousands of Apply calls.
  public static void BeforeApplyPatches(XmlDocument xml) {
    foreach (ModContentPack mod in LoadedModManager.RunningMods) {
      foreach (PatchOperation root in mod.Patches) {
        Record(mod.Name, root, xml, []);
      }
    }
  }

  // RimWorld sets neverSucceeded to false only when an operation matched something, so this
  // drops the ones that targeted a def on paper and changed nothing.
  public static void BeforeClearCachedPatches() {
    foreach (Entry entry in Entries) {
      if (NeverSucceededField?.GetValue(entry.Operation) is true) {
        continue;
      }

      foreach (string defName in entry.DefNames) {
        if (!ModsByDefName.TryGetValue(defName, out HashSet<string>? mods)) {
          mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
          ModsByDefName[defName] = mods;
        }

        mods.Add(entry.ModName);
      }
    }

    Entries.Clear();
  }

  private static void Record(
      string modName, PatchOperation operation, XmlDocument xml, HashSet<PatchOperation> seen) {
    if (!seen.Add(operation)) {
      return;
    }

    List<string> defNames = TargetsOf(operation, xml);
    if (defNames.Count > 0) {
      Entries.Add(new Entry(operation, modName, defNames));
    }

    foreach (PatchOperation child in ChildrenOf(operation)) {
      Record(modName, child, xml, seen);
    }
  }

  private static List<string> TargetsOf(PatchOperation operation, XmlDocument xml) {
    List<string> defNames = [];
    string? xpath = ReadXpath(operation);
    if (string.IsNullOrEmpty(xpath)) {
      return defNames;
    }

    XmlNodeList? nodes;
    try {
      nodes = xml.SelectNodes(xpath);
    } catch (XPathException) {
      // a malformed xpath is the mod's bug, and RimWorld already reports it
      return defNames;
    }

    if (nodes == null) {
      return defNames;
    }

    foreach (XmlNode node in nodes) {
      string? defName = FindDefName(node, xml);
      if (defName != null && !defNames.Contains(defName)) {
        defNames.Add(defName);
      }
    }

    return defNames;
  }

  // Composite operations hold their children in fields. Reflection covers the vanilla ones
  // and any modded operation that stores children the same way.
  private static IEnumerable<PatchOperation> ChildrenOf(PatchOperation operation) {
    FieldInfo[] fields = operation.GetType()
        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    foreach (FieldInfo field in fields) {
      object? value = field.GetValue(operation);

      if (value is PatchOperation child) {
        yield return child;
        continue;
      }

      if (value is IEnumerable<PatchOperation> children) {
        foreach (PatchOperation each in children) {
          yield return each;
        }
      }
    }
  }

  private static string? ReadXpath(PatchOperation operation) {
    for (Type? type = operation.GetType(); type != null; type = type.BaseType) {
      FieldInfo? field = type.GetField(
          "xpath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

      if (field != null) {
        return field.GetValue(operation) as string;
      }
    }

    return null;
  }

  // Every def sits directly under the unified document's root, so walking up until the
  // parent is that root lands on the def the node belongs to.
  private static string? FindDefName(XmlNode node, XmlDocument xml) {
    XmlNode? current = node;
    while (current != null && current.ParentNode != xml.DocumentElement) {
      current = current.ParentNode;
    }

    return current?["defName"]?.InnerText;
  }

  private sealed class Entry(PatchOperation operation, string modName, List<string> defNames) {
    public PatchOperation Operation { get; } = operation;

    public string ModName { get; } = modName;

    public List<string> DefNames { get; } = defNames;
  }
}
