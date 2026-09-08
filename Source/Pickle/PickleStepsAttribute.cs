using System;

namespace RimWorks.Pickle;

/// <summary>Marks a class as holding step definitions. <see cref="StepScanner"/> scans every loaded
/// assembly for classes carrying this attribute, so any mod can ship its own.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class PickleStepsAttribute : Attribute {
}
