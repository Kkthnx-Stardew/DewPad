# DewPad

Controller button hints for Stardew Valley's menus, and the game's own PlayStation, Switch and Xbox button art in its controller screens. It only draws. It never handles, blocks or sends an input, so it cannot change what a button does.

Status, `0.1.0-beta.1`, the first beta. Needs Stardew Valley 1.6.15 and SMAPI 4.2.1 or newer.

## What it does

- **Hints along the bottom of the game menu, chests and shops** while a controller is in use, in the game's own tooltip box. For example, in a chest, "A Select, B Close, LB Backpack, RB Chest, View Sort". Every hint was checked against the game's code. A hint only shows when its button does that in that menu, and Close and Sort only show when the menu can do them.
- **Your pad's buttons.** Choose Xbox, PlayStation, PlayStation with Circle to confirm, or Switch. The hints use the game's own glyphs.
- **The game's own controller screens in that style too.** The controls tutorial, the fishing and arcade help and the button icons. The game already has this art for all four layouts and only ever draws the Xbox one. DewPad shows the one you chose.

Keyboard and mouse players see nothing. The hints use the game's own controller mode, so they appear when the game switches to the controller and go when it switches back.

## Settings

Every setting is in the Generic Mod Config Menu if you have it, or in `config.json`.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | on | Turns DewPad off completely |
| `ShowPrompts` | on | Show the hints along the bottom of supported menus |
| `PromptStyle` | Auto | `Auto`, `Xbox`, `PlayStation`, `PlayStationCircleConfirm` or `Switch`. Auto shows Xbox, because a PC cannot tell a PlayStation or Switch pad from an Xbox one once a driver has translated it |
| `SwitchFaceLayout` | GameArt | Only for the Switch style. `GameArt` follows the game's own Switch art, where Check is the Switch A. `Position` keeps each button where it sits, so the bottom button shows as the Switch B |
| `PromptScale` | 1.0 | Size of the hints, 0.75 to 1.5 |
| `ReplaceGameIcons` | on | Show the chosen style in the game's controller screens. Turn it off if another mod changes those icons |

## Limits

- Only the game menu, chests and shops have hints so far. Other menus show none.
- The strip never covers a menu. On a screen about 800 pixels high, such as the Steam Deck's, the game menu has room for a compact strip, and the shop and the chest do not, so those show none. A screen 720 pixels high has room for none.
- The shoulder and trigger buttons are drawn as dark labelled pills, because the game's art has no glyph for them.
- Not tested yet, the settings page in Generic Mod Config Menu, split screen, a real PlayStation, Switch or Steam Deck pad, and anything but Windows. `docs/RESEARCH.md` has the full list.

## Compatibility

DewPad only reads menus and draws. It does not patch the game with Harmony and saves nothing, so removing it leaves the save as vanilla. It uses an asset edit for `LooseSprites/ControllerMaps`, and leaves that sheet alone if another mod has changed its size. Star Control and its fork both use the manifest ID `focustense.StarControl` and remap buttons. DewPad does not touch input, so the two do not compete.

## For other mods

`IModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad")` gives a way to draw the hints, and where and how many are showing. The interface is in `DewPad/DewPadApi.cs`.

## Translations

Text lives in `DewPad/i18n/default.json`. A translation is a copy of that file named for the language, such as `fr.json`.

## Building and testing

```bash
dotnet build DewPad.slnx -c Release
dotnet test DewPad.Tests/DewPad.Tests.csproj
```

The build does not copy into the game's Mods folder. The zip lands in `DewPad/bin/Release/net6.0/`.

A real test runs inside the game with `tools/Run-Harness.ps1`, for example `-Scenario menus -WithDewPad -ConfigJson '{"PromptStyle":"Switch"}'`. It uses a throwaway farm and its own small Mods folder, and draws each menu with the hints into pictures. Your Mods folder and saves are never touched.

## Credits

All button art is from Stardew Valley's own controller sheet. Nothing from another project is bundled.
