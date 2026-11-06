# DewPad

Controller button hints for Stardew Valley's menus, and the game's own PlayStation, Switch and Xbox button art in its controller screens. It only draws. It never handles, blocks or sends an input, so it cannot change what a button does.

Status, `0.1.0-beta.5`, a beta. Needs Stardew Valley 1.6.15 and SMAPI 4.2.1 or newer.

## What it does

- **Hints along the bottom of the game menu, chests, shops, the Community Center bundles, Robin's building list and the journal** while a controller is in use, in the game's own tooltip box. For example, in a chest, "A Select, B Close, LB Backpack, RB Chest, View Sort". Every hint was checked against the game's code. A hint only shows when its button does that in that menu, and Close and Sort only show when the menu can do them.
- **Hints that follow what is highlighted.** In the backpack page of the game menu, A says "Pick up" on an item. In a chest, A says "Take" on a chest item and "Put in chest" on a backpack item. In a shop it says "Buy" on an item for sale and "Sell stack" on a backpack item the shop will take, which the game sells at once with no confirmation. On anything else it says "Select". While you hold an item, A says "Place" on a backpack slot and "Put in chest" on the chest side, and Close drops out, because a chest will not close with an item in your hand. X, a hidden controller feature, splits ONE item off a stack where A moves the whole stack, so it shows as "Take one", "Put one in chest" or "Pick up one", and only for a stack of more than one that can be split, never for a tool, a single item or with a full hand. The game already shows the sell price itself in its own hover tooltip, so DewPad does not repeat it. Each one was read from the game's code and checked in the game, including that an item the shop does not buy, and a tool, stay "Select".
- **Your pad's buttons.** Choose Xbox, PlayStation, PlayStation with Circle to confirm, Switch, or Steam Deck. The hints use the game's own glyphs, and the shoulder and trigger buttons are labelled the way your pad labels them, such as L1 and L2 on a PlayStation pad and on a Steam Deck. Auto picks for you where the pad says what it is.
- **The game's own controller screens in that style too.** The controls tutorial, the fishing and arcade help and the button icons. The game already has this art for all four layouts and only ever draws the Xbox one. DewPad shows the one you chose.

Keyboard and mouse players see nothing. The hints use the game's own controller mode, so they appear when the game switches to the controller and go when it switches back.

## Settings

Every setting is in the Generic Mod Config Menu if you have it, or in `config.json`.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | on | Turns DewPad off completely |
| `ShowPrompts` | on | Show the hints along the bottom of supported menus |
| `PromptStyle` | Auto | `Auto`, `Xbox`, `PlayStation`, `PlayStationCircleConfirm`, `Switch` or `SteamDeck`. Auto shows PlayStation or Switch when your pad's name says so, the Steam Deck style on a Steam Deck, and Xbox otherwise. A pad that Steam Input or a driver presents as an Xbox pad shows as Xbox, so pick the layout yourself in that case |
| `SwitchFaceLayout` | GameArt | Only for the Switch style. `GameArt` follows the game's own Switch art, where Check is the Switch A. `Position` keeps each button where it sits, so the bottom button shows as the Switch B |
| `PromptScale` | 1.0 | Size of the hints, 0.75 to 1.5 |
| `ReplaceGameIcons` | on | Show the chosen style in the game's controller screens. Turn it off if another mod changes those icons |

## Limits

- Only the game menu, chests, shops, the bundle screens, Robin's building list and the journal have hints so far. Other menus show none, and so does any menu another mod has derived from those, because it may handle the buttons differently. Inside a quest in the journal there are none, because B may go back or close the whole journal and the code does not make clear which.
- The strip never covers a menu. On a screen about 800 pixels high, such as the Steam Deck's, the game menu has room for a compact strip, and the shop and the chest do not, so those show none. A screen 720 pixels high has room for none.
- The shoulder and trigger buttons are drawn as dark labelled pills, because the game's art has no glyph for them.
- Not tested yet, the settings page in Generic Mod Config Menu, split screen, a real PlayStation, Switch or Steam Deck pad (the pad name matching is written from SDL's documented names and has not met real hardware, and the `dewpad` console command shows what name was read), and anything but Windows. `docs/RESEARCH.md` has the full list.

## Player complaints

`docs/COMPLAINTS.md` lists what players say is wrong with the controller, checked against the game's code, with what DewPad does about each and what is left to a different kind of mod. One of the oldest, a chest erasing an item when the backpack is full, is already fixed in 1.6.15.

## Cost

Measured in the game on this machine, drawing the hints takes about 17 microseconds a frame, allocates effectively nothing because the layout is kept, and costs 13 nanoseconds a frame for a keyboard player. Opening and closing menus 300 times changed memory by 12 KB, which is noise. `docs/RESEARCH.md` has the numbers.

## How it differs from the other controller mods

Star Control and its fork add pie menus, quick actions and button remapping, need StardewUI and ask for a screen of at least 1080p. Controller Icons Pack swaps the game's own icons. DewPad does neither of those things and works next to them. It only draws, it needs nothing else installed, and it does the part none of them do, telling you what each button will do in the menu you are in, with your pad's own buttons, down to Steam Deck sized screens.

## Compatibility

DewPad only reads menus and draws. It does not patch the game with Harmony and saves nothing, so removing it leaves the save as vanilla. It uses an asset edit for `LooseSprites/ControllerMaps`, and leaves that sheet alone if another mod has changed its size. Star Control and its fork both use the manifest ID `focustense.StarControl` and remap buttons. DewPad does not touch input, so the two do not compete.

## For other mods

`IModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad")` is in `DewPad/DewPadApi.cs`. The call that matters is `RegisterMenu`, which gives a menu of your mod the same strip, glyphs, placement and style handling as the game's own menus:

```csharp
api.RegisterMenu(typeof(MyMenu).FullName, new[] { "A=Select", "LeftTrigger,RightTrigger=Switch page", "B=Back" });
```

Each hint is a button name, or two joined by a comma, an equals sign and the words to show. The button names are A, B, X, Y, LeftBumper, RightBumper, LeftTrigger, RightTrigger, Start and Back. Up to 8 hints, no button twice, 40 characters of text each, and a bad one is skipped with a warning in the log that names your mod. The words are shown as given, so translate them first. DewPad only draws the hint and cannot check it, so register a button only when it really does that in your menu. The registration is for the exact class, so a derived class needs its own call.

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
