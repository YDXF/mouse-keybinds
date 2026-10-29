# Mouse Keybinds

[中文说明](README.zh-CN.md) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3829349177)

Bind **mouse side buttons** in Probably Stolen's vanilla key rebinding UI — and fix the intermittent scroll-to-rotate failure while dragging items.

![cover](art/cover.jpg)

## Features

- **Side button capture** — side buttons 1–4 (Unity `Mouse3`–`Mouse6`) can be bound to any of the 22 rebindable actions in Settings → Key Bindings, and persist with the save file.
- **Localized key names** — Key names follow the game language (zh / en / ru / es / fr / de / pl / ja), falling back to English.
- **Scroll-to-rotate fallback** — Hold `LeftAlt` while dragging and scroll to rotate, even for stacked items (steel sheets, coins, fuel cells) that normally change quantity on scroll. Also self-heals a stuck state flag that can disable scrolling entirely.
- **Cleaner mod panel** — Hides the native "Developer mode" toggle from the local mod detail page.

## Install

**Steam Workshop:** subscribe, enable in the in-game Mods (改装件) panel, restart the game.

**Manual:** copy the `mouse_keybinds` folder into `Probably Stolen/Mods/`, enable in-game, restart.

## Usage

1. Open **Settings → Key Bindings**.
2. Click any binding, then press a mouse side button (1–4) — the button shows the localized name.
3. Play. Bindings are stored in the save file (`ES3`) and survive restarts.

## Configuration

In-game mod settings (stored at `ModConfigs/mouse_keybinds.json`):

| Key | Default | Description |
|---|---|---|
| `ScrollRotateModifier` | `LeftAlt` | Hold this key + scroll while dragging to force rotation. `None` restores vanilla behavior. |
| `VerboseLog` | `false` | Log side-button captures and drag diagnostics. |

## Compatibility

- **Game version:** 0.46D (1.0)
- Uses the game's built-in official ModLoader + Harmony. No reflection, no extra capabilities, no save-format changes.
- Coexists with other keybind mods (e.g. *BackspaceClear* patches a different phase of the same method).
- **Not supported:** left / right / middle mouse buttons, and buttons beyond side button 4 — map extra buttons to keyboard keys in your mouse driver.
- **About "Mouse 2":** the game binds *Flip Item* to the middle mouse button by default, so that entry reads `Mouse 2` out of the box. `Mouse 2` is the middle click — not side button 2. Side buttons use the 1–4 naming above (`Mouse3`–`Mouse6`).

## Build

Requires .NET SDK and the game's managed assemblies.

```bash
dotnet build src/MouseKeybinds.csproj -c Release -p:GameDir="<path to Probably Stolen>"
```

Deploy: `bash deploy.sh` (set `GAME_DIR` to override the default path).

## License

[MIT](LICENSE)
