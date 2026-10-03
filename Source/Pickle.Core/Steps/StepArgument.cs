namespace RimWorks.Pickle.Core.Steps;

/// <summary>Where one captured argument sits inside a step's text, so a reader can show the value apart from the
/// prose around it.</summary>
public class StepArgument {
  /// <summary>The argument's first character in the step text, counting from zero.</summary>
  public int Start { get; set; }

  /// <summary>How many characters of step text the argument spans.</summary>
  public int Length { get; set; }
}
