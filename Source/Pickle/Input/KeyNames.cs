using System;
using UnityEngine;

namespace RimWorks.Pickle.Input;

internal static class KeyNames {
  private const string Supported =
      "Escape, Return, Enter, Space, Tab, Delete, Backspace, A-Z, 0-9";

  public static KeyCode Parse(string keyName) {
    string name = keyName.Trim();

    switch (name.ToLowerInvariant()) {
      case "escape":
        return KeyCode.Escape;
      case "return":
      case "enter":
        return KeyCode.Return;
      case "space":
        return KeyCode.Space;
      case "tab":
        return KeyCode.Tab;
      case "delete":
        return KeyCode.Delete;
      case "backspace":
        return KeyCode.Backspace;
      default:
        break;
    }

    if (name.Length == 1 && char.IsLetter(name[0])) {
      return KeyCode.A + (char.ToLowerInvariant(name[0]) - 'a');
    }

    if (name.Length == 1 && char.IsDigit(name[0])) {
      return KeyCode.Alpha0 + (name[0] - '0');
    }

    throw Unknown(keyName);
  }

  public static ArgumentException Unknown(string keyName) {
    return new ArgumentException($"Unknown key: {keyName}; supported keys: {Supported}");
  }
}
