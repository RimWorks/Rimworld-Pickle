using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StepSkeletonGeneratorTests {
  [Fact]
  public void Generate_BasicStep() {
    string skeleton = StepSkeletonGenerator.Generate("I do something");

    Assert.Contains("[When(", skeleton);
    Assert.Contains("public void", skeleton);
    Assert.Contains("PickleContext ctx", skeleton);
    Assert.Contains("IDoSomething", skeleton);
  }

  [Fact]
  public void Generate_ReplaceDoubleQuotedWithString() {
    string skeleton = StepSkeletonGenerator.Generate("I click \"OK\"");

    Assert.Contains("[When(\"I click {string}\")]", skeleton);
    Assert.Contains("string", skeleton);
  }

  [Fact]
  public void Generate_ReplaceSingleQuotedWithString() {
    string skeleton = StepSkeletonGenerator.Generate("I click 'OK'");

    Assert.Contains("{string}", skeleton);
  }

  [Fact]
  public void Generate_ReplaceIntegerWithInt() {
    string skeleton = StepSkeletonGenerator.Generate("I have 42 items");

    Assert.Contains("{int}", skeleton);
    Assert.Contains("int", skeleton);
  }

  [Fact]
  public void Generate_ReplaceDecimalWithFloat() {
    string skeleton = StepSkeletonGenerator.Generate("price is 19.99");

    Assert.Contains("{float}", skeleton);
    Assert.Contains("float", skeleton);
  }

  [Fact]
  public void Generate_MultipleParameters() {
    string skeleton = StepSkeletonGenerator.Generate("I have 5 \"red\" items for 19.99");

    Assert.Contains("{int}", skeleton);
    Assert.Contains("{string}", skeleton);
    Assert.Contains("{float}", skeleton);
    Assert.Contains("int arg1", skeleton);
    Assert.Contains("string arg2", skeleton);
    Assert.Contains("float arg3", skeleton);
  }

  [Fact]
  public void Generate_SkeletonIsCompilable() {
    string skeleton = StepSkeletonGenerator.Generate("user submits form");

    Assert.Contains("[When(", skeleton);
    Assert.Contains("public void", skeleton);
    Assert.Contains("(PickleContext ctx)", skeleton);
    Assert.Contains("{", skeleton);
    Assert.Contains("}", skeleton);
  }

  [Fact]
  public void Generate_UsesCorrectKeyword() {
    string givenSkeleton = StepSkeletonGenerator.Generate("setup is done", StepKind.Given);
    string whenSkeleton = StepSkeletonGenerator.Generate("user clicks", StepKind.When);
    string thenSkeleton = StepSkeletonGenerator.Generate("result is verified", StepKind.Then);

    Assert.Contains("[Given(", givenSkeleton);
    Assert.Contains("[When(", whenSkeleton);
    Assert.Contains("[Then(", thenSkeleton);
  }

  [Fact]
  public void Generate_NegativeNumbers() {
    string skeleton = StepSkeletonGenerator.Generate("temperature is -5");

    Assert.Contains("{int}", skeleton);
  }

  [Fact]
  public void Generate_NegativeDecimals() {
    string skeleton = StepSkeletonGenerator.Generate("balance is -3.14");

    Assert.Contains("{float}", skeleton);
  }

  [Fact]
  public void Generate_MethodNamePascalCase() {
    string skeleton = StepSkeletonGenerator.Generate("I have a nice day");

    Assert.Contains("IHaveANiceDay", skeleton);
  }
}
