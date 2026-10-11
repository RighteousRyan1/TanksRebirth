Holy crap, what an update! Lots of great new things!

Remember you can enable speedrun mode with `F1`! Top 5 uploaded speedruns will get their spot on the main menu!

# Additions

- Added local multiplayer support! It's rather rudimentary, so here's the parameters, *for now*:
	- Either all players must use game pads, or one player uses a keyboard and the rest game pads (PlayStation/Xbox controllers)
	- Does not work in multiplayer contexts (e.g: two local players cannot play with an online player) (yet?)
- Added Wii remote support, check the [guide](https://github.com/RighteousRyan1/TanksRebirth/wiki/Wii-Remote-Support) on how to use one
	- Also allows the use of a nunchuk
	- Not the most stable, so please report Wiimote bugs in my server
- Made the game take advantage of multiple threads! If your CPU has multiple threads (hopefully it does), the game should now run better under higher workloads, and just overall better
- Added slight randomized pitch shifting in many game-related sound effects
- Way more localization where previously missing
- Added a ton of new options in the settings menu
- Added a new mods menu!
	- It appears in the top right of the main menu
	- It allows you to quickly load/unload/reload mods instead of having to use debug functions or restart the game
- Added a new dev console which is accessible from the tilde key (~)
- Player bullet count UI is now shown for all players (including in online contexts)
- Even more localization!
	- Translators are still welcome to correct any faulty translations- some translations were done via machine translation
- Added a new animation/text popup when a mission attempt is over
- Added an entirely new OPTIONAL lighting system that can be accessed via the graphics settings
	- Shadows, lights, and time of day all are settings you can change
	- You can simply choose to not enable this if you either don't want it on or your computer cannot handle it
- Added a second hand to the clock (thank you BigKitty)
- Added heaps more graphics settings for regular gameplay, including Anti-Aliasing, FPS limit, and more!
- New setting which allows the display of team colors on tanks
	- I'd suggest only using this in non-standard campaigns (e.g: campaigns where it's only players vs ai tanks, 2 teams)

# Changes

- The level editor has gotten a number of various improvements
- Missions and campaigns are now stored in a smaller (and better) format, gzipped JSON
	- Around 40-50% smaller than before
	- No forwards compatibility. Don't go back to older versions and load these files, they won't work
- Debug tools are now locked behind debug builds of the game
- Increased the maximum tank count to 60
- Made mines not destroy everything instantly, but rather grow, similar to the original
- "Difficulties" now changed to "Modifiers"
- Severely overhauled the chat box
- MASSIVE revamps to the game stats menu, multiplayer menu, and the chat window
- Hit/hurtboxes now occur on 3 dimensions instead of previously 2, to allow for modders to do potentially cool things, and also makes debugging much easier
- Bullet count UI for players has been slightly revamped
- The "Shotguns" modifier has been improved and changed
	- Tanks now have recoil when firing
	- Shot spread is much tighter
- Pings now grow and shrink quicker
- Rocket trails are now MUCH better looking
- Completely overhauled the Settings UI (which will be the standard for future UI updates!)
	- I caved into using AI to assist me with the design and layout, since UI frontend is not my strong suit in the slightest
- Completely overhauled the Modifiers UI!
- Lantern mode will now look entirely different if using the new lighting system, read the description
- Bindings in the Controls menu can now use mouse buttons!

# Changes Important for Modders

- Don't create, modify, or generate textures in AI-related tank logic code now! It will throw an exception as it won't be the main thread
	- You can enqueue to `TankGame.MainThreadTasks` for actions to be performed on the main thread
- There is now a much higher limit on pitch-shifting (of specifically `OggAudio`. Change `OggAudio.Pitch` directly)
- Added `mod_info.json` to the mod template. If your mod does not have it, you can download the default `mod_info.json` [here](link_lol_PLEASE_ADD_ME)
- `Difficulties` is now `Modifiers` in source
	- `Types` now renamed to just `Map`
	- Each default modifier identifier now has a constant string associated with it, which makes things much less of a headache
		- The constants have simplified names for lesser code cram. The names should be good enough to decipher 
