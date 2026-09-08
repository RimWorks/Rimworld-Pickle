using System;

namespace RimWorks.Pickle;

/// <summary>Marks a class whose static <c>public static void Init()</c> method <see cref="StepScanner"/>
/// calls once, before scanning for step classes. Use it to register fluent steps through <see cref="Pickle"/>.</summary>
[AttributeUsage(AttributeTargets.Class)]
public class PickleEntryAttribute : Attribute {
}
