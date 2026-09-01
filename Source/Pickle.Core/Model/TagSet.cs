using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.Pickle.Core.Model;

public class TagSet : IReadOnlyCollection<string> {
  private readonly HashSet<string> tags;

  public TagSet(IEnumerable<string> tagList) {
    tags = [.. tagList];
  }

  public int Count => tags.Count;

  public static TagSet Merge(TagSet first, TagSet second) {
    HashSet<string> merged = [.. first.tags];
    merged.UnionWith(second.tags);
    return new TagSet(merged);
  }

  public bool Contains(string tag) {
    return tags.Contains(tag);
  }

  public TagSet With(IEnumerable<string> additional) {
    HashSet<string> combined = [.. tags];
    combined.UnionWith(additional);
    return new TagSet(combined);
  }

  /// <inheritdoc/>
  public IEnumerator<string> GetEnumerator() {
    return tags.GetEnumerator();
  }

  /// <inheritdoc/>
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}