- `MathUtils.Rotate` -> `MathUtils.RotatedBy` because the new MonoGame version has `Vector2.Rotate` declared
- `Campaign.TrackedSpawnPoints` is now static instead of instanced, and is now `CurrentTrackedSpawns`
- Every `Crate` field has been properly named for C# naming conventions
- Most rendering/drawing info has been moved into a struct called `DrawParamsBasic`, where the field name for it is `DrawParams`
	- Entity-specific draw params are named `DrawParamsX` (i.e: `DrawParamsTank`)
- `SwapXTexture` from entity classes is now just `DrawParamsX.XTexture = newTexture;`
- Keyframes now are dimension-agnostic
- `CameraGlobals.IsUsingFirstPresonCamera` has been de-typo'd :|
- Animators have been made less archaic- instead of the final frame not needing a duration or easing function, it is now the first one
- Added a really neat camera animation system to the freecam debug menu
	- You can see controls on the left
- `LocalizedString` is now indexed directly instead of through the method
- Added `Description` to `ModBlock` and `ModTank` for use in the level editor
- Level and campaign files are now gzipped JSON (format version 7) instead of binary
	- All reading and writing goes through `LevelFiles`. `Mission.Read(Stream)` reads any format, binary or JSON
	- `Mission.WriteToStream` was removed. Use `Mission.Save` or `LevelFiles.WriteMission`
	- Tank, team, player and block types are saved by name, not by number. Modded tanks are saved under their English name, so changing your tank's name will break levels that use it
	- If a level uses a tank or block from a mod that isn't loaded, it logs a warning and uses a default instead of crashing
	- Binary versions 1 to 6 still load
- Added `ModModifier`, which handles everything automatically, including the UI element and net sync
- Some `Shell` and `Tank` properties are now bit flags
- Tanks are now drawn with a custom shader instead of `BaiscEffect`, so if you were modifying anything related to that- it will no longer work
- `PlacementSquare` -> `EditorTile`
- `TankGame.PostDrawEverything` -> `TankGame.PreDrawBackBuffer`

# Fixes

- Fixed nametags showing for non-host clients after the game has started if the host started the game with the client being in the multiplayer menu
- Fixed smokes generated from smoke grenades being the wrong color
- Mods should load 2x quicker on average, depending on content
- Fixed some tank destruction particles having frame-dependent animation
- Fixed a bug where you would Game Over with 2 lives in single player and in some multiplayer contexts
- Fixed clock pendulum not accurately tracking seconds (there was some margin of error before)
- The mission intro sequence/fanfare is now controlled by the sound effects slider
- Fixed the room scene not being updated while in pause
- Places in the room where glass would before not render properly now renders
- Performance has been improved in various places of the game (the level editor should be much more performant even with the new changes)
- Particles are now infinitely more performant than they were before, allowing for tens to hundreds of thousands of particles to be present before noticeable framerate loss
- Tank treads now look much more proper, and there is not a performance loss to have 'Fading Tank Tracks' disabled
- Massively optimized the level editor, should run much better on all systems
- Fixed a bug where fast enough bullets could skip all ricochets when colliding with the outer border
- Fixed a lot of strange graphical bugs relating to christmas mode

# TODO

- [ ] Real-time multiplayer stats

# Note for Everyone

Since I value transparency, I want to be upfront: I’ve recently started using AI to help program small pieces of Tanks Rebirth.

I’ve used it where I struggled the most- lighting and the UI rework. 
I'm still squishing bugs, adding new features, and designing the game myself.

I review, test and rework everything I keep, and I decide what the game is and how it plays, and I remain knowledgeable of the codebase.
The game design, direction and the vast majority of the codebase are still mine, since that’s the part I love doing.

Thanks for reading, and I hope you enjoy the update!

*PLEASE*, if you find anything wrong, go to my [discord](https://discord.gg/KhfzvbrrKx) server and report bugs in the #bugs 
channel, or, if you want to suggest something, suggest them in the #suggestions channel.

Happy tanking!
