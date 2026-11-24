namespace RimWorks.Pickle.Hosting;

/// <summary>The host's translations, which the dashboard labels come from.</summary>
public interface ITranslator {
  /// <summary>Returns the text for a translation key in the active language.</summary>
  /// <param name="key">The translation key.</param>
  /// <returns>The translated text, or an empty string when the host has nothing for the key.</returns>
  string Translate(string key);
}
