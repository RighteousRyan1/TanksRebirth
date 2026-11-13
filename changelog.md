MESSAGE!!!!

Remember you can enable speedrun mode with `F1`! Top 5 uploaded speedruns will get their spot on the main menu!

# Additions

- Added Tanks Rebirth's first official MOD, "Original Campaign"
    - Has the same randomized missions/tanks as the original, even down to the RNG implementation
    - You may view the mod's repository and guide for installation [here](https://github.com/RighteousRyan1/TanksRebirth/wiki/Official-Mods)
- The level editor now remembers the last directory you saved to / loaded from

# Changes

- Allow for many smaller resolution scales, intended for fullscreen use
- Disallow egregious XP farming (thank you Ausyellow for reminding me)
- FFA mode now ends if all player tanks that spawned have died
- Slightly updated the look of the XP bar
- Largely updated the look of the loading mods / mods user interfaces
- Day cycle lighting mode now matches your real time
- Changed the Game Stats menu to look more like the other UI refreshes

# Changes Important for Modders

- No need for a `projrefs` folder anymore or a folder containing the game's assemblies.
    - The game now sets an environment variable on your system locating the last used Tanks Rebirth location
    - Also, the game creates a `TanksRebirth.targets` file within the `Mods/` folder
    - Now, just have the `.csproj` file of your mod contain the following:
```xml
<!-- You will need to launch the game once so it generates this file in order for it to work -->
<Import Project="..\TanksRebirth.targets" />
```
- Slight changes to mine behavior
- Various UI changes

# Fixes

- Fix some desync and animation issues in online multiplayer
- Various UI fixes
- Improve some performance for AI tanks

# TODO

- [ ] Real-time multiplayer stats
- [ ] Make all UI look cleaner. Could rework existing UI revamps as well

Happy tanking!