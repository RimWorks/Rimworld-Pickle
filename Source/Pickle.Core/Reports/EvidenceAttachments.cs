using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RimWorks.Pickle.Core.Reports;

public static class EvidenceAttachments {
  public static IEnumerable<(string Name, string Content)> Expand(IEnumerable<(string Name, string Content)> attachments) {
    HashSet<string> films = new HashSet<string>(StringComparer.Ordinal);
    foreach ((string name, string content) in attachments) {
      string? directory = name == "film-frames" ? Path.GetDirectoryName(content) : null;
      if (directory == null || !Directory.Exists(directory)) {
        yield return (name, content);
        continue;
      }

      if (!films.Add(directory)) {
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
