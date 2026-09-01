#!/usr/bin/env python3
"""Fail when a built-in step has no row in Docs/steps.md.

Docs/steps.md is the published catalogue. A step that never reaches it is a step
nobody can find, so this is a build failure rather than a lint warning.
"""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / "Docs" / "steps.md"

# Pickle.Vanilla ships the built-in steps. The step classes under Source/Pickle/Runtime are
# fixtures for the runner's own smoke tests and are deliberately undocumented.
STEP_DIR = ROOT / "Source" / "Pickle.Vanilla"
ENGINE_FILE = ROOT / "Source" / "Pickle" / "Run" / "RunSession.cs"

ATTRIBUTE = re.compile(r'\[(?:Given|When|Then)\("((?:[^"\\]|\\.)*)"')
ENGINE = re.compile(r'AddEngineStep\(\s*"((?:[^"\\]|\\.)*)"')
DOC_ROW = re.compile(r"^\|\s*`([^`]+)`")


def unescape(expression):
    """Source escapes the regex parens a cucumber expression needs; docs do not."""
    return expression.replace("\\\\", "\\").replace('\\"', '"').replace("\\", "")


def steps():
    found = {}
    for path in sorted(STEP_DIR.glob("*.cs")):
        for match in ATTRIBUTE.finditer(path.read_text(encoding="utf-8")):
            found.setdefault(unescape(match.group(1)), path.name)

    for match in ENGINE.finditer(ENGINE_FILE.read_text(encoding="utf-8")):
        found.setdefault(unescape(match.group(1)), ENGINE_FILE.name)

    return found


def documented():
    return {
        DOC_ROW.match(line).group(1)
        for line in DOCS.read_text(encoding="utf-8").splitlines()
        if DOC_ROW.match(line)
    }


def main():
    found = steps()
    if not found:
        print("check-step-docs: found no steps at all, which means the parser broke")
        return 2

    missing = sorted((name, expr) for expr, name in found.items() if expr not in documented())
    print(f"check-step-docs: {len(found)} steps, {len(found) - len(missing)} documented")

    if not missing:
        return 0

    print(f"\n{len(missing)} step(s) have no row in Docs/steps.md:\n")
    for name, expr in missing:
        print(f"  {name}: {expr}")

    print("\nAdd a row to the table for that family, in the form:")
    print("  | `the step text` | What it does |")
    return 1


if __name__ == "__main__":
    sys.exit(main())
