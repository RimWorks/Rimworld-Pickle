using System;

namespace RimWorks.Pickle;

/// <summary>Marks a method to run and attach to the report whenever a scenario in that suite fails.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class PickleStateDumpAttribute : Attribute {
}
