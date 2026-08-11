# AGENTS.md

/ See `README.md` for gameplay/design overview and `CONTEXT.md` for domain vocabulary.

## Cursor Cloud specific instructions

This is a **Godot 4.7.1 (.NET/Mono) + C# (net10.0)** game with **[2dog](https://2dog.dev)**
hosts for desktop and browser. Main scene `scenes/Main.tscn`; most UI/entities are
created in code. There are no automated test projects in this repo.

### Toolchain
- `.NET SDK 10.0+` (`dotnet`) — pinned by root `global.json` (rollForward latestFeature).
  The VM image may only ship 8.0; install 10.x under `$HOME/.dotnet` if needed and put it
  first on `PATH`.
- `wasm-tools` workload (for web publish): `dotnet workload install wasm-tools`.
- `Godot 4.7.1` **.NET/Mono** build on `PATH` as `godot` (symlinked from
  `/opt/godot/Godot_v4.7.1-stable_mono_linux.x86_64`). Must be the Mono build — the
  Standard build has no C# support. Still used for editor authoring / asset import.

### Build / lint
- Game assembly: `dotnet build` (or `dotnet build TDProject.csproj`).
- Desktop host: `dotnet build TDProject.2dog`.
- Web host is excluded from plain solution builds; publish with `dotnet publish TDProject.web`.
- There is no separate linter and no unit-test suite.

### Run the game
- A first-time (or post-clean) run needs Godot to import assets and generate the
  `.godot/` folder: `godot --headless --import` (safe to re-run; `.godot/` is gitignored).
- **Godot editor / exe**: `DISPLAY=:1 godot` — hero-select, then a 5-minute run (WASD,
  weapons auto-attack).
- **2dog desktop host**: `DISPLAY=:1 dotnet run --project TDProject.2dog`
- **Web**: `dotnet publish TDProject.web` then serve `TDProject.web/AppBundle/`
  (e.g. `dotnet tool run dotnet-serve -- --directory TDProject.web/AppBundle -z -b`).
- Headless (`godot --headless`) is only useful for smoke checks; shutdown log spam
  (`Unreferenced static string`, `Pages in use exist at exit`, RID leaks) when killed
  via `timeout` is **not** a real error.

### Rendering / audio caveats (non-obvious)
- The VM has no GPU/Vulkan, so Godot auto-falls-back to **OpenGL 3 (llvmpipe software
  rendering)**. This works but is CPU-heavy (high CPU while running) — expect low FPS,
  not a bug. The `VK_KHR_surface not found` warning is expected.
- There is no audio device, so Godot falls back to the **dummy audio driver**; the ALSA
  `cannot find card '0'` errors on startup are expected and harmless.
