namespace RimWorks.Pickle.Core.Steps;

/// <summary>A step whose text matched no registered <see cref="StepDefinition"/>.</summary>
public class UndefinedStep : StepResolution {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="skeleton">A generated method stub for the missing step, from <see cref="StepSkeletonGenerator"/>.</param>
  public UndefinedStep(string skeleton) {
    Skeleton = skeleton;
  }

  /// <summary>A generated method stub for the missing step, ready to paste into a steps class.</summary>
  public string Skeleton { get; }
}
