using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorks.Pickle.Core.Model;

/// <summary>The <c>@tag</c> names attached to a feature, rule or scenario, deduplicated.</summary>
public class TagSet : IReadOnlyCollection<string> {
  private readonly HashSet<string> tags;

  /// <summary>Initializes a new instance.</summary>
  /// <param name="tagList">The tags to store. Duplicates collapse to one entry.</param>
  public TagSet(IEnumerable<string> tagList) {
    tags = [.. tagList];
  }

  /// <summary>How many distinct tags this set holds.</summary>
  public int Count => tags.Count;

  /// <summary>Combines two tag sets into one, with duplicates collapsed.</summary>
  /// <param name="first">The first set of tags.</param>
  /// <param name="second">The second set of tags.</param>
  /// <returns>A new set holding every tag from both inputs.</returns>
  public static TagSet Merge(TagSet first, TagSet second) {
    HashSet<string> merged = [.. first.tags];
    merged.UnionWith(second.tags);
    return new TagSet(merged);
  }

  /// <summary>Checks whether a tag is present in this set.</summary>
  /// <param name="tag">The tag to look for, including its leading <c>@</c>.</param>
  /// <returns><c>true</c> if the tag is present.</returns>
  public bool Contains(string tag) {
    return tags.Contains(tag);
  }

  /// <summary>Builds a new set carrying this set's tags plus more, for layering a scenario's tags onto its
  /// feature's without mutating either.</summary>
  /// <param name="additional">The tags to add on top of this set's own.</param>
  /// <returns>A new set holding this set's tags and the additional ones combined.</returns>
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
