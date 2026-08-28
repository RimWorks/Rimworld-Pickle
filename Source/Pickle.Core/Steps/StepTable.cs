using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CucumberExpressions;

namespace Pickle.Core.Steps;

public class StepTable {
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  private readonly List<(StepDefinition Definition, Regex Pattern)> definitions = new();
  private readonly PickleParameterTypeRegistry registry;

  public StepTable() {
    registry = new PickleParameterTypeRegistry();
  }

  public void Add(StepDefinition definition) {
    Regex pattern = CompilePattern(definition.Pattern);
    definitions.Add((definition, pattern));
  }

  public StepResolution Resolve(string stepText) {
    List<(StepDefinition Definition, Match Match)> matches = new();

    foreach ((StepDefinition definition, Regex pattern) in definitions) {
      Match match = pattern.Match(stepText);
      if (match.Success) {
        matches.Add((definition, match));
      }
    }

    if (matches.Count == 0) {
      string skeleton = StepSkeletonGenerator.Generate(stepText);
      return new UndefinedStep(skeleton);
    }

    if (matches.Count > 1) {
      List<StepDefinition> matchedDefs = [.. matches.Select(m => m.Definition)];
      return new AmbiguousStep(matchedDefs);
    }

    (StepDefinition matchedDef, Match matchedMatch) = matches[0];
    List<object?> args = ExtractArgs(matchedDef, matchedMatch);
    return new MatchedStep(matchedDef, args);
  }

  private static object? ConvertValue(string value, Type targetType) {
    if (targetType == typeof(int)) {
      return int.Parse(value);
    } else if (targetType == typeof(float)) {
      return float.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
    } else if (targetType == typeof(bool)) {
      return bool.Parse(value);
    } else if (targetType == typeof(string)) {
      if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''))) {
        return value.Substring(1, value.Length - 2);
      }
      return value;
    }

    return value;
  }

  private static List<object?> ExtractArgs(StepDefinition definition, Match match) {
    List<object?> args = new();

    for (int i = 1; i < match.Groups.Count && i <= definition.ParameterTypes.Count; i++) {
      string capturedValue = match.Groups[i].Value;
      Type paramType = definition.ParameterTypes[i - 1];
      object? convertedValue = ConvertValue(capturedValue, paramType);
      args.Add(convertedValue);
    }

    return args;
  }

  private Regex CompilePattern(string pattern) {
    if (pattern.StartsWith("^")) {
      return new Regex(pattern, RegexOptions.Compiled, RegexTimeout);
    }

    try {
      CucumberExpression expression = new CucumberExpression(pattern, registry);
      Regex cucumberRegex = expression.Regex;
      return new Regex(cucumberRegex.ToString(), RegexOptions.Compiled, RegexTimeout);
    } catch (Exception ex) {
      throw new ArgumentException($"Invalid step pattern: {pattern}", ex);
    }
  }
}
