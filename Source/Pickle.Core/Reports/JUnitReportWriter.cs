using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

public static class JUnitReportWriter {
  public static string Write(IReadOnlyList<ScenarioResult> results) {
    XElement root = new XElement("testsuites");

    foreach (IGrouping<string, ScenarioResult> feature in results.GroupBy(r => r.FeatureName)) {
      root.Add(BuildTestSuite(feature.Key, [.. feature]));
    }

    XDocument document = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    StringBuilder buffer = new StringBuilder();
    using (Utf8StringWriter writer = new Utf8StringWriter(buffer)) {
      document.Save(writer);
    }

    return buffer.ToString();
  }

  private static XElement BuildTestSuite(string featureName, List<ScenarioResult> scenarios) {
    int failures = scenarios.Count(s => s.Outcome == ScenarioOutcome.Failed);
    int skipped = scenarios.Count(s => s.Outcome == ScenarioOutcome.Skipped);
    double totalSeconds = scenarios.Sum(s => s.DurationMs) / 1000.0;

    XElement suite = new XElement(
        "testsuite",
        new XAttribute("name", featureName),
        new XAttribute("tests", scenarios.Count),
        new XAttribute("failures", failures),
        new XAttribute("skipped", skipped),
        new XAttribute("time", Seconds(totalSeconds)));

    foreach (ScenarioResult scenario in scenarios) {
      suite.Add(BuildTestCase(featureName, scenario));
    }

    return suite;
  }

  private static XElement BuildTestCase(string featureName, ScenarioResult scenario) {
    XElement testCase = new XElement(
        "testcase",
        new XAttribute("name", scenario.ScenarioName),
        new XAttribute("classname", featureName),
        new XAttribute("time", Seconds(scenario.DurationMs / 1000.0)));

    if (scenario.Outcome == ScenarioOutcome.Failed) {
      string message = scenario.FailureMessage ?? "Scenario failed";
      testCase.Add(new XElement("failure", new XAttribute("message", message), message));
    } else if (scenario.Outcome == ScenarioOutcome.Skipped) {
      testCase.Add(new XElement("skipped"));
    }

    string? evidence = BuildEvidence(scenario);
    if (evidence != null) {
      testCase.Add(new XElement("system-out", evidence));
    }

    return testCase;
  }

  private static string? BuildEvidence(ScenarioResult scenario) {
    if (scenario.Attachments.Count == 0 && scenario.StateDumps.Count == 0 && scenario.LogTail.Count == 0) {
      return null;
    }

    StringBuilder builder = new StringBuilder();

    foreach ((string name, string content) in scenario.Attachments) {
      builder.Append("Attachment: ").Append(name).Append(" -> ").Append(content).Append('\n');
    }

    foreach ((string source, string content) in scenario.StateDumps) {
      builder.Append("State dump [").Append(source).Append("]: ").Append(content).Append('\n');
    }

    if (scenario.LogTail.Count > 0) {
      builder.Append("Log tail:\n");
      foreach (string line in scenario.LogTail) {
        builder.Append(line).Append('\n');
      }
    }

    return builder.ToString();
  }

  private static string Seconds(double seconds) {
    return seconds.ToString("0.000", CultureInfo.InvariantCulture);
  }

  private sealed class Utf8StringWriter : StringWriter {
    public Utf8StringWriter(StringBuilder builder)
        : base(builder) {
    }

    public override Encoding Encoding => Encoding.UTF8;
  }
}
