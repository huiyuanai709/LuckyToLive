# AGENTS.md

/ See `README.md` for gameplay/design overview and `CONTEXT.md` for domain vocabulary.

## Cursor Cloud specific instructions

This is a **Godot 4.7.1 (.NET/Mono) + C# (net8.0)** game. It is a graphical desktop
game (main scene `scenes/Main.tscn`, most UI/entities are created in code). There are
no automated test projects in this repo.

### Toolchain (already installed in the VM image)
- `.NET SDK 8.0` (`dotnet`).
- `Godot 4.7.1` **.NET/Mono** build on `PATH` as `godot` (symlinked from
  `/opt/godot/Godot_v4.7.1-stable_mono_linux.x86_64`). Must be the Mono build — the
  Standard build has no C# support.

### Build / lint
- Build (also serves as the lint/compile check): `dotnet build`. Output goes to
  `.godot/mono/temp/bin/`. There is no separate linter and no unit-test suite.

### Run the game
- A first-time (or post-clean) run needs Godot to import assets and generate the
  `.godot/` folder: `godot --headless --import` (safe to re-run; `.godot/` is gitignored).
- Then run with the desktop display: `DISPLAY=:1 godot`. The game opens on the
  hero-select screen; pick a hero to start a 5-minute run (WASD to move, weapons
  auto-attack).
- Headless (`godot --headless`) launches the game loop but is only useful for smoke
  checks; the shutdown log spam (`Unreferenced static string`, `Pages in use exist at
  exit`, RID leaks) appears when the process is killed via `timeout` and is **not** a
  real error.

### Rendering / audio caveats (non-obvious)
- The VM has no GPU/Vulkan, so Godot auto-falls-back to **OpenGL 3 (llvmpipe software
  rendering)**. This works but is CPU-heavy (high CPU while running) — expect low FPS,
  not a bug. The `VK_KHR_surface not found` warning is expected.
- There is no audio device, so Godot falls back to the **dummy audio driver**; the ALSA
  `cannot find card '0'` errors on startup are expected and harmless.
