using System;

namespace RimWorks.Pickle.Autorun;

internal static class CommandLineFlags {
  public static bool Passed(string key) {
    foreach (string arg in Environment.GetCommandLineArgs()) {
      if (Matches(arg, key)) {
        return true;
      }
    }

    return false;
  }

  public static bool TryGet(string key, out string value) {
    foreach (string arg in Environment.GetCommandLineArgs()) {
      string[] parts = arg.Split('=');
      if (parts.Length == 2 && Matches(parts[0], key)) {
        value = parts[1];
        return true;
      }
    }

    value = string.Empty;
    return false;
  }

  private static bool Matches(string arg, string key) {
    return string.Equals(arg, key, StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "-" + key, StringComparison.OrdinalIgnoreCase);
  }
}
