## What does it do
You press a button (F by default), nearby valuable items & cosmetic boxes are discovered (and shown on screen). Already discovered items are shown a second time. There is an option to only scan already discovered valuables (disabled by default).

If Multiplayer RPC is enabled, items discovered for the first time with the scanner will appear on both your minimap and your friends', should you be playing multiplayer.

Also capable of discovering enemies, dead teammates and equippable items, although they will not appear on your teammates minimap, even if you enable Multiplayer RPC. All of this can be disabled (or enabled) by demand.

Has a cooldown of 10 seconds, tracked on the top of the screen. Said bar shouldn't appear by itself unless scanner is used.

You can switch between a new GUI version (the one where all screen effects are applied) and a legacy one (where it's as clear as possible). There's two versions because I liked the clear one and my friend liked the immersive one.

I believe that at this point the development of this mod is complete. [Still feel free to contribute, report issues or add suggestions](https://github.com/Kistras/REPO-Scanner).

## Configuring
Use your modinstaller, BepInEx's config or [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)

You can change:
- Keybind for scanning (f by default)
- Scan radius (10 by default)
- Cooldown (10 by default)
- Multiplayer RPC discovery (enabled by default)
- GUI (enabled by default)
- GUI version (new by default)
- Valuable items discovery (enabled by default)
- Only scan already discovered items (disabled by default)
- Enemies discovery (enabled by default)
- Dead teammate's heads discovery (enabled by default)
- Equippable items discovery (enabled by default)
- Cosmetic boxes discovery (enabled by default)
There are overrides for discover radius:
- Override radius for valuables (0 to use main radius, 0 by default)
- Override radius for enemies (0 to use main radius, 0 by default)
- Override radius for dead teammates (0 to use main radius, 0 by default)
- Override radius for equippable items (0 to use main radius, 0 by default)
- Override radius for cosmetic boxes (0 to use main radius, 0 by default)

## Installation
0. Make sure you have BepInEx installed.
1. Download the REPO Scanner Mod package.
2. Extract the contents to the game's `BepInEx/plugins` directory.

Or use [Gale](https://thunderstore.io/c/repo/p/Kesomannen/GaleModManager/)/[r2modman](https://thunderstore.io/c/repo/p/ebkr/r2modman/).

## How it looks ingame
<details>
  <summary>Click to expand</summary>
  <h3>New version of GUI</h3>
  <img src="https://i.imgur.com/ZuS2z17.png" alt="New GUI"/>
  <h3>Legacy version of GUI</h3>
  <img src="https://i.imgur.com/5St7tSd.png" alt="Old GUI"/>
  <h3>Feature overview</h3>
  <img src="https://i.imgur.com/f27bBng.png" alt="Overview"/>
  <h3>Enemies</h3>
  <img src="https://i.imgur.com/zSZ1kwP.png" alt="Enemies"/>
  <h3>Items</h3>
  <img src="https://i.imgur.com/Od92yHa.png" alt="Items"/>
</details>

## Known issues
- Enemy dimensions are currently broken, but their location is still accurately tracked.
- Game freaks out when the scan size is too big. Nothing I can do, make it smaller in config by yourself.
- Special mouse buttons, gamepad and special obscure keys cannot be used for keybinding. Will probably rework how keybinds work in the future.
- When scanning for cosmetic boxes they will blip for each box each time. It's more bearable with regular valuables, but may get really annoying later on.