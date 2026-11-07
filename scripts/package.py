"""Package an already published binary; no compilation or execution claims."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile
from datetime import datetime, timezone

root = Path(__file__).resolve().parent.parent
output = root / "artifacts/win-x64"
if not (output / "RoutePlanner.exe").is_file():
    sys.exit("Publish RoutePlanner.exe first")
for symbols in output.glob("*.pdb"):
    symbols.unlink()  # Optional debugging symbols are not part of the portable package.
for name in ("README.md", "LICENSE"):
    shutil.copy2(root / name, output / name)
for name in ("docs", "third-party", "examples"):
    shutil.copytree(root / name, output / name, dirs_exist_ok=True)
(output / "STARTEN.txt").write_text(
    "ROUTE PLANNER 0.2.1 - LOKALE ROUTENPLANUNG\n\n"
    "RoutePlanner.exe unter Windows x64 starten. Keine .NET-Installation notwendig.\n"
    "Eigene Karten und Daten, keine Fortnite-Verbindung oder Live-Erkennung.\n"
    "Keine Freigabe fuer Verwendung mit Fortnite. README.md und docs lesen.\n"
    "Die Runtime kann native Dateien ins temporaere .net-Verzeichnis entpacken.\n"
    "Lizenzen und Dokumentation sind separate Dateien im Paket.\n",
    encoding="utf-8",
)
(output / "BUILD_INFO.json").write_text(json.dumps({
    "sdk": "10.0.403", "runtime": "10.0.14", "rid": "win-x64", "configuration": "Release",
    "buildHost": "Linux x64", "packagedUtc": datetime.now(timezone.utc).isoformat(),
    "executionVerified": False,
}, indent=2) + "\n")

def digest(path):
    with path.open("rb") as file:
        return hashlib.file_digest(file, "sha256").hexdigest()

files = sorted(p for p in output.rglob("*") if p.is_file() and p.name != "SHA256SUMS.txt")
(output / "SHA256SUMS.txt").write_text("".join(
    f"{digest(p)}  {p.relative_to(output).as_posix()}\n" for p in files))
archive = root / "artifacts/Routenatelier-win-x64.zip"
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zip:
    for p in sorted(output.rglob("*")):
        if p.is_file():
            zip.write(p, p.relative_to(output))
(archive.parent / (archive.name + ".sha256")).write_text(f"{digest(archive)}  {archive.name}\n")
print(f"Packaged {archive.name}: {archive.stat().st_size:,} bytes")
print(f"EXE SHA256 {digest(output / 'RoutePlanner.exe')}")
print(f"ZIP SHA256 {digest(archive)}")
