#!/usr/bin/env python3
"""Baimendutako egoera zehatza egiaztatu Git aldatzeke."""
from pathlib import Path
import subprocess
import sys

repo = Path(sys.argv[1]); initial = Path(sys.argv[2]); tools = Path(sys.argv[3])

def entries():
    raw = subprocess.check_output(["git", "-C", str(repo), "status", "--porcelain=v1", "-z", "--untracked-files=all"])
    parts = [p for p in raw.split(b"\0") if p]
    return {p[3:].decode("utf-8", "surrogateescape"): p[:2].decode("ascii") for p in parts}

old = {}
for line in initial.read_text().splitlines():
    status, name = line[:2], line[3:]
    old[name] = status
now = entries()
base = "Laberintoa3D_Final/Assets/Editor/"
new_assets = {base + "HorrorBuilderV11.meta",
              base + "HorrorBuilderV11/HorrorBuilderV11.cs",
              base + "HorrorBuilderV11/HorrorBuilderV11.cs.meta"}
new_tools = {"tools/HorrorBuilderV11/" + str(x.relative_to(tools)) for x in tools.rglob("*") if x.is_file()}
if len(old) != 63:
    sys.exit("HBB11_VERIFY_ERROR: aurretik ez zeuden 63 fitxategi")
if any(now.get(n) != s for n, s in old.items()):
    sys.exit("HBB11_VERIFY_ERROR: V7-V10ko fitxategi baten Git egoera aldatu da")
added = set(now) - set(old)
expected = new_assets | new_tools
if added != expected or any(now[n] != "??" for n in added):
    sys.exit("HBB11_VERIFY_ERROR: fitxategi ez-baimenduak: falta=" + repr(sorted(expected-added)) + " extra=" + repr(sorted(added-expected)))
print(f"HBB11_STATUS_OK: {len(old)} aldaketa zahar + {len(added)} fitxategi berri; after")
