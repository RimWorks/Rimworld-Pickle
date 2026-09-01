using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RimWorks.Pickle.Web;

// Minimal JSON writing. RimWorld ships no serializer and the payload shapes here
// are fixed, so hand-writing beats taking a dependency.
public static class Json {
  public static string Quote(string? value) {
    if (value == null) {
      return "null";
    }

    StringBuilder quoted = new StringBuilder(value.Length + 2);
    quoted.Append('"');
    foreach (char c in value) {
      switch (c) {
        case '"':
          quoted.Append("\\\"");
          break;
        case '\\':
          quoted.Append("\\\\");
          break;
        case '\n':
          quoted.Append("\\n");
          break;
        case '\r':
          quoted.Append("\\r");
          break;
        case '\t':
          quoted.Append("\\t");
          break;
        default:
          if (c < 0x20) { quoted.Append("\\u").Append(((int)c).ToString("x4")); } else { quoted.Append(c); }
          break;
      }
    }
    quoted.Append('"');
    return quoted.ToString();
  }

  public static string Number(double value) {
    return value.ToString("0.##", CultureInfo.InvariantCulture);
  }

  public static string Array(IEnumerable<string> items) {
    return "[" + string.Join(",", items) + "]";
  }
}
