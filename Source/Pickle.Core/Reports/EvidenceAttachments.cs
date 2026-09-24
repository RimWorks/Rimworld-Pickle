using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RimWorks.Pickle.Core.Reports;

/// <summary>Turns a scenario's raw attachment list into what a report actually renders, one film folder at a time.</summary>
public static class EvidenceAttachments {
  /// <summary>Collapses each <c>film-frames</c> attachment into its <c>film-video</c> plus a sorted frame list, deduped
  /// by folder, dropping a film whose folder is gone. Any other attachment passes through unchanged.</summary>
  /// <param name="attachments">A scenario's raw attachments, in the order they were recorded.</param>
  /// <returns>The attachments a report should render.</returns>
  public static IEnumerable<(string Name, string Content)> Expand(IEnumerable<(string Name, string Content)> attachments) {
    HashSet<string> films = new HashSet<string>(StringComparer.Ordinal);
    foreach ((string name, string content) in attachments) {
      if (name != "film-frames") {
        yield return (name, content);
        continue;
      }

      // a film whose folder is gone has nothing to point at, so drop it rather than pass the path on.
      string? directory = Path.GetDirectoryName(content);
      if (directory == null || !Directory.Exists(directory) || !films.Add(directory)) {
        continue;
      }

      string video = Path.Combine(directory, "film.webm");
      if (File.Exists(video)) {
        yield return ("film-video", video);
      }

      foreach (string frame in Directory.GetFiles(directory, "*.jpg").OrderBy(path => path, StringComparer.Ordinal)) {
        yield return ("film-frames", frame);
      }
    }
  }
}
