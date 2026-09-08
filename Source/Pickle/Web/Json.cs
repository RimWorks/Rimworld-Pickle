using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RimWorks.Pickle.Web;

/// <summary>Hand-rolled JSON writing for the dashboard payloads.</summary>
// Minimal JSON writing. RimWorld ships no serializer and the payload shapes here
// are fixed, so hand-writing beats taking a dependency.
public static class Json {
  /// <summary>Quotes and escapes a string for JSON output.</summary>
  /// <param name="value">The value to quote, or <c>null</c>.</param>
  /// <returns>A JSON string literal, or the bare token <c>null</c> when <paramref name="value"/> is <c>null</c>.</returns>
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

  /// <summary>Formats a number for JSON output, trimming to at most two decimal places.</summary>
  /// <param name="value">The value to format.</param>
  /// <returns>The value as a JSON number literal.</returns>
  public static string Number(double value) {
    return value.ToString("0.##", CultureInfo.InvariantCulture);
  }

  /// <summary>Joins already-serialized JSON values into an array.</summary>
  /// <param name="items">Each item, already valid JSON.</param>
  /// <returns>A JSON array literal.</returns>
  public static string Array(IEnumerable<string> items) {
    return "[" + string.Join(",", items) + "]";
  }
}
