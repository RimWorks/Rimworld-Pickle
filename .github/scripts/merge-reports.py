#!/usr/bin/env python3
"""Splices several Pickle report.html files into one that can switch between mod sets.

    merge-reports.py <out.html> <report.html> [report.html ...]
    merge-reports.py --self-test

The report bundle already carries its whole run in one <script id="pickle-report">, so
merging never renders anything. It reads each payload, tags it with its set name, and
writes them back as {"sets": [...]} into a copy of the first file.
"""
import json
import re
import sys
from pathlib import Path

PAYLOAD = re.compile(
    r'(<script id="pickle-report" type="application/json">)(.*?)(</script>)', re.S
)

# BuildPayload escapes </ so a failure message cannot close the script tag it lives in.
# json.loads decodes \/ on its own, so only the write side has to put it back.
def _escape(text):
    return text.replace("</", "<\\/")


def _read_payload(html):
    match = PAYLOAD.search(html)
    if not match:
        raise ValueError("no pickle-report payload in this file")
    return json.loads(match.group(2))


def _prefix_film_paths(payload, set_name):
    """Films are linked, not inlined, so two sets would otherwise claim the same folder."""
    for feature in payload.get("features", []):
        for scenario in feature.get("scenarios", []):
            for attachment in scenario.get("attachments", []):
                content = attachment.get("content", "")
                if content.startswith("screenshots/film/"):
                    attachment["content"] = f"screenshots/{set_name}/film/" + content[len("screenshots/film/"):]


def merge(paths):
    sets = []
    for index, path in enumerate(paths):
        payload = _read_payload(Path(path).read_text(encoding="utf-8"))
        # The flag names it. Falling back to the folder beats an unlabelled column.
        name = payload.get("setName") or Path(path).resolve().parent.name or f"set-{index + 1}"
        payload["setName"] = name
        _prefix_film_paths(payload, name)
        sets.append(payload)
    return {"sets": sets}


def main(argv):
    if argv[:1] == ["--self-test"]:
        return _self_test()
    if len(argv) < 2:
        print(__doc__, file=sys.stderr)
        return 2

    out, inputs = argv[0], argv[1:]
    template = Path(inputs[0]).read_text(encoding="utf-8")
    merged = _escape(json.dumps(merge(inputs), separators=(",", ":")))
    Path(out).write_text(PAYLOAD.sub(lambda m: m.group(1) + merged + m.group(3), template, count=1), encoding="utf-8")
    print(f"merge-reports: {len(inputs)} set(s) -> {out}")
    return 0


def _report(payload):
    return '<!doctype html><script id="pickle-report" type="application/json">' + _escape(json.dumps(payload)) + "</script>"


def _self_test():
    import tempfile

    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        # A failure message holding </script> is the case the escaping exists for.
        a = {"setName": "harmony", "exitReason": "passed", "features": [
            {"name": "F", "scenarios": [{"name": "s", "failureMessage": "broke on </script>",
                                         "attachments": [{"name": "film-frames", "content": "screenshots/film/f/0000.jpg"}]}]}]}
        b = {"exitReason": "failed", "features": []}
        (root / "harmony").mkdir()
        (root / "concord").mkdir()
        (root / "harmony" / "report.html").write_text(_report(a), encoding="utf-8")
        (root / "concord" / "report.html").write_text(_report(b), encoding="utf-8")

        out = root / "merged.html"
        assert main([str(out), str(root / "harmony" / "report.html"), str(root / "concord" / "report.html")]) == 0

        merged = _read_payload(out.read_text(encoding="utf-8"))
        assert [s["setName"] for s in merged["sets"]] == ["harmony", "concord"], merged
        assert merged["sets"][0]["features"][0]["scenarios"][0]["failureMessage"] == "broke on </script>"
        assert merged["sets"][0]["features"][0]["scenarios"][0]["attachments"][0]["content"] \
            == "screenshots/harmony/film/f/0000.jpg"
        assert "</script>" not in out.read_text(encoding="utf-8").split("</script>")[0]

        # One report in still reads as one report, so a single-set merge is not a special case.
        single = root / "single.html"
        assert main([str(single), str(root / "harmony" / "report.html")]) == 0
        assert len(_read_payload(single.read_text(encoding="utf-8"))["sets"]) == 1

    print("merge-reports: self-test passed")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
