using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RimWorks.Pickle.Core.Steps;

public static class StepSkeletonGenerator {
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  public static string Generate(string stepText, StepKind kind = StepKind.When) {
    string expression = GenerateExpression(stepText);
    string methodName = GenerateMethodName(stepText);
    List<string> parameters = ExtractParameters(expression);

    string keywordAttr = kind switch {
      StepKind.Given => "Given",
      StepKind.When => "When",
      StepKind.Then => "Then",
      _ => "When"
    };

    StringBuilder sb = new StringBuilder();
    sb.AppendLine($"[{keywordAttr}(\"{EscapeForAttribute(expression)}\")]");
    sb.Append("public void ");
    sb.Append(methodName);
    sb.Append("(PickleContext ctx");

    foreach (string param in parameters) {
      string[] parts = param.Split('|');
      string paramType = parts[0];
      string paramName = parts.Length > 1 ? parts[1] : "arg";
      sb.Append($", {paramType} {paramName}");
    }

    sb.AppendLine(")");
    sb.AppendLine("{");
    sb.AppendLine("}");

    return sb.ToString();
  }

  private static string GenerateExpression(string stepText) {
    string result = stepText;

    result = Regex.Replace(result, "\"[^\"]*\"", "{string}", RegexOptions.None, RegexTimeout);
    result = Regex.Replace(result, "'[^']*'", "{string}", RegexOptions.None, RegexTimeout);

    result = Regex.Replace(result, @"\b-?\d+\.\d+\b", "{float}", RegexOptions.None, RegexTimeout);

    result = Regex.Replace(result, @"\b-?\d+\b", "{int}", RegexOptions.None, RegexTimeout);

    return result;
  }

  private static string GenerateMethodName(string stepText) {
    string withoutQuotes = Regex.Replace(stepText, "[\"'].*?[\"']", string.Empty, RegexOptions.None, RegexTimeout);
    withoutQuotes = Regex.Replace(withoutQuotes, @"-?\d+\.?\d*", string.Empty, RegexOptions.None, RegexTimeout);

    string[] words = withoutQuotes.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

    if (words.Length == 0) {
      return "Step";
    }

    StringBuilder sb = new StringBuilder();
    foreach (string word in words) {
      string sanitized = Regex.Replace(word, "[^a-zA-Z0-9]", string.Empty, RegexOptions.None, RegexTimeout);
      if (sanitized.Length > 0) {
        sb.Append(char.ToUpperInvariant(sanitized[0]));
        if (sanitized.Length > 1) {
          sb.Append(sanitized, 1, sanitized.Length - 1);
        }
      }
    }

    string methodName = sb.ToString();
    if (methodName.Length == 0) {
      return "Step";
    }

    if (!char.IsLetter(methodName[0])) {
      methodName = "Step" + methodName;
    }

    return methodName;
  }

  private static List<string> ExtractParameters(string expression) {
    List<string> parameters = new List<string>();
    int argIndex = 1;

    MatchCollection matches = Regex.Matches(expression, @"\{(int|float|string)\}", RegexOptions.None, RegexTimeout);
    foreach (Match match in matches) {
      string paramType = match.Groups[1].Value;
      parameters.Add($"{paramType}|arg{argIndex}");
      argIndex++;
    }

    return parameters;
  }

  private static string EscapeForAttribute(string text) {
    return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
  }
}
