# UI fonts

`NotoSansSC-Game.ttf` is a **glyph subset** of [Noto Sans SC](https://github.com/notofonts/noto-cjk) (OFL), sized for the web build.

Godot’s default font has no CJK coverage, and **web/WASM cannot use OS font fallbacks**, so Chinese would show as tofu/garbled without a bundled font. Desktop would still look fine via system fonts.

The subset includes project UI copy **plus GB2312 level-1 hanzi** (~3755) so player-typed character names render correctly on web.

Regenerate after adding a lot of new Chinese copy (or when expanding name coverage):

```bash
# Download NotoSansSC-Regular.otf from the Sans release on notofonts/noto-cjk, then:
python3 assets/fonts/regen_subset.py /path/to/NotoSansSC-Regular.otf
```

License: see `OFL-NotoSansSC.txt`.
