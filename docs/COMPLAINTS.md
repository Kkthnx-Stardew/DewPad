# Player complaints about the controller, checked

Complaints gathered from the Stardew Valley forums and Steam discussions, each checked against the 1.6.15 game code. A report is a lead and not a regression test, so a verdict only says what the code shows. Many of the threads are older, and some are about Android, iOS, Mac or Switch, which DewPad cannot affect.

| Complaint | Where it came from | Verdict from the 1.6.15 code | What DewPad does |
|---|---|---|---|
| With a full backpack, cancelling a take from a chest closes the chest and erases the item | A PC and Switch player, in a general controller thread | **Already fixed.** `MenuWithInventory.RescueHeldItemOnExit` puts a held item in the backpack, and drops what does not fit in front of the player, and `readyToClose` is false while an item is held unless the menu allows it, so a chest stays open. Nothing is erased | The confusing part is real, the chest will not close and nothing says why. While an item is held, A now reads "Put in chest" on the chest side and "Place" on a backpack slot, and the Close hint is gone, which shows the item has to be put down first |
| The bumper jumps to the sort button in a chest but not in the backpack page | A PC player, [improvement of gamepad and mouse controls](https://forums.stardewvalley.net/threads/improvement-of-gamepad-and-mouse-controls.49142) | **Confirmed.** `InventoryPage.receiveGamePadButton` only handles Back, which sorts. `ItemGrabMenu` handles both bumpers | The strip names the real sort button, "View Sort", and the chest's "LB Backpack" and "RB Chest". Making the bumper jump would change an input, which DewPad does not do |
| Hotbar slots use the bumpers and menus use the triggers, which is the other way round from other games | The same player | **Confirmed as designed.** `GameMenu` changes tabs with the triggers | The hints show which button does what in each menu |
| Connecting a gamepad on PC does not change the control prompts to gamepad icons | The same player | The game draws no menu prompts at all, only its controller tutorial screens | DewPad's hints follow the game's own controller mode, so they appear when the game switches to the controller and go when it switches back |
| The inventory cursor does not wrap from the first slot to the last | The same player | Not checked, it is cursor navigation | Not touched. A candidate for an opt-in controls mod |
| Moving items between the inventory and a chest sends the cursor back to the other side after each click | A PC player, [Stardew Valley](https://steamcommunity.com/app/413150/discussions/0/352788917746075828) | Not checked, it is cursor navigation | Not touched. The strip at least names the action at each spot, "Take" and "Put in chest", and X moves one item at a time |
| Selling a stack in a shop happens at once with no confirmation | A player asking for confirmation boxes | **Confirmed.** `ShopMenu.receiveLeftClick` sells the whole stack on A | The A hint says "Sell stack" on an item the shop will take, so it is no surprise |
| The game ignores an external controller on a Steam Deck, or a Steam Input remap | Several, for example [steam deck controller switching issues](https://forums.stardewvalley.net/threads/steam-deck-controller-switching-issues.14493) | Steam and device setup, not game code | Not ours. Gamepad Slot Fix covers phantom devices and player slots |
| Remapping a gamepad on PC | The same PC player | The game has no gamepad remap | Not ours. Star Control remaps buttons |
| The cursor or the hotbar cycles by itself on a PlayStation or Mac pad, or the cursor bounces at the bottom of menus on a Mac | Several, one on [a DualShock 4 on 1.6.15](https://forums.stardewvalley.net/threads/dualshock-4-controller-issues.50284) | One report each, no reproduction, and mostly Mac or Linux | Not touched. Nothing here can be checked on this PC |
| An invisible cursor in the inventory, and a button deleting an item | An iOS report | Android and iOS, not the PC game | Not applicable |

## What this changed in DewPad

- Hints for a held item, "Place" and "Put in chest", with the Close hint dropping out while an item is held.
- The journal now has hints.
- The cursor and wrap complaints, and the bumper jump in the backpack page, are all input changes. They belong in a separate opt-in mod, so DewPad keeps its promise that it only draws and cannot cause a wrong sale or a stuck button.
