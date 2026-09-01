using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class JUnitReportWriterTests {
  [Fact]
  public void Write_ProducesOneTestSuitePerFeature() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    List<XElement> suites = [.. document.Root!.Elements("testsuite")];
    Assert.Equal(2, suites.Count);
    Assert.Contains(suites, s => s.Attribute("name")!.Value == "Login");
    Assert.Contains(suites, s => s.Attribute("name")!.Value == "Checkout");
  }

  [Fact]
  public void Write_TestSuiteCountsMatchScenarioOutcomes() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    XElement login = document.Root!.Elements("testsuite").Single(s => s.Attribute("name")!.Value == "Login");
    Assert.Equal("2", login.Attribute("tests")!.Value);
    Assert.Equal("1", login.Attribute("failures")!.Value);
    Assert.Equal("0", login.Attribute("skipped")!.Value);

    XElement checkout = document.Root!.Elements("testsuite").Single(s => s.Attribute("name")!.Value == "Checkout");
    Assert.Equal("2", checkout.Attribute("tests")!.Value);
    Assert.Equal("0", checkout.Attribute("failures")!.Value);
    Assert.Equal("1", checkout.Attribute("skipped")!.Value);
  }

  [Fact]
  public void Write_FailedScenario_HasFailureNodeWithMessage() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    XElement testCase = document.Descendants("testcase").Single(tc => tc.Attribute("name")!.Value == "failed login");
    XElement failure = testCase.Element("failure")!;

    Assert.Equal(ReportWriterTestData.JUnitFailureMessage, failure.Attribute("message")!.Value);
    Assert.Equal(ReportWriterTestData.JUnitFailureMessage, failure.Value);
  }

  [Fact]
  public void Write_FailureMessageWithXmlSpecialCharacters_RoundTripsThroughXDocument() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());

    Assert.Contains("&lt;", xml);
    Assert.Contains("&amp;&amp;", xml);
    Assert.Contains("&gt;", xml);

    XDocument document = XDocument.Parse(xml);
    XElement failure = document.Descendants("failure").Single();
    Assert.Equal(ReportWriterTestData.JUnitFailureMessage, failure.Value);
    Assert.Equal(ReportWriterTestData.JUnitFailureMessage, failure.Attribute("message")!.Value);
  }

  [Fact]
  public void Write_SkippedScenario_HasSkippedNode() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    XElement testCase = document.Descendants("testcase").Single(tc => tc.Attribute("name")!.Value == "skipped scenario");
    Assert.NotNull(testCase.Element("skipped"));
    Assert.Null(testCase.Element("failure"));
  }

  [Fact]
  public void Write_PassedScenario_HasNoFailureOrSkippedNode() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    XElement testCase = document.Descendants("testcase").Single(tc => tc.Attribute("name")!.Value == "guest checkout");
    Assert.Null(testCase.Element("failure"));
    Assert.Null(testCase.Element("skipped"));
    Assert.Null(testCase.Element("system-out"));
  }

  [Fact]
  public void Write_ScenarioWithEvidence_PutsAttachmentsDumpsAndLogTailInSystemOut() {
    string xml = JUnitReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    XDocument document = XDocument.Parse(xml);

    XElement testCase = document.Descendants("testcase").Single(tc => tc.Attribute("name")!.Value == "failed login");
    string systemOut = testCase.Element("system-out")!.Value;

    Assert.Contains("screenshot", systemOut);
    Assert.Contains("/tmp/login-fail.png", systemOut);
    Assert.Contains("PlayerState", systemOut);
    Assert.Contains("hp=10", systemOut);
    Assert.Contains("log line 1", systemOut);
    Assert.Contains("log line 2", systemOut);
  }

  [Fact]
  public void Write_EmptyResultList_ProducesValidEmptyDocument() {
    string xml = JUnitReportWriter.Write(new List<ScenarioResult>());
    XDocument document = XDocument.Parse(xml);

    Assert.Empty(document.Root!.Elements("testsuite"));
  }
}
