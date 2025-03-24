## What does it do
You press a button (F by default), nearby valuable items are discovered (and shown on screen). Already discovered items are shown a second time.

If Multiplayer RPC is enabled, items discovered for the first time with the scanner will appear on both your minimap and your friends', should you be playing multiplayer.

Also capable of discovering enemies, dead teammates and equippable items, although they will not appear on your teammates minimap, even if you enable Multiplayer RPC. All of this can be disabled (or enabled) by demand.

Has a cooldown of 10 seconds, tracked on the top of the screen. Said bar shouldn't appear by itself unless scanner is used.

## Configuring
Use your modinstaller, BepInEx's config or [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)

You can change:
- Keybind for scanning (f by default)
- Scan radius (10 by default)
- Cooldown (10 by default)
- Multiplayer RPC discovery (enabled by default)
- GUI (enabled by default)
- Valuable items discovery (enabled by default)
- Enemies discovery (enabled by default)
- Dead teammate's heads discovery (enabled by default)
- Equippable items discovery (enabled by default)

## Installation
0. Make sure you have BepInEx installed.
1. Download the REPO Scanner Mod package.
2. Extract the contents to the game's `BepInEx/plugins` directory.

Or use [Gale](https://thunderstore.io/c/repo/p/Kesomannen/GaleModManager/)/[r2modman](https://thunderstore.io/c/repo/p/ebkr/r2modman/).

## Why scan GUI ugly and not in the style of the game
Because I can't figure out how to make it render properly.

## How it looks
<details>
  <summary>Click to expand</summary>
  <img src="https://i.imgur.com/5St7tSd.png" alt="GUI"/>
  <img src="https://i.imgur.com/f27bBng.png" alt="Overview"/>
  <img src="https://i.imgur.com/zSZ1kwP.png" alt="Enemies"/>
  <img src="https://i.imgur.com/Od92yHa.png" alt="Items"/>
</details>

## Known issues
- Enemy dimensions are currently broken, but their location is still accurately tracked.
- Cart is considered an equippable item. Twice. So the discovery's frame for it is very... opaque.
- Game freaks out when the scan size is too big. Nothing I can do, make it smaller in config by yourself.
- Special mouse buttons, gamepad and special obscure keys cannot be used for keybinding. Will probably rework how keybinds work in the future.