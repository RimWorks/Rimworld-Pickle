const KEY = "pickle-theme";

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
