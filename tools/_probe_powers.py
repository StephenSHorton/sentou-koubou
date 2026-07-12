from pathlib import Path
import re
import struct
import zlib

xml_path = Path(r"C:\Users\4step\.nuget\packages\alchyr.sts2.baselib\3.3.5\lib\net9.0\BaseLib.xml")
xml = xml_path.read_text(encoding="utf-8", errors="ignore")
members = re.findall(r'<member name="([^"]*Power[^"]*)"', xml)
print("BaseLib power-ish members:", len(members))
for m in members[:100]:
    print(m)

# strings from sts2.dll looking for After* methods on PowerModel
sts2 = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll")
data = sts2.read_bytes()
# extract utf-8-ish strings
strings = re.findall(rb"[A-Za-z_][A-Za-z0-9_]{4,60}", data)
decoded = [s.decode("ascii", errors="ignore") for s in strings]
interesting = [
    s
    for s in decoded
    if any(
        k in s
        for k in (
            "AfterKill",
            "OnKill",
            "BeforeDeath",
            "AfterDeath",
            "OnDeath",
            "AfterExhaust",
            "Whenever",
            "AfterCardPlayed",
            "OnCardPlayed",
            "AfterDamage",
            "BeforeDamage",
            "AfterHpLost",
            "OnTurnStart",
            "AfterTurn",
            "PowerModel",
            "AfterMonster",
            "Fatal",
        )
    )
]
# unique preserve order
seen = set()
uniq = []
for s in interesting:
    if s not in seen:
        seen.add(s)
        uniq.append(s)
print("\nsts2 interesting strings:")
for s in uniq[:200]:
    print(s)
