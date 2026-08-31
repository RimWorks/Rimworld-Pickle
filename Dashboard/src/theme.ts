const KEY = "pickle-theme";

export const THEMES = { dark: "dim", light: "winter" } as const;

// A sandboxed iframe throws on the property access itself, not just on write, so a
// report opened in one has to treat storage as absent rather than fail to render.
export function readTheme(): string | null {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
}

export function writeTheme(value: string): void {
  try {
    localStorage.setItem(KEY, value);
  } catch {
    // no storage in this context; the theme just does not persist
  }
}

// Storage does not survive a sandboxed frame, so a report embedded in a host page takes
// the host's theme rather than resetting to its own default on every open.
export function initialTheme(): string {
  const stored = readTheme();
  if (stored) {
    return stored;
  }

  const host = document.documentElement.dataset.docbinTheme;
  if (host === "dark") return THEMES.dark;
  if (host === "light") return THEMES.light;

  try {
    if (window.matchMedia("(prefers-color-scheme: light)").matches) {
      return THEMES.light;
    }
  } catch {
    // no matchMedia here; fall through to the default
  }

  return THEMES.dark;
}
