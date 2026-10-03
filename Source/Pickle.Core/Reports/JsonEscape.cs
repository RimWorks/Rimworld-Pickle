using System.Collections.Generic;
using System.Text;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle.Core.Reports;

internal static class JsonEscape {
  public static string Spans(IReadOnlyList<StepArgument> spans) {
    StringBuilder builder = new StringBuilder("[");
    for (int i = 0; i < spans.Count; i++) {
      if (i > 0) {
        builder.Append(',');
      }

      builder.Append("{\"start\":").Append(spans[i].Start).Append(",\"length\":").Append(spans[i].Length).Append('}');
    }

    builder.Append(']');
    return builder.ToString();
  }

  public static string Quote(string value) {
    StringBuilder builder = new StringBuilder(value.Length + 2);
    builder.Append('"');
    foreach (char c in value) {
      switch (c) {
        case '"':
          builder.Append("\\\"");
          break;
        case '\\':
          builder.Append("\\\\");
          break;
        case '\n':
          builder.Append("\\n");
          break;
        case '\r':
          builder.Append("\\r");
          break;
        case '\t':
          builder.Append("\\t");
          break;
        case '\b':
          builder.Append("\\b");
          break;
        case '\f':
          builder.Append("\\f");
          break;
        default:
          if (c < 0x20) {
            builder.Append("\\u").Append(((int)c).ToString("x4"));
          } else {
            builder.Append(c);
          }

          break;
      }
    }

    builder.Append('"');
    return builder.ToString();
  }
}
