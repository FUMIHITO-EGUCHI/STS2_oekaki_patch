# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

The authoritative agent rules live in [AGENTS.md](AGENTS.md) (working rules, scope, decision priorities, things to avoid). Read it before any non-trivial change. This file only adds Claude-specific shortcuts and the architecture summary.

## Commands

```powershell
# Build (pre-commit hook runs both)
dotnet build src/EraserMod -c Release   # Harmony patch DLL  (.NET 8)
dotnet build src/Injector  -c Release   # IL injector tool   (.NET 9)

# Install / uninstall against a Steam install
.\install.ps1                 # default Steam path
.\install.ps1 -GameDir "..."  # custom path
.\uninstall.ps1               # restores sts2.dll from sts2.dll.orig

# Branch / hook setup (Git Bash required for .sh scripts on Windows)
& "C:\Program Files\Git\bin\sh.exe" scripts/setup-hooks.sh
& "C:\Program Files\Git\bin\sh.exe" scripts/start-issue.sh <type> <issue#> <topic>
```

There is no test suite. Verification is build success + manual in-game check on the supported STS2 version listed in README.md.

## Architecture (the cross-file picture)

The mod patches a shipped Godot 4 + .NET game (Slay the Spire 2) at runtime. Three pieces cooperate:

1. **`src/Injector/`** — *(legacy, not used in current ModManager-based setup)* one-shot tool that used **dnlib** to rewrite `sts2.dll`'s `<Module>.cctor`. Retained for pre-commit hook parity; `uninstall.ps1` still restores `sts2.dll` from `sts2.dll.orig` for users who installed v0.0.1 via the old injector.

2. **`src/EraserMod/`** — the Harmony patch DLL, loaded by `ModManager` via `[ModInitializer("Init")]` on `Bootstrap`.
   - `Bootstrap.cs` — entry point; sets up logging, calls `Harmony.PatchAll()`.
   - `SupportedVersion.cs` — single source for `GameVersion` / `GameCommit` / `GameDate` constants.
   - `Patches.cs` — drawing patches: `NMapDrawings.CreateLineForPlayer` (eraser/pencil width + peer color), `BeginLineLocal` (style announce).
   - `NetPatches.cs` — MP patches: `NMapDrawings.Initialize` (register handlers + send hello), `_ExitTree` (unregister handlers).
   - `Config.cs` — reads/writes `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.json`; auto-migrates from legacy `config.txt`.
   - `HotkeyHandler.cs` — `[`/`]` eraser width, `Shift+[`/`]` pencil width, `\`/`Shift+\` reset, `Ctrl+Z` undo (+ MP notify), `Ctrl+Shift+E` toolbar, `Ctrl+Shift+L` log overlay.
   - `Toolbar.cs` — in-game toolbar UI (tool select, width display, color picker).
   - `CursorPreview.cs` — circle cursor scaled to current eraser/pencil width.
   - `UndoStack.cs` — local undo stack; `UndoPeer` removes the last line drawn by a given peer net-id.
   - `MapReflection.cs` — reflection helpers for `NMapDrawings` private fields (`_netService`, `_playerCollection`, drawing state).
   - `ColorUtil.cs` — `#RRGGBB` hex → `Godot.Color` parse utility.
   - `Toast.cs` — transient in-game toast messages.
   - `LogOverlay.cs` — toggleable MOD log overlay node.
   - `Net/NetSync.cs` — holds `INetGameService` reference; send helpers for hello / style / undo messages.
   - `Net/PeerStyleCache.cs` — per-peer style cache and MOD-peer registry (hello handshake guard).
   - `Net/zEraserModHelloMessage.cs` / `zEraserModLineStyleMessage.cs` / `zEraserModUndoMessage.cs` — `INetMessage` types prefixed with `z` so their ordinal sorts after all vanilla types, keeping vanilla packet IDs stable.

3. **`refs/`** — local copies of the game's DLLs used only as build references. Never commit (`.gitignore`'d) and never redistribute.

`decompiled/` holds ILSpy output of the game assemblies for **reference only** when locating patch targets. It is not the source of truth and is not edited.

### When the game updates

Symptom is usually a missing method / changed signature in the Harmony target. Workflow: regenerate `decompiled/` from the new `sts2.dll`, diff against the previous decompile to find the renamed/moved member, update `Patches.cs`, then update **`src/EraserMod/SupportedVersion.cs`** (the single source for the supported game version — `GameVersion`, `GameCommit`, `GameDate`). Also update the version line at the top of `README.md` to match.

Multiplayer-specific check (only relevant after a game update touched `MegaCrit.Sts2.Core.Multiplayer.Serialization/`):

- `src/EraserMod/Net/z*Message.cs` types are deliberately prefixed with `z` so they sort *after* every vanilla `INetMessage` in `NetTypeCache` ordinal sort, which keeps vanilla packet IDs stable across MOD presence. If `MessageTypes.cs` switches sorting (e.g. `string.CompareOrdinal` → hash-based), the prefix trick stops working and protocol compatibility breaks. Re-verify `NetTypeCache.cs` sort logic when `decompiled/` is regenerated.
- Confirm no new vanilla `INetMessage` whose name starts with a character `> 'z'` (0x7A) was introduced — that would push our types into the middle of the vanilla list and shift IDs.

## Git workflow specifics

- Branches: `feature|fix|refactor|docs|chore/<issue#>-<topic>`; chores without an issue use `chore/skip-<topic>`.
- Direct commits to `main`/`master` are blocked by hook; override with `ALLOW_MASTER_COMMIT=1` only for maintenance.
- Commit messages must include `#<issue>`, or `[skip-issue]` for chores.
- Pre-commit hook runs both `dotnet build` commands above — fix build failures, don't bypass with `--no-verify`.
- Don't `git push` unless the user asks.
