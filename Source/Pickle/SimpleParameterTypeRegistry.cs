using System.Collections.Generic;
using System.Linq;
using CucumberExpressions;

namespace RimWorks.Pickle;

public class SimpleParameterTypeRegistry : IParameterTypeRegistry {
  /// <inheritdoc/>
  public IParameterType? LookupByTypeName(string name) {
    return null;
  }

  /// <inheritdoc/>
  public IEnumerable<IParameterType> GetParameterTypes() {
    return [];
  }
}
