using System.IO;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Model;
using Xunit;

namespace RimWorks.Pickle.Game.Tests;

[Collection(GameCollection.Name)]
[Trait("Category", "Game")]
public class ScenarioTests {
  private readonly GameSession session;

  public ScenarioTests(GameFixture fixture) {
    session = fixture.Session;
  }

  [Theory]
  [MemberData(nameof(ScenarioRows.All), MemberType = typeof(ScenarioRows))]
  public void Scenario(string feature, int index, string name) {
    FeaturePlan plan = ScenarioRows.Read(Path.Combine(ScenarioRows.FeaturesDir(), feature));
    ScenarioPlan scenario = plan.Scenarios[index];
    Assert.Equal(name, scenario.Name);

    string? failure = session.RunScenario(GherkinWriter.Write(plan, scenario));

    Assert.Null(failure);
  }
}
