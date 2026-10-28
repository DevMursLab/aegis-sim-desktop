"""Verify an extracted self-contained Windows release, without using a .NET SDK.

Usage: python scripts/verify-package.py artifacts/AEGIS-SIM-3.3.0-win-x64.zip
Writes <archive>.verification.json after all checks pass. Uses isolated temp data.
"""
from pathlib import Path
import hashlib
import json
import os
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import zipfile


def verify(archive):
    archive = Path(archive).resolve()
    if os.name != "nt":
        raise SystemExit("Package execution checks require Windows x64.")
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    checksum = Path(str(archive) + ".sha256").read_text(encoding="ascii").split()
    assert checksum == [digest, archive.name], "Checksum sidecar does not match archive"
    with tempfile.TemporaryDirectory(prefix="aegis-package-") as temporary:
        work = Path(temporary)
        package = work / "extracted"
        with zipfile.ZipFile(archive) as bundle:
            names = bundle.namelist()
            assert len(names) == len(set(names)), "Duplicate ZIP entries"
            assert bundle.testzip() is None, "ZIP integrity failure"
            for name in names:
                target = (package / name).resolve()
                assert target.is_relative_to(package.resolve()), "Unsafe ZIP path"
                lower = name.lower()
                assert "session-secrets" not in lower and not lower.endswith((".pfx", ".p12", ".env")), "Unexpected credential file"
            required = ["package-manifest.json", "README.md", "Install.ps1", "Start-Local.cmd",
                        "Start-Seeded-Demo.cmd", "Start-Multiplayer.cmd", "app/Aegis.Desktop.Wpf.exe",
                        "host/Aegis.SyncHost.exe", "tools/Aegis.Tools.exe", "app/coreclr.dll",
                        "assets/maps/road-exercise.geojson", "docs/REVIEWER-GUIDE.md",
                        "docs/INTERVIEW-PREP.bn.md", "docs/images/forecast-lab.png"]
            for name in required:
                assert name in names, "Missing package file: " + name
            bundle.extractall(package)
        manifest = json.loads((package / "package-manifest.json").read_text(encoding="utf-8-sig"))
        assert manifest["selfContained"] and manifest["runtime"] == "win-x64"
        assert not manifest["sourceDirty"], "Release was built from uncommitted source"
        flags = subprocess.CREATE_NO_WINDOW

        def run(*args):
            result = subprocess.run([str(x) for x in args], capture_output=True, text=True,
                                    timeout=180, creationflags=flags)
            assert result.returncode == 0, (result.returncode, result.stdout[-2000:], result.stderr[-2000:])

        run(package / "app/Aegis.Desktop.Wpf.exe", "--data-dir", work / "fresh", "--verify-startup")
        run(package / "app/Aegis.Desktop.Wpf.exe", "--data-dir", package / "demo-data", "--verify-startup")
        run(package / "tools/Aegis.Tools.exe", "--profile", work / "profile.json")
        run(package / "tools/Aegis.Tools.exe", "--forecast-eval", work / "forecast.json")
        forecast = json.loads((work / "forecast.json").read_text(encoding="utf-8"))
        assert forecast["Evaluation"]["Mae"] < forecast["Evaluation"]["BaselineMae"]
        installed = work / "installed"
        run("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", package / "Install.ps1",
            "-Destination", installed, "-SkipShortcut")
        run(installed / "app/Aegis.Desktop.Wpf.exe", "--data-dir", work / "installed-data", "--verify-startup")
        with socket.socket() as listener:
            listener.bind(("127.0.0.1", 0))
            port = listener.getsockname()[1]
        with (work / "host.log").open("w", encoding="utf-8") as log:
            host = subprocess.Popen([str(package / "host/Aegis.SyncHost.exe"), "--urls",
                                     f"http://127.0.0.1:{port}", "--data-dir", str(work / "host-data"),
                                     "--scenario", str(package / "assets/maps/road-exercise.geojson")],
                                    stdout=log, stderr=log, creationflags=flags)
            try:
                for _ in range(100):
                    try:
                        with urllib.request.urlopen(f"http://127.0.0.1:{port}/health", timeout=1) as response:
                            health = json.load(response)
                        assert health["engine"] == "3.2.0", "Unexpected packaged simulation engine"
                        break
                    except (urllib.error.URLError, OSError):
                        assert host.poll() is None, "Packaged host exited before healthy"
                        time.sleep(0.2)
                else:
                    raise AssertionError("Packaged host did not become healthy")
            finally:
                if host.poll() is None:
                    host.terminate()
                host.wait(timeout=20)
        evidence = {"desktopVersion": manifest["desktopVersion"], "engineVersion": health["engine"],
                    "sourceCommit": manifest["sourceCommit"], "sha256": digest, "bytes": archive.stat().st_size,
                    "zipIntegrity": True, "credentialExclusion": True, "freshStartup": True,
                    "seededReplayStartup": True, "profileTool": True, "forecastTool": True,
                    "isolatedInstallAndStartup": True, "packagedRoadHost": True,
                    "limits": "Same Windows environment, isolated extraction/data. Not a clean physical machine or broad DPI/accessibility audit."}
        output = Path(str(archive) + ".verification.json")
        output.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
        print("Package verification passed:", output)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    verify(sys.argv[1])
