"""Check shared Reins source identity; use --write deliberately after reviewing replay compatibility."""
import argparse
import hashlib
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--write', action='store_true')
args = parser.parse_args()
folder = Path(__file__).resolve().parents[1] / 'Packages/com.barrelrivals.core/Runtime/Reins'
digest = hashlib.sha256()
for name in ('ReinsContracts.cs', 'ReinsCourseJudge.cs', 'ReinsRun.cs', 'ReinsReplay.cs'):
    digest.update(name.encode() + b'\0' + (folder / name).read_bytes())
value = digest.hexdigest()
path = folder / 'ReinsRuleFingerprint.cs'
if args.write:
    path.write_text('// Generated from the four Reins simulation source files; ReinsLabBuilder validates this before build.\n'
                    'namespace BarrelRivals.Core.Reins\n{\n    public static class ReinsRuleFingerprint\n    {\n'
                    f'        public const string Sha256 = "{value}";\n'
                    '    }\n}\n')
    print('Fingerprint written. Re-run fixtures; older incompatible recordings must remain rejected.')
elif f'"{value}"' not in path.read_text():
    raise SystemExit('Rule fingerprint mismatch. Review version/replay compatibility before using --write.')
else:
    print('Reins rule fingerprint matches source: ' + value)
