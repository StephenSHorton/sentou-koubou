"""Inject char_select_bg_*.tscn into a Godot 4.5 v3 PCK (PckPacker-compatible).

PckPacker cannot pack .tscn files, so we inject them after the image pack is built.
Paths are written without the res:// prefix (PACK_REL_FILEBASE).
"""
from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path


def align_pad(pos: int, boundary: int = 32) -> int:
    rem = pos % boundary
    return 0 if rem == 0 else boundary - rem


def align_pad4(n: int) -> int:
    rem = n % 4
    return 0 if rem == 0 else 4 - rem


def read_pck(path: Path) -> list[tuple[str, bytes]]:
    data = path.read_bytes()
    if data[:4] != b"GDPC":
        raise ValueError(f"Not a Godot PCK: {path}")
    # header: magic(4) + format(4) + eng maj/min/patch (4*3) + flags(4) + file_base(8) + file_base_ofs(8) + reserved
    fmt_ver, eng_maj, eng_min, eng_patch, flags = struct.unpack_from("<IIIII", data, 4)
    file_base = struct.unpack_from("<Q", data, 24)[0]
    # After packed file data comes file count at end section
    # With PACK_REL_FILEBASE (0x2), offsets are relative to file_base
    # Directory is at end: after all file data
    # Structure from PckWriter: header 112, then file data, then count + entries
    # file_base in header is 112; second long is total data end offset from start? 
    # From writer: value = 112 + num (total size of header+data), written at offset 32
    data_end = struct.unpack_from("<Q", data, 32)[0]
    # Directory starts at data_end
    off = data_end
    count = struct.unpack_from("<I", data, off)[0]
    off += 4
    files: list[tuple[str, bytes]] = []
    for _ in range(count):
        path_size = struct.unpack_from("<I", data, off)[0]
        off += 4
        # path_size includes padding to 4-byte align of path bytes
        # actual path is null-free utf8; pad is zeros
        path_bytes = data[off : off + path_size]
        off += path_size
        # strip trailing nulls/pad
        path_str = path_bytes.split(b"\x00")[0].decode("utf-8")
        # offset, size, md5(16), flags(4)
        file_ofs, file_size = struct.unpack_from("<QQ", data, off)
        off += 16
        md5 = data[off : off + 16]
        off += 16
        flags_u = struct.unpack_from("<I", data, off)[0]
        off += 4
        # With PACK_REL_FILEBASE, file_ofs is relative to start of file section (after header)
        # Writer stores offset as position after 112+padding within the stream from 0...
        # Looking at writer: item3 = num which starts at 0 after header alignment, and
        # binaryWriter writes padding then data starting at position 112+...
        # So absolute offset in file = 112 + file_ofs? 
        # Writer: list.Add((file.Path, num, file.Data.Length, item)) where num is
        # cumulative from 0 with AlignPadding(112+num) first...
        # Actually: num starts 0; for each file: pad = AlignPadding(112+num); num+=pad; store num as offset; num+=len
        # When writing: write header 112 bytes, then for each: write pad then data
        # So absolute start of first file data = 112 + first_pad, and stored offset = first_pad? 
        # Wait: AlignPadding(112+num) when num=0 is AlignPadding(112)=0 since 112%32=16? 112/32=3.5, 112%32=16, pad=16
        # So first pad=16, num becomes 16, offset stored=16, then data written after 112+16=128
        # Absolute file offset = 112 + stored_offset? first data at 128, stored 16, so abs = 112+16 = 128. Yes.
        abs_ofs = 112 + file_ofs
        payload = data[abs_ofs : abs_ofs + file_size]
        files.append((path_str, payload))
    return files


def write_pck(path: Path, files: list[tuple[str, bytes]]) -> None:
    files = sorted(files, key=lambda x: x[0])
    # Mirror StS2PckPacker.PckWriter
    entries_meta: list[tuple[str, int, int, bytes]] = []
    blobs: list[tuple[int, bytes]] = []
    num = 0
    for fpath, fdata in files:
        pad = align_pad(112 + num)
        num += pad
        md5 = hashlib.md5(fdata).digest()
        entries_meta.append((fpath, num, len(fdata), md5))
        blobs.append((pad, fdata))
        num += len(fdata)
    data_end = 112 + num

    with path.open("wb") as fh:
        fh.write(b"GDPC")
        fh.write(struct.pack("<IIIII", 3, 4, 5, 1, 2))  # fmt, eng, flags=PACK_REL_FILEBASE
        fh.write(struct.pack("<Q", 112))  # file base
        fh.write(struct.pack("<Q", data_end))
        fh.write(bytes(64))
        fh.write(bytes(8))
        for pad, fdata in blobs:
            if pad:
                fh.write(bytes(pad))
            fh.write(fdata)
        fh.write(struct.pack("<I", len(entries_meta)))
        for fpath, fofs, fsize, md5 in entries_meta:
            pb = fpath.encode("utf-8")
            pad4 = align_pad4(len(pb))
            # Writer: value2 = bytes.Length + num4; writes value2 then bytes then pad
            fh.write(struct.pack("<I", len(pb) + pad4))
            fh.write(pb)
            if pad4:
                fh.write(bytes(pad4))
            fh.write(struct.pack("<QQ", fofs, fsize))
            fh.write(md5)
            fh.write(struct.pack("<I", 0))


def inject(pck_path: Path, inject_path: str, content: bytes) -> None:
    files = read_pck(pck_path)
    # replace or add
    files = [(p, d) for p, d in files if p != inject_path]
    files.append((inject_path, content))
    write_pck(pck_path, files)
    print(f"injected {inject_path} into {pck_path} ({len(content)} bytes, {len(files)} files)")


def make_bg_tscn(texture_res_path: str, node_name: str) -> bytes:
    # texture_res_path like res://Brennen/images/charui/char_select_bg_brennen.png
    text = f"""[gd_scene load_steps=2 format=3]

[ext_resource type=\"Texture2D\" path=\"{texture_res_path}\" id=\"1_bg\"]

[node name=\"{node_name}\" type=\"TextureRect\"]
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
texture = ExtResource(\"1_bg\")
expand_mode = 1
stretch_mode = 6
"""
    return text.encode("utf-8")


def main() -> int:
    # usage: inject_char_select_bg.py <pck> <char_lower> <res_prefix>
    if len(sys.argv) != 4:
        print("Usage: inject_char_select_bg.py <pckPath> <charLower> <resPrefix>")
        return 1
    pck = Path(sys.argv[1])
    char = sys.argv[2]  # brennen
    prefix = sys.argv[3]  # Brennen
    tscn_path = f"scenes/screens/char_select/char_select_bg_{char}.tscn"
    tex = f"res://{prefix}/images/charui/char_select_bg_{char}.png"
    content = make_bg_tscn(tex, f"CharSelectBg{prefix}")
    inject(pck, tscn_path, content)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
