using System;
using System.Collections.Generic;
using System.IO;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Parses every feature a suite ships and drops the ones that cannot run.</summary>
public static class FeatureParser {
  /// <summary>Parses every feature file across the given suites, logging and skipping the ones that fail.</summary>
  /// <param name="suites">The suites to parse features from.</param>
  /// <returns>Each suite paired with the plan for one of its feature files.</returns>
  public static List<(DiscoveredSuite Suite, FeaturePlan Plan)> ParseAll(List<DiscoveredSuite> suites) {
    List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsed = [];

    foreach (DiscoveredSuite suite in suites) {
      foreach (string featureFile in suite.FeatureFiles) {
        FeaturePlan? plan = ParseOne(featureFile);
        if (plan != null) {
          parsed.Add((suite, plan));
        }
      }
    }

    return parsed;
  }

  /// <summary>Parses Gherkin text that came from no file, such as a one-off run typed into the dashboard.</summary>
  /// <param name="gherkin">The Gherkin source.</param>
  /// <returns>The plan, whose <c>SourcePath</c> is <c>null</c> because there is no file.</returns>
  /// <exception cref="InvalidOperationException">The text is blank, will not parse, or holds a problem that stops it running. The message is shown to whoever typed the Gherkin, so it carries no parameter name.</exception>
  public static FeaturePlan ParseText(string? gherkin) {
    if (string.IsNullOrWhiteSpace(gherkin)) {
      throw new InvalidOperationException("Send some Gherkin to run.");
    }

    FeaturePlan plan;
    try {
      Parser parser = new Parser();
      using StringReader reader = new StringReader(gherkin);
      plan = GherkinAdapter.Adapt(parser.Parse(reader), null);
    } catch (CompositeParserException composite) {
      throw new InvalidOperationException(Explain(composite.Errors), composite);
    } catch (ParserException single) {
      throw new InvalidOperationException(Explain([single]), single);
    } catch (Exception ex) {
      throw new InvalidOperationException(ex.Message, ex);
    }

    IReadOnlyList<string> problems = QuickstartTag.Problems(plan);
    if (problems.Count > 0) {
      throw new InvalidOperationException(string.Join("; ", problems));
    }

    return plan;
  }

  private static string Explain(IEnumerable<ParserException> errors) {
    List<string> lines = [];

    foreach (ParserException error in errors) {
      string where = error.Location is Location at ? $"line {at.Line}, column {at.Column}: " : string.Empty;
      string? hint = error switch {
        UnexpectedTokenException token => GherkinHint.For(token.ExpectedTokenTypes, false),
        UnexpectedEOFException end => GherkinHint.For(end.ExpectedTokenTypes, true),
        NoSuchLanguageException => GherkinHint.UnknownLanguage,
        _ => null,
      };

      lines.Add(where + (hint ?? error.Message));
      if (hint != null) {
        lines.Add("    " + error.Message);
      }
    }

    return string.Join("\n", lines);
  }

  private static FeaturePlan? ParseOne(string featureFile) {
    string fileName = Path.GetFileName(featureFile);

    try {
      Parser parser = new Parser();
      using StringReader reader = new StringReader(File.ReadAllText(featureFile));
      FeaturePlan plan = GherkinAdapter.Adapt(parser.Parse(reader), featureFile);

      IReadOnlyList<string> problems = QuickstartTag.Problems(plan);
      if (problems.Count == 0) {
        return plan;
      }

      // One bad feature must not take the suite down, so this drops the file and keeps going.
      foreach (string problem in problems) {
        Log.ErrorTo(PickleLog.Channel, "{FileName} cannot run: {Problem}", [fileName, problem]);
      }

      return null;
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, $"failed to parse {fileName}");
      return null;
    }
  }
}
