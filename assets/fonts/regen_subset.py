#!/usr/bin/env python3
"""Regenerate NotoSansSC-Game.ttf from a full NotoSansSC-Regular.otf source.

Usage:
  python3 assets/fonts/regen_subset.py /path/to/NotoSansSC-Regular.otf

Requires: pip install fonttools brotli
Source font: https://github.com/notofonts/noto-cjk/releases (Sans / NotoSansSC)
"""
from __future__ import annotations

import csv
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / "NotoSansSC-Game.ttf"
CHARSET = Path(__file__).resolve().parent / "charset.txt"


def collect_chars() -> str:
	chars = set(chr(c) for c in range(0x20, 0x7F))
	chars.update(chr(c) for c in range(0xA0, 0x100))
	chars.update(chr(c) for c in range(0xFF01, 0xFF5F))
	chars.update("，。！？：；、（）【】《》「」『』…—–‐‑·•‧※★☆●○◆◇■□▲△▼▽→←↑↓￥℃°±×÷％～｜／＼＿中文")
	skip = {"/.git", "/TDProject.", "/.godot", "/node_modules", "/.scratch", "/assets/fonts"}
	for dirpath, _, files in os.walk(ROOT):
		if any(p in dirpath for p in skip):
			continue
		for name in files:
			if not name.endswith((".cs", ".csv", ".gd", ".md", ".tscn")):
				continue
			path = Path(dirpath) / name
			try:
				text = path.read_text(encoding="utf-8")
			except OSError:
				continue
			chars.update(ch for ch in text if ord(ch) > 127)
	# Always include translation CSV explicitly
	csv_path = ROOT / "assets/i18n/translations.csv"
	if csv_path.exists():
		with csv_path.open(encoding="utf-8") as f:
			for row in csv.reader(f):
				for col in row:
					chars.update(ch for ch in col if ord(ch) > 127)
	return "".join(sorted(chars))


def main() -> int:
	if len(sys.argv) != 2:
		print(__doc__)
		return 2
	src = Path(sys.argv[1])
	if not src.is_file():
		print(f"missing source font: {src}", file=sys.stderr)
		return 1
	text = collect_chars()
	CHARSET.write_text(text, encoding="utf-8")
	print(f"charset size: {len(text)}")
	subprocess.check_call(
		[
			"pyftsubset",
			str(src),
			f"--text-file={CHARSET}",
			f"--output-file={OUT}",
			"--layout-features=*",
			"--notdef-glyph",
			"--notdef-outline",
			"--recommended-glyphs",
			"--name-IDs=*",
			"--name-legacy",
			"--name-languages=*",
		]
	)
	print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")
	return 0


if __name__ == "__main__":
	raise SystemExit(main())
