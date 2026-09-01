using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;
using RimWorks.Pickle.Core.Model;

namespace RimWorks.Pickle.Core;

public static class GherkinAdapter {
  public static FeaturePlan Adapt(GherkinDocument doc, string? sourcePath) {
    if (doc.Feature == null) {
      return new FeaturePlan(string.Empty, new TagSet(Array.Empty<string>()), Array.Empty<ScenarioPlan>(), sourcePath);
    }

    TagSet featureTags = ExtractTags(doc.Feature.Tags);
    List<ScenarioPlan> scenarios = new List<ScenarioPlan>();
    List<StepPlan> backgroundSteps = new List<StepPlan>();

    ProcessFeatureChildren(doc.Feature.Children, featureTags, backgroundSteps, scenarios);

    return new FeaturePlan(doc.Feature.Name, featureTags, scenarios, sourcePath);
  }

  private static void ProcessFeatureChildren(IEnumerable<IHasLocation> children, TagSet featureTags, List<StepPlan> featureBackground, List<ScenarioPlan> scenarios) {
    foreach (IHasLocation child in children) {
      if (child is Background background) {
        featureBackground.AddRange(ExtractSteps(background.Steps));
      } else if (child is Scenario scenario) {
        AddScenario(scenario, featureTags, featureBackground, scenarios);
      } else if (child is Rule rule) {
        // A rule is a feature-shaped scope: it inherits the tags and background, then
        // layers its own on top. Recursing keeps the two paths from drifting apart.
        TagSet ruleTags = featureTags.With(ExtractTags(rule.Tags));
        List<StepPlan> ruleBackground = [.. featureBackground];
        ProcessFeatureChildren(rule.Children, ruleTags, ruleBackground, scenarios);
      }
    }
  }

  private static void AddScenario(Scenario scenario, TagSet inheritedTags, List<StepPlan> background, List<ScenarioPlan> scenarios) {
    if (scenario.Examples.Any()) {
      ExpandOutline(scenario, inheritedTags, background, scenarios);
      return;
    }

    TagSet scenarioTags = inheritedTags.With(ExtractTags(scenario.Tags));
    List<StepPlan> steps = [.. background];
    steps.AddRange(ExtractSteps(scenario.Steps));
    scenarios.Add(new ScenarioPlan(scenario.Name, scenarioTags, steps, scenario.Location.Line));
  }

  private static void ExpandOutline(Scenario outline, TagSet inheritedTags, List<StepPlan> backgroundSteps, List<ScenarioPlan> scenarios) {
    foreach (Examples examples in outline.Examples) {
      TagSet examplesTagSet = ExtractTags(examples.Tags);
      TagSet scenarioTags = inheritedTags.With(ExtractTags(outline.Tags)).With(examplesTagSet);

      List<string> headerNames = [.. examples.TableHeader.Cells.Select(cell => cell.Value)];

      foreach (TableRow bodyRow in examples.TableBody) {
        Dictionary<string, string> substitutions = new Dictionary<string, string>();
        List<string> cellValues = [.. bodyRow.Cells.Select(cell => cell.Value)];
        for (int i = 0; i < headerNames.Count && i < cellValues.Count; i++) {
          substitutions[headerNames[i]] = cellValues[i];
        }

        string expandedName = ExpandText(outline.Name, substitutions);
        List<StepPlan> expandedSteps = [.. backgroundSteps];

        foreach (Step step in outline.Steps) {
          string expandedText = ExpandText(step.Text, substitutions);
          IReadOnlyList<IReadOnlyList<string>> expandedTable = ExpandTable(step.DataTable, substitutions);
          string? expandedDocString = step.DocString != null ? ExpandText(step.DocString.Content, substitutions) : null;

          expandedSteps.Add(new StepPlan(step.Keyword, expandedText, expandedTable, expandedDocString, step.Location.Line));
        }

        scenarios.Add(new ScenarioPlan(expandedName, scenarioTags, expandedSteps, outline.Location.Line));
      }
    }
  }

  private static string ExpandText(string text, Dictionary<string, string> substitutions) {
    string result = text;
    foreach (KeyValuePair<string, string> kv in substitutions) {
      result = result.Replace($"<{kv.Key}>", kv.Value);
    }
    return result;
  }

  private static IReadOnlyList<IReadOnlyList<string>> ExpandTable(DataTable? dataTable, Dictionary<string, string> substitutions) {
    if (dataTable == null) {
      return Array.Empty<IReadOnlyList<string>>();
    }

    List<IReadOnlyList<string>> result = new List<IReadOnlyList<string>>();

    foreach (TableRow row in dataTable.Rows) {
      List<string> expandedRow = [.. row.Cells.Select(cell => ExpandText(cell.Value, substitutions))];
      result.Add(expandedRow);
    }

    return result;
  }

  private static List<StepPlan> ExtractSteps(IEnumerable<Step> steps) {
    List<StepPlan> result = new List<StepPlan>();
    foreach (Step step in steps) {
      IReadOnlyList<IReadOnlyList<string>> table = ExtractTable(step.DataTable);
      string? docString = step.DocString?.Content;
      result.Add(new StepPlan(step.Keyword, step.Text, table, docString, step.Location.Line));
    }
    return result;
  }

  private static IReadOnlyList<IReadOnlyList<string>> ExtractTable(DataTable? dataTable) {
    if (dataTable == null) {
      return Array.Empty<IReadOnlyList<string>>();
    }

    List<IReadOnlyList<string>> result = new List<IReadOnlyList<string>>();
    foreach (TableRow row in dataTable.Rows) {
      List<string> rowValues = [.. row.Cells.Select(cell => cell.Value)];
      result.Add(rowValues);
    }
    return result;
  }

  private static TagSet ExtractTags(IEnumerable<Tag> tags) {
    List<string> tagStrings = [.. tags.Select(t => t.Name)];
    return new TagSet(tagStrings);
  }
}
