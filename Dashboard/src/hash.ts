import type { Selection } from "./types";

export function toHash(selection: Selection): string {
  return `#${encodeURIComponent(selection.path)}:${selection.index}`;
}

export function readHash(): Selection | null {
  const raw = window.location.hash.slice(1);
  if (!raw) return null;

  const split = raw.lastIndexOf(":");
  if (split < 0) return null;

  const index = Number(raw.slice(split + 1));
  if (!Number.isInteger(index)) return null;

  return { path: decodeURIComponent(raw.slice(0, split)), index };
}
