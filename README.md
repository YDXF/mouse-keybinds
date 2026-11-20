# Mouse Keybinds

[中文说明](README.zh-CN.md) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829349177)

Adds **mouse side button** capture to Probably Stolen's **Storage Coordinator presets**, and fixes the intermittent scroll-to-rotate failure while dragging items.

> Settings → Key Bindings has supported mouse buttons natively since game 1.0.0.x, so this mod no longer touches that screen as of v1.3.0. It only fills the gap in Storage Coordinator presets, which still accept keyboard keys only.

![cover](art/cover.jpg)

## Features

- **Side buttons for coordinator presets** — Storage Coordinator container preset hotkeys can be bound to side buttons 1–4 (Unity `Mouse3`–`Mouse6`) and persist with the save file. Buttons 1–2 are the standard forward/back buttons; buttons 3–4 depend on the mouse exposing extra buttons as standard mouse buttons (see Compatibility).
- **Localized key names** — Key names follow the game language (zh / en / ru / es / fr / de / pl / ja), falling back to English.
- **Conflict warning in both places** — When a key you record in Settings → Key Bindings or in a coordinator preset is already used by another function (other actions / presets / hidden scene keys), a dialog lists every conflicting function and asks what to do: allow one key for multiple actions, or cancel (the coordinator restores the old key). Check "don't ask again" to bind silently; re-enable anytime in the mod settings.
- **Scroll-to-rotate fallback** — Hold `LeftAlt` while dragging and scroll to rotate, even for stacked items (steel sheets, coins, fuel cells) that normally change quantity on scroll. Also self-heals a stuck state flag that could disable scrolling entirely on older builds.
- **Cleaner mod panel** — Hides the native "Developer mode" toggle from the local mod detail page.

## Install

**Steam Workshop:** subscribe, enable in the in-game Mods (改装件) panel, restart the game.

**Manual:** copy the `mouse_keybinds` folder into `Probably Stolen/Mods/`, enable in-game, restart.

## Usage

1. Build or walk up to a **Storage Coordinator** and open its container screen.
2. Create (or select) a container preset and click its hotkey field.
3. Press a mouse side button (1–4) — the field shows the localized name.
4. Save. The binding persists with the save file.

For regular key rebinding, use the vanilla Settings → Key Bindings screen — it already supports mouse buttons natively. Key conflicts there still trigger this mod's confirm dialog.

## Configuration

In-game mod settings (stored at `ModConfigs/mouse_keybinds.json`):

| Key | Default | Description |
|---|---|---|
| `ScrollRotateModifier` | `LeftAlt` | Hold this key + scroll while dragging to force rotation. `None` restores vanilla behavior. |
| `ConflictPrompt` | `true` | Warn when a coordinator preset key conflicts with another function; turn off to allow conflicts silently. |
| `VerboseLog` | `false` | Log preset captures and drag diagnostics. |

## Compatibility

- **Game version:** 1.0.0.x (verified on the public 1.0.0.13 build and the 1.0.0.16 beta)
- Uses the game's built-in official ModLoader + Harmony. No declared capabilities, no save-format changes.
- Coexists with other keybind mods (e.g. *BackspaceClear* patches a different phase of the vanilla rebinding method).
- **Not supported:** left / right / middle mouse buttons, and anything beyond `Mouse6` (not visible to the engine). Note: many multi-button mice (e.g. Logitech G502 G6/G7/G8) consume their extra buttons in the driver and never send them to the OS — map such buttons to keyboard keys in your mouse driver.
- **Uninstalling:** side buttons bound to coordinator presets keep working after the mod is removed, but they can no longer be re-recorded as mouse buttons (the coordinator's own whitelist is keyboard-only). Overwrite them with any keyboard key in the preset editor.

## Build

Requires .NET SDK and the game's managed assemblies.

```bash
dotnet build src/MouseKeybinds.csproj -c Release -p:GameDir="<path to Probably Stolen>"
```

Deploy: `bash deploy.sh` (set `GAME_DIR` to override the default path).

## License

[MIT](LICENSE)
