MESSAGE!!!!

Remember you can enable speedrun mode with `F1`! Top 5 uploaded speedruns will get their spot on the main menu!

# Additions

- Added "Faithful" lighting preset. It won't look proper with 'Room Shadows' on
- The game now saves your last used multiplayer username/IP/port
- You can now drag and drop mission/campaign/bin files onto the level editor to open them
- Added messages appearing above players' tanks when sent
- You can now press F10 to view frame data in the bottom right
- If dynamic lighting is enabled, lights now emit when tanks shoot

# Changes

- The tank AI system is now more accurate to the original (again)
    - Movement and shooting logic has been improved largely
    - I want to spend time next update perfectly fine-tuning the shooting logic (and hopefully optimizing it)
- Menu missions are now stored locally instead of fetched from the internet
- Pinging things in first person now casts a ping to the closest point in the direction faced
- Blocks breaking now have the color averaged instead of a preset color

# Changes Important for Modders

- Word 22 (MaxQueuedMovements) has been renamed to `MaxMoveHistory` to better fit its actual use
- New custom AI parameter: `SweepScanRadius`, read the XML docs

# Fixes

- Fixed bug where connecting clients into multiplayer saw extra dummy clients
- Shell physics should now no longer differ across framerates OR different clients
    - So with this, ghost shells should no longer exist, ever

# TODO

- [ ] Real-time multiplayer stats
- [ ] Make all UI look cleaner. Could rework existing UI revamps as well

Happy tanking!