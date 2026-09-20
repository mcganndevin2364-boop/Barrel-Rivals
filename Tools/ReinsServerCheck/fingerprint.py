#!/usr/bin/env python3
"""Print or deliberately refresh the v2 simulation fingerprint after reviewing rule edits."""
import hashlib
import json
from pathlib import Path
import re
import sys

REPO = Path(__file__).resolve().parents[2]
CORE = REPO / "Packages/com.barrelrivals.core/Runtime/Reins"
SOURCES = ("ReinsContracts.cs", "ReinsCourseJudge.cs", "ReinsRun.cs", "ReinsReplay.cs",
           "ReinsAlley.cs", "../StandardCourse.cs")
if sys.argv[1:] not in ([], ["--write"]):
    raise SystemExit("Usage: fingerprint.py [--write]")
content = bytearray()
for name in SOURCES:
    content.extend(name.encode("utf-8") + b"\0")
    content.extend((CORE / name).read_bytes())
digest = hashlib.sha256(content).hexdigest()
if sys.argv[1:]:
    if not re.search(r"\bRulesVersion\s*=\s*2\s*;", (CORE / "ReinsRun.cs").read_text()):
        raise SystemExit("Refusing to write a v2 fingerprint before RulesVersion 2 exists.")
    target = CORE / "ReinsRuleFingerprint.cs"
    text = target.read_text()
    text, count = re.subn(r'public const string Sha256 = "[0-9a-f]{64}";',
                         'public const string Sha256 = "' + digest + '";', text)
    if count != 1:
        raise SystemExit("Expected exactly one fingerprint constant; no files changed.")
    text = re.sub(r"^// Generated[^\n]*", "// Generated from six simulation/geometry sources; see Contracts/Reins/README.md.", text)
    preview_path = REPO / "Contracts/Reins/preview-request.v2.json"
    preview = json.loads(preview_path.read_text())
    if preview["contractVersion"] != 2 or preview["manifest"]["courseId"] != "reins-v2":
        raise SystemExit("Expected a v2 preview request; no files changed.")
    preview["ruleFingerprint"] = digest
    target.write_text(text)
    preview_path.write_text(json.dumps(preview, indent=2) + "\n")
print(digest)
