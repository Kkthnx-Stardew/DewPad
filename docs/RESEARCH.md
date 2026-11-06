# Research notes

DewPad for Stardew Valley 1.6.15 (build 24356) and SMAPI 4.5.2. Everything in the first table was read from the decompiled 1.6.15 game code or measured in the running game, not taken from the blueprint or from forum posts.

## What the game does

| Question | Answer |
|---|---|
| Does the game draw button hints in its menus | No. Nothing in 1.6.15 draws a key help line in a menu. A hint strip is a new thing |
| How does the game decide a controller is in use | `Game1.CheckGamepadMode` sets `Options.gamepadControls`. In Auto mode a button, stick or trigger turns it on and mouse, keyboard or a disconnect turns it off, with Force On and Force Off as the other two `Options.GamepadModes`. DewPad reads that flag and keeps no input state of its own, so it can never disagree with the game |
| Which button changes tabs in the game menu | The triggers. `GameMenu.receiveGamePadButton` handles `RightTrigger` and `LeftTrigger`. The bumpers do nothing there |
| Which buttons close a menu | B and Start, through `Game1` when `activeClickableMenu.readyToClose()` is true, so not while an item is held |
| What do the bumpers do in a chest | `ItemGrabMenu.receiveGamePadButton`. Right bumper moves to the chest side, left bumper to the backpack, and the left one does nothing in the shipping bin |
| What does Back (View) do in a menu | Sorts the backpack when the menu has a sort button. `InventoryPage` and `ItemGrabMenu` both handle `Buttons.Back` that way. Outside a menu Back opens the journal |
| What do the triggers do in a shop | `ShopMenu.receiveGamePadButton` moves between the shop list and the backpack |
| Does the game ship PlayStation and Switch art | Yes. `LooseSprites/ControllerMaps` is 841 by 1152 pixels and holds four bands of 288 rows, one each for Xbox, PlayStation, Switch and PlayStation with Circle to confirm. Each band has the controls tutorial, the fishing and arcade help, and a row of six button glyphs. The game only ever draws the first band, because `Utility.controllerMapSourceRect` returns its argument unchanged |
| Where is the glyph row | Six glyphs of 28 by 28 pixels starting at x 513, y 260 in the first band, which is where the emote menu and the local co-op join menu draw from. They read X, A, B, Y, Back, Start for Xbox and PlayStation, and Y, B, A, X, minus, plus for Switch |
| Which Switch button is Check | The game's Switch art labels Check as the Switch A, Cancel as B, Use Tool as Y and Crafting as X |
| What do the triggers do in a bundle | `JunimoNoteMenu.receiveGamePadButton`. Inside a bundle the right trigger jumps to the first empty ingredient slot and the left trigger back to the backpack. In the room overview opened from the game menu they swap the room page. B does not close a bundle page, because `readyToClose` is false there, so the Close hint is hidden by the same rule |
| What do the triggers do in Robin's menu | `CarpenterMenu.receiveGamePadButton`. On the building list, not the farm view, they move to the previous and next building |
| Why is the journal left out | `QuestLog` handles the triggers for its pages, but `pages`, `currentPage` and `questPage` are protected, so a correct hint would need reflection |
| How does the game deliver controller buttons to a menu | `Game1` calls `receiveGamePadButton` on the top-most menu whenever `gamepadControls` is on and the chat box is not active, and the generic A and X clicks apply to every menu that does not override `areGamePadControlsImplemented`, which none of these do. Neither needs "controller-style menus", so the hints hold with it off |
| Where does a pad's name come from | The game ships SDL2 with MonoGame's SDL backend, where `GamePadCapabilities.DisplayName` is the SDL controller name. A pad that Steam Input presents as an Xbox pad is named like one |
| Which controller mods exist | Star Control and its fork both use the manifest ID `focustense.StarControl`. They draw radial menus and remap buttons. DewPad does not touch input, so there is nothing to conflict over |
| Controller mods in your game | None of the installed mods are controller mods |

## Decisions

- **DewPad only draws.** It never handles, blocks or sends an input. That is what keeps it safe next to every other mod, and it means it cannot cause a lost item, a wrong purchase or a stuck button.
- **The game's own art.** The icon change copies one band of the game's sheet over the band it draws, with SMAPI's asset edit. It needs no new art and no licence. The hint strip draws the game's own glyphs for the face, Back and Start buttons. The sheet has no glyphs for the bumpers and triggers, so those are drawn as dark pill shapes with a label in the game's font.
- **Auto means Xbox.** A PC cannot tell a PlayStation or Switch pad from an Xbox one once a driver or Steam Input has translated it, so the style is a setting.
- **Hints only for menus that were read.** The game menu, chests, shops, bundles and Robin's building list, and only the game's own classes. A menu another mod derives from them shows nothing, because it may handle the buttons differently. So does any menu with another menu, a dialogue, text entry or an active chat box on top.
- **Auto follows the pad's name.** PlayStation or Switch when the name says so, Xbox for everything else, including a pad that Steam Input presents as an Xbox pad. It reads the name when a menu opens and changes the game's sheet if the answer changed. The matching is written from SDL's documented names and has not met real hardware.
- **A hint is listed only if it works.** Close only shows when the menu can close. Sort only shows when the menu has the sort button. The bumper jumps only show for an ordinary chest, never the shipping bin or a fishing treasure.
- **Never over a menu.** The strip sits under the menu's real bottom edge, which includes the backpack grid and the buttons that hang below the menu box. It uses a tighter layout when the room is small, and is not drawn at all when there is no clear room.

## Checked in the running game

Run with `tools/Run-Harness.ps1` on a throwaway farm. The live Mods folder and your saves are never used.

- The game menu, a chest and a shop show the right hints in the Xbox, PlayStation and Switch styles, drawn in the order the game draws them.
- A menu DewPad does not describe shows nothing, and so does the game menu with the keyboard and mouse in use.
- With the PlayStation style, the first band of the game's sheet is identical to the PlayStation band, so the game draws the PlayStation art, and the controls tutorial shows the cross.
- At a 2560 by 1440 UI, 1707 by 960 and 1423 by 800 the strip is clear of the game menu, by 302, 62 and 9 pixels. At 1423 by 800 the shop and the chest fill the screen, so the strip is not drawn for them. At 1280 by 720 nothing has room.

- Drawing the hints costs 16.5 microseconds a frame and allocates 0.01 bytes a frame, in a batch like the game's. The layout is built once, in 0.29 ms. With the keyboard in use the call costs 13 nanoseconds. The game menu's own draw takes 71 microseconds, for scale.
- Opening and closing the game menu 300 times with the hints drawn changed managed memory by 12 KB, which is noise, so nothing is kept.
- Changing the style while the game runs, the way saving the settings page does, put the PlayStation, Switch and Circle to confirm bands in the game's sheet, each 100% identical to its band, with no restart and without disposing the texture.
- The bundle screens and Robin's building list show their hints in the PlayStation style, clear of the menu at three UI sizes.

## Not checked

The Generic Mod Config Menu page, split screen, a real PlayStation, Switch or Steam Deck pad (so the pad name matching has not met real names), other mods that draw in the same place, and any platform other than Windows. The glyph for Back on the PlayStation layout is the game's touchpad glyph, and whether a Steam Input setup sends the touchpad click as Back is not known.
