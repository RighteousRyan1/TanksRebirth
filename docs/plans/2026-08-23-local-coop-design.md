# Tanks Rebirth Local Co-op Design

**Date:** 2026-08-23
**Base version:** `1.8.1.1-alpha` (`feaf21a`)
**Status:** Approved

## Goal

Add a two-player, same-Mac campaign mode to Tanks Rebirth. Corin controls Player 1 with keyboard and mouse. Andrew controls Player 2 with a second keyboard using a disjoint key layout. Both players share the existing arena camera.

The deliverable is a separate `Tanks Rebirth Local Co-op.app`, preserving the original downloaded game as a fallback.

## Constraints and Existing Behavior

- macOS combines all attached keyboards into one logical keyboard. Local co-op therefore separates players by key assignment rather than by physical keyboard identity.
- The fixed arena camera already shows the entire playfield, so split-screen is unnecessary.
- The Vanilla campaign contains Blue, Red, Green, and Yellow player spawn templates in every mission. The local two-player mode will use Blue and Red and suppress Green and Yellow.
- Offline player tanks currently share global keyboard, mouse, and controller snapshots. This must be replaced with explicit local-player input ownership.
- Online multiplayer must continue to use the existing client-matching and synchronization path.

## Player Experience

The main play flow gains a **Local Co-op** choice before campaign selection. Selecting it configures two local players and then uses the existing campaign picker and mission flow.

### Default Controls

| Action | P1 Blue - Corin | P2 Red - Andrew |
| --- | --- | --- |
| Move | W/A/S/D | I/J/K/L |
| Aim | Mouse | Arrow keys |
| Fire | Left mouse button | Return |
| Place mine | Space | Right Shift |
| Menus and pause | P1 | Disabled for P2 |

P2 aiming is digital and independent. Pressing an aim key changes the turret direction; releasing the key retains the last direction. Diagonal combinations produce diagonal aim. P2 input never moves or recenters P1's mouse cursor.

The initial release uses fixed P2 defaults. The input architecture will keep bindings data-driven enough for a later remapping UI without making remapping part of this first delivery.

## Campaign and Life Rules

- Local co-op spawns only P1 Blue and P2 Red.
- Each player has a separate life count and kill count.
- A mission continues while at least one local player is alive.
- If one player is destroyed, the surviving player may complete the mission.
- On the next mission, each player respawns if that player still has lives remaining.
- Bonus-life missions award one life to both local players.
- The HUD displays both player colors, labels, and lives clearly.
- Single-player retains its current behavior.

## Architecture

### Local Session State

Introduce a small local-session component that records the active offline mode and local player count. The supported initial states are single-player and two-player local co-op. Network-connected sessions bypass this component for control ownership.

### Per-Player Input

Poll the physical keyboard and mouse once per frame, then create an immutable input frame for each local player. Each frame contains:

- movement direction,
- aim direction or mouse world position,
- fire edge state,
- mine edge state,
- shot-path state where supported.

PlayerTank consumes the frame assigned to its `PlayerId`. P1 receives the keyboard/mouse frame; P2 receives the keyboard-only frame. Static bind objects must no longer decide input for every PlayerTank instance.

### Control Eligibility

Separate offline local control ownership from `NetPlay.IsClientMatched`. In an offline session, a tank is controllable only when its player ID is an active local slot. In a network session, the existing client-matching rule remains authoritative.

### Aiming

P1 keeps the existing mouse-to-world turret calculation. P2 stores a normalized aim vector and derives turret rotation from that vector. Controller-style aiming must not warp the operating-system mouse cursor. This also creates the seam needed for compatible controllers or an iPhone controller in a later iteration.

### Campaign Spawning

When a campaign mission loads offline, spawn player templates only when their player IDs are active in the local session. Vanilla mission files remain unchanged. AI and block loading remain unchanged.

### Online Isolation

Do not alter packet formats or client IDs. Network play continues to synchronize the one tank matched to the current client. Local co-op state is disabled whenever a client or server multiplayer session owns player control.

## Error Handling and Edge Cases

- Because macOS exposes multiple keyboards as one device, the game cannot detect which physical keyboard was unplugged. P2 controls remain available from any attached keyboard.
- Default P1 and P2 keys must be validated as disjoint.
- Chat, pause, level editor, and menu focus continue to suppress gameplay input.
- Simultaneous P1 and P2 fire/mine edges must be tracked independently so one action cannot trigger both tanks.
- Missions with fewer than two player templates fall back to their available slots and show a clear message instead of crashing.
- POV difficulty remains single-player-only for the first local co-op release because one first-person camera cannot represent two tanks.

## Verification

Automated tests or isolated test seams will cover:

- P1 keys affect only P1;
- P2 keys affect only P2;
- P2 aim does not mutate the mouse position;
- exactly two player tanks spawn in local co-op Vanilla missions;
- only one player spawns in single-player;
- separate lives and kill counts are preserved across missions;
- the mission continues with one survivor and ends when appropriate;
- bonus lives apply to both local players;
- network control eligibility remains unchanged.

Manual acceptance testing on the target M1 Mac will verify:

1. The separate app launches to a visible window.
2. Local Co-op can be selected and a Vanilla campaign started.
3. Two physical keyboards can move and act simultaneously without cross-control.
4. P1 mouse aiming remains stable while P2 aims and fires.
5. A full mission can be won with both players and with one survivor.
6. Single-player still starts and plays normally.

## Delivery

Publish the customized build with the same required .NET runtime and native libraries as the working original bundle. Install it as `~/Applications/Tanks Rebirth Local Co-op.app`. Do not overwrite `~/Applications/Tanks Rebirth.app`.
