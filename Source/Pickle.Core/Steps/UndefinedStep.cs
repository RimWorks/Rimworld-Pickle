namespace RimWorks.Pickle.Core.Steps;

public class UndefinedStep : StepResolution {
  public UndefinedStep(string skeleton) {
    Skeleton = skeleton;
  }

  public string Skeleton { get; }
}
