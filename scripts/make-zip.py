#!/usr/bin/env python
r"""
Builds the distributable zip of this assessment package.

Included : README, docs, scripts, bootstrap (setup.ps1, patches, config templates, patched eRSD KAR),
           sample-data, .NET source (no bin/obj), results (evidence) except bulky raw exports.
Excluded : logs, build outputs, UDS+ raw export copies (results/dotnet-output/udsplus/*/source), .git.

Usage: python make-zip.py [--out C:\git\Healthcare-Interop-Assessment.zip]
"""
import argparse, os, time, zipfile

EXCLUDE_DIRS = {"bin", "obj", "logs", ".git", ".vs", "__pycache__"}
EXCLUDE_SUFFIXES = (".part", ".err.log", ".db", ".db-shm", ".db-wal")

def wanted(rel):
    parts = rel.replace("\\", "/").split("/")
    if any(p in EXCLUDE_DIRS for p in parts):
        return False
    if rel.endswith(EXCLUDE_SUFFIXES):
        return False
    # raw identifiable export copies kept by the UDS+ pipeline are not needed for review
    if "/dotnet-output/udsplus/" in "/" + rel.replace("\\", "/") and "/source/" in "/" + rel.replace("\\", "/"):
        return False
    return True

def main():
    pkg = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(os.path.dirname(pkg), f"Healthcare-Interop-Assessment-{time.strftime('%Y%m%d')}.zip"))
    a = ap.parse_args()
    top = os.path.basename(pkg)  # interop-assessment
    n = 0
    with zipfile.ZipFile(a.out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for root, dirs, files in os.walk(pkg):
            dirs[:] = [d for d in dirs if d not in EXCLUDE_DIRS]
            for f in files:
                full = os.path.join(root, f)
                rel = os.path.relpath(full, pkg)
                if not wanted(rel):
                    continue
                z.write(full, os.path.join(top, rel))
                n += 1
    print(f"wrote {a.out}: {n} files, {os.path.getsize(a.out)/1024/1024:.1f} MB")

if __name__ == "__main__":
    main()
