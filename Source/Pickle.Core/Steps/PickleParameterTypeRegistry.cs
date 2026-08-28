using System;
using System.Collections.Generic;
using CucumberExpressions;

namespace Pickle.Core.Steps;

public class PickleParameterTypeRegistry : IParameterTypeRegistry {
  private readonly Dictionary<string, IParameterType> parameterTypes = new();

  public PickleParameterTypeRegistry() {
    RegisterStandardTypes();
  }

  /// <inheritdoc/>
  public IParameterType? LookupByTypeName(string name) {
    return parameterTypes.TryGetValue(name, out IParameterType? paramType) ? paramType : null;
  }

  /// <inheritdoc/>
  public IEnumerable<IParameterType> GetParameterTypes() {
    return parameterTypes.Values;
  }

  private void RegisterStandardTypes() {
    parameterTypes["int"] = new IntParameterType();
    parameterTypes["float"] = new FloatParameterType();
    parameterTypes["word"] = new WordParameterType();
    parameterTypes["string"] = new StringParameterType();
    parameterTypes[string.Empty] = new AnonymousParameterType();
  }

  private class IntParameterType : IParameterType {
    public string Name => "int";

    public string[] RegexStrings => ["-?\\d+"];

    public Type ParameterType => typeof(int);

    public int Weight => 0;

    public bool UseForSnippets => true;

    public bool PreferForRegularExpressionMatch => false;

    public object? Transform(string[] args) {
      return args.Length > 0 && int.TryParse(args[0], out int result) ? (object)result : null;
    }
  }

  private class FloatParameterType : IParameterType {
    public string Name => "float";

    public string[] RegexStrings => ["-?\\d*\\.?\\d+"];

    public Type ParameterType => typeof(float);

    public int Weight => 0;

    public bool UseForSnippets => true;

    public bool PreferForRegularExpressionMatch => false;

    public object? Transform(string[] args) {
      return args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result) ? (object)result : null;
    }
  }

  private class WordParameterType : IParameterType {
    public string Name => "word";

    public string[] RegexStrings => ["\\S+"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => true;

    public bool PreferForRegularExpressionMatch => false;

    public object? Transform(string[] args) {
      return args.Length > 0 ? args[0] : null;
    }
  }

  private class StringParameterType : IParameterType {
    public string Name => "string";

    public string[] RegexStrings => ["\"([^\"]*)\"|'([^']*)'"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => true;

    public bool PreferForRegularExpressionMatch => false;

    public object? Transform(string[] args) {
      if (args.Length == 0) {
        return null;
      }

      string value = args[0];
      if (value.Length >= 2) {
        if ((value[0] == '"' && value[value.Length - 1] == '"') ||
            (value[0] == '\'' && value[value.Length - 1] == '\'')) {
          return value.Substring(1, value.Length - 2);
        }
      }
      return value;
    }
  }

  private class AnonymousParameterType : IParameterType {
    public string Name => string.Empty;

    public string[] RegexStrings => [".*"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => false;

    public bool PreferForRegularExpressionMatch => false;

    public object? Transform(string[] args) {
      return args.Length > 0 ? args[0] : null;
    }
  }
}
