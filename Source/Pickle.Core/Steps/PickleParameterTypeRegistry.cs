using System;
using System.Collections.Generic;
using CucumberExpressions;

namespace RimWorks.Pickle.Core.Steps;

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

  private sealed class IntParameterType : IParameterType {
    public string Name => "int";

    public string[] RegexStrings => ["-?\\d+"];

    public Type ParameterType => typeof(int);

    public int Weight => 0;

    public bool UseForSnippets => true;
  }

  private sealed class FloatParameterType : IParameterType {
    public string Name => "float";

    public string[] RegexStrings => ["-?\\d*\\.?\\d+"];

    public Type ParameterType => typeof(float);

    public int Weight => 0;

    public bool UseForSnippets => true;
  }

  private sealed class WordParameterType : IParameterType {
    public string Name => "word";

    public string[] RegexStrings => ["\\S+"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => true;
  }

  private sealed class StringParameterType : IParameterType {
    public string Name => "string";

    public string[] RegexStrings => ["\"([^\"]*)\"|'([^']*)'"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => true;
  }

  private sealed class AnonymousParameterType : IParameterType {
    public string Name => string.Empty;

    public string[] RegexStrings => [".*"];

    public Type ParameterType => typeof(string);

    public int Weight => 0;

    public bool UseForSnippets => false;
  }
}
