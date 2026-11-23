using RimWorks.Pickle.Hosting;
using Verse;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class Translations : ITranslator {
  public string Translate(string key) {
    if (LanguageDatabase.activeLanguage == null || !key.CanTranslate()) {
      return string.Empty;
    }

    return key.Translate().ToString();
  }
}
