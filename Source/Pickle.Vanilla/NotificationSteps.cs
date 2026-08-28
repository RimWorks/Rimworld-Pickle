using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>
/// Alerts and messages. Both live in private lists, so both need reflection: the
/// readout keeps its own list, and Messages holds one static list of live toasts.
/// </summary>
[PickleSteps]
public class NotificationSteps {
  private static readonly FieldInfo? ActiveAlertsField =
      typeof(AlertsReadout).GetField("activeAlerts", BindingFlags.Instance | BindingFlags.NonPublic);

  private static readonly FieldInfo? LiveMessagesField =
      typeof(Messages).GetField("liveMessages", BindingFlags.Static | BindingFlags.NonPublic);

  [Then("alert {string} is active")]
  public async Task AssertAlertActive(PickleContext ctx, string labelSubstring) {
    await ctx.AssertEventually(
        () => ActiveAlerts().Any(a => Matches(a, labelSubstring)),
        () => $"alert '{labelSubstring}' should be active; active alerts: {DescribeAlerts()}");
  }

  [Then("alert {string} is not active")]
  public void AssertAlertInactive(PickleContext ctx, string labelSubstring) {
    ctx.Assert(
        !ActiveAlerts().Any(a => Matches(a, labelSubstring)),
        $"alert '{labelSubstring}' should not be active; active alerts: {DescribeAlerts()}");
  }

  [Then("a message {string} was shown")]
  public async Task AssertMessageShown(PickleContext ctx, string textSubstring) {
    await ctx.AssertEventually(
        () => LiveMessages().Any(m => m.IndexOf(textSubstring, StringComparison.OrdinalIgnoreCase) >= 0),
        () => $"no message containing '{textSubstring}'; messages: {DescribeMessages()}");
  }

  [When("I dismiss letter {string}")]
  public void DismissLetter(PickleContext ctx, string labelSubstring) {
    Letter? letter = Find.LetterStack.LettersListForReading
        .FirstOrDefault(l => l.Label.RawText?.IndexOf(labelSubstring, StringComparison.OrdinalIgnoreCase) >= 0);

    ctx.Require(letter != null, $"no letter matching '{labelSubstring}' to dismiss");
    Find.LetterStack.RemoveLetter(letter!);
  }

  [Then("no letters are pending")]
  public void AssertNoLetters(PickleContext ctx) {
    List<Letter> letters = Find.LetterStack.LettersListForReading;
    ctx.Assert(
        letters.Count == 0,
        $"expected no letters; still pending: {string.Join(", ", letters.Select(l => l.Label.RawText))}");
  }

  private static bool Matches(Alert alert, string substring) {
    string label;
    try {
      label = alert.Label ?? string.Empty;
    } catch (Exception) {
      // An alert whose label throws is still an active alert, so do not fail the scan.
      return false;
    }

    return label.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;
  }

  private static IEnumerable<Alert> ActiveAlerts() {
    if (ActiveAlertsField?.GetValue(Find.Alerts) is List<Alert> alerts) {
      return alerts;
    }

    return [];
  }

  private static IEnumerable<string> LiveMessages() {
    if (LiveMessagesField?.GetValue(null) is not System.Collections.IEnumerable messages) {
      return [];
    }

    return messages.Cast<object>()
        .Select(m => m.GetType().GetField("text")?.GetValue(m) as string ?? string.Empty);
  }

  private static string DescribeAlerts() {
    string[] labels = [.. ActiveAlerts().Select(a => {
      try {
        return a.Label;
      } catch (Exception) {
        return a.GetType().Name;
      }
    })];

    return labels.Length == 0 ? "(none)" : string.Join(", ", labels);
  }

  private static string DescribeMessages() {
    string[] texts = [.. LiveMessages()];
    return texts.Length == 0 ? "(none)" : string.Join(" | ", texts);
  }
}
