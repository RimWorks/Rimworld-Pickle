using System;
using RimWorks.Pickle.Core.Ui;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class DialogAnswersTests {
  [Fact]
  public void TryGet_FindsAnAnswerByExactTypeName() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    Assert.True(answers.TryGet("Dialog_NewFactionSpawning", out string button));
    Assert.Equal("Ignore", button);
  }

  [Fact]
  public void TryGet_DoesNotMatchADifferentCase() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    Assert.False(answers.TryGet("dialog_newfaction", out _));
  }

  [Fact]
  public void TryGet_DoesNotMatchAnUnregisteredWindow() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    Assert.False(answers.TryGet("Dialog_MessageBox", out _));
  }

  [Fact]
  public void TryGet_IsFalseForNull() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    Assert.False(answers.TryGet(null, out _));
  }

  [Fact]
  public void Add_TrimsBothArguments() {
    DialogAnswers answers = new();
    answers.Add("  Dialog_NewFactionSpawning  ", "  Ignore  ");

    Assert.True(answers.TryGet("Dialog_NewFactionSpawning", out string button));
    Assert.Equal("Ignore", button);
  }

  [Fact]
  public void Add_ReplacesAnEarlierAnswerForTheSameWindow() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Skip");
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    Assert.True(answers.TryGet("Dialog_NewFactionSpawning", out string button));
    Assert.Equal("Ignore", button);
  }

  [Theory]
  [InlineData(null, "Ignore")]
  [InlineData("", "Ignore")]
  [InlineData("   ", "Ignore")]
  [InlineData("Dialog_NewFactionSpawning", null)]
  [InlineData("Dialog_NewFactionSpawning", "")]
  [InlineData("Dialog_NewFactionSpawning", "   ")]
  public void Add_RejectsABlankArgument(string? windowType, string? button) {
    DialogAnswers answers = new();

    Assert.Throws<ArgumentException>(() => answers.Add(windowType, button));
  }

  [Fact]
  public void Any_IsFalseUntilAnAnswerIsAdded() {
    DialogAnswers answers = new();

    Assert.False(answers.Any);
    answers.Add("Dialog_NewFactionSpawning", "Ignore");
    Assert.True(answers.Any);
  }

  [Fact]
  public void Clear_LeavesNoAnswerForTheNextScenario() {
    DialogAnswers answers = new();
    answers.Add("Dialog_NewFactionSpawning", "Ignore");

    answers.Clear();

    Assert.False(answers.Any);
    Assert.False(answers.TryGet("Dialog_NewFactionSpawning", out _));
  }
}
