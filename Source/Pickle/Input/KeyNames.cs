using System;

namespace RimWorks.Pickle.Input;

// Each backend maps these to its own codes, so the list of what a step may write lives here
// rather than in whichever backend the platform happened to pick.
internal static class KeyNames {
  public static ArgumentException Unknown(string keyName) {
    return new ArgumentException(
        $"Unknown key: {keyName}; supported keys: Escape, Return, Enter, Space, Tab, Delete, Backspace, A-Z, 0-9");
  }
}
