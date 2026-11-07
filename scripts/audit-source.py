"""Supplementary source/dependency review; not a proof of game approval."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
forbidden = re.compile(
    r"DllImport|LibraryImport|System\.Net|HttpClient|WebSocket|Process\."
    r"|OpenProcess|ReadProcessMemory|WriteProcessMemory|CreateRemoteThread"
    r"|SetWindowsHook|SendInput|keybd_event|mouse_event|PacketDotNet"
    r"|SharpPcap|EasyHook|Vortice|SharpDX|overwolf\.games|System\.Reflection\.Emit"
)
errors = []
files = [p for p in (root / "src").rglob("*")
         if p.suffix in (".cs", ".xaml", ".csproj", ".manifest")
         and not {"obj", "bin"}.intersection(p.relative_to(root).parts)]
for path in files:
    for number, line in enumerate(path.read_text().splitlines(), 1):
        if forbidden.search(line):
            errors.append(f"{path.relative_to(root)}:{number}: requires review")
for project in (root / "src").rglob("*.csproj"):
    if any(ET.parse(project).iter("PackageReference")):
        errors.append(f"{project.name}: unexpected explicit package dependency")
manifest = ET.parse(root / "src/RoutePlanner.App/app.manifest")
requested = manifest.find(".//{urn:schemas-microsoft-com:asm.v3}requestedExecutionLevel")
if requested is None or requested.attrib != {"level": "asInvoker", "uiAccess": "false"}:
    errors.append("unexpected application privilege declaration")
if errors:
    print("\n".join(errors))
    sys.exit(1)
print(f"PASS supplementary source review: {len(files)} application files, no flagged API/pattern")
print("PASS no explicit PackageReference; ordinary asInvoker / uiAccess=false manifest")
print("LIMIT: source pattern checks do not establish Fortnite permission or ban safety")
