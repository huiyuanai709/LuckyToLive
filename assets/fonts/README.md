# UI fonts

`NotoSansSC-Game.ttf` is a **glyph subset** of [Noto Sans SC](https://github.com/notofonts/noto-cjk) (OFL), sized for the web build.

Godot’s default font has no CJK coverage, and **web/WASM cannot use OS font fallbacks**, so Chinese would show as tofu/garbled without a bundled font. Desktop would still look fine via system fonts.

Regenerate after adding a lot of new Chinese copy:

```bash
# Download NotoSansSC-Regular.otf from the Sans release on notofonts/noto-cjk, then:
python3 assets/fonts/regen_subset.py /path/to/NotoSansSC-Regular.otf
```

License: see `OFL-NotoSansSC.txt`.
