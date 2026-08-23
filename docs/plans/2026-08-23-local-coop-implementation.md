# Tanks Rebirth Local Co-op Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a separate macOS Tanks Rebirth app in which P1 uses keyboard/mouse and P2 uses independent keyboard controls during two-player local campaign co-op.

**Architecture:** Add a pure local-session model and a per-player input router that turns the single macOS keyboard snapshot into disjoint P1/P2 input frames. Offline campaign spawning and PlayerTank control use active local slots; connected network play remains on the existing `NetPlay.IsClientMatched` path. Package the customized x64 build beside the original app and verify it with two physical keyboards.

**Tech Stack:** C# 12, .NET 8, MonoGame DesktopGL 3.8, xUnit, zsh app-bundle scripts, macOS/Rosetta 2.

---

## Working Rules

- Work in `/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/TanksRebirth-source`.
- Base commit is `ff883cf` on top of tag `1.8.1.1-alpha`.
- Use @test-driven-development for every behavior change.
- Use @systematic-debugging if a build, test, or launch behaves unexpectedly.
- Use @verification-before-completion before claiming the app is ready.
- Preserve `/Users/andrewbowlus/Applications/Tanks Rebirth.app` unchanged.
- Do not change network packet formats.

### Task 1: Install a Private .NET 8 x64 SDK and Capture the Baseline

**Files:**
- No repository files changed.
- Create outside the repository: `/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/`

**Step 1: Verify the source checkpoint**

Run:

```bash
git status --short
git log -2 --oneline
```

Expected: clean status; `ff883cf docs: design local keyboard co-op` above `feaf21a`.

**Step 2: Download the official SDK installer**

Run:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /private/tmp/tanks-dotnet-install.sh
```

Expected: exit 0 and a non-empty installer file.

**Step 3: Install the x64 .NET 8 SDK privately**

Run:

```bash
bash /private/tmp/tanks-dotnet-install.sh --channel 8.0 --architecture x64 --install-dir /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64
```

Expected: installer reports a .NET 8 SDK and successful installation.

**Step 4: Verify Rosetta and the SDK**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet --info
```

Expected: RID `osx-x64` and an 8.0 SDK.

**Step 5: Restore and build the untouched game**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet restore TanksRebirth.sln
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet build TanksRebirth.sln -c Debug --no-restore
```

Expected: restore and build succeed. If they fail, stop and diagnose the baseline before feature edits.

### Task 2: Add the xUnit Test Project

**Files:**
- Create: `Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj`
- Create: `Tests/TanksRebirth.Tests/SmokeTests.cs`
- Modify: `TanksRebirth.csproj`
- Modify: `TanksRebirth.sln`

**Step 1: Create the test project file**

Use this project definition:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../TanksRebirth.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Exclude test source from the game executable**

Add to `TanksRebirth.csproj`:

```xml
<ItemGroup>
  <Compile Remove="Tests/**/*.cs" />
</ItemGroup>
```

**Step 3: Add a smoke test**

Create `SmokeTests.cs`:

```csharp
namespace TanksRebirth.Tests;

public sealed class SmokeTests {
    [Fact]
    public void TestRunnerStarts() => Assert.True(true);
}
```

**Step 4: Add the project to the solution**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet sln TanksRebirth.sln add Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj
```

Expected: solution reports the test project was added.

**Step 5: Run the smoke test**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj -c Debug
```

Expected: 1 passed, 0 failed.

**Step 6: Commit**

```bash
git add TanksRebirth.csproj TanksRebirth.sln Tests/TanksRebirth.Tests
git commit -m "test: add local co-op test project"
```

### Task 3: Add Pure Local Session State

**Files:**
- Create: `GameContent/Systems/LocalCoop/LocalSession.cs`
- Create: `Tests/TanksRebirth.Tests/LocalSessionTests.cs`

**Step 1: Write failing session tests**

```csharp
using TanksRebirth.GameContent.Systems.LocalCoop;

namespace TanksRebirth.Tests;

public sealed class LocalSessionTests {
    [Fact]
    public void SinglePlayerActivatesOnlyBlue() {
        var session = new LocalSession();
        session.StartSinglePlayer();

        Assert.Equal(1, session.PlayerCount);
        Assert.True(session.IsActivePlayer(0));
        Assert.False(session.IsActivePlayer(1));
    }

    [Fact]
    public void LocalCoopActivatesBlueAndRedOnly() {
        var session = new LocalSession();
        session.StartLocalCoop();

        Assert.Equal(2, session.PlayerCount);
        Assert.True(session.IsActivePlayer(0));
        Assert.True(session.IsActivePlayer(1));
        Assert.False(session.IsActivePlayer(2));
        Assert.False(session.IsActivePlayer(3));
    }
}
```

**Step 2: Run tests and verify failure**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj --filter LocalSessionTests
```

Expected: compile failure because `LocalSession` does not exist.

**Step 3: Implement the minimal session model**

```csharp
namespace TanksRebirth.GameContent.Systems.LocalCoop;

public enum LocalPlayMode {
    SinglePlayer,
    LocalCoop
}

public sealed class LocalSession {
    public LocalPlayMode Mode { get; private set; } = LocalPlayMode.SinglePlayer;
    public int PlayerCount => Mode == LocalPlayMode.LocalCoop ? 2 : 1;
    public bool IsLocalCoop => Mode == LocalPlayMode.LocalCoop;

    public void StartSinglePlayer() => Mode = LocalPlayMode.SinglePlayer;
    public void StartLocalCoop() => Mode = LocalPlayMode.LocalCoop;
    public bool IsActivePlayer(int playerId) => playerId >= 0 && playerId < PlayerCount;
}

public static class LocalGameSession {
    public static LocalSession Current { get; } = new();
}
```

**Step 4: Run all tests**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj
```

Expected: all tests pass.

**Step 5: Commit**

```bash
git add GameContent/Systems/LocalCoop/LocalSession.cs Tests/TanksRebirth.Tests/LocalSessionTests.cs
git commit -m "feat: add local play session state"
```

### Task 4: Build the Per-Player Input Router

**Files:**
- Create: `Internals/Common/Framework/Input/LocalPlayerInputFrame.cs`
- Create: `Internals/Common/Framework/Input/LocalPlayerInputRouter.cs`
- Create: `Tests/TanksRebirth.Tests/LocalPlayerInputRouterTests.cs`

**Step 1: Write failing input-isolation tests**

Cover these cases in `LocalPlayerInputRouterTests.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.Framework.Input;

namespace TanksRebirth.Tests;

public sealed class LocalPlayerInputRouterTests {
    [Fact]
    public void P1KeysDoNotMoveP2() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.W), new KeyboardState(), default, default);

        Assert.Equal(new Vector2(0, -1), router.GetFrame(0).Movement);
        Assert.Equal(Vector2.Zero, router.GetFrame(1).Movement);
    }

    [Fact]
    public void P2KeysDoNotMoveP1() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.I), new KeyboardState(), default, default);

        Assert.Equal(Vector2.Zero, router.GetFrame(0).Movement);
        Assert.Equal(new Vector2(0, -1), router.GetFrame(1).Movement);
    }

    [Fact]
    public void P2AimRetainsItsLastDirection() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.Right), new KeyboardState(), default, default);
        var aimed = router.GetFrame(1).Aim;
        router.Update(new KeyboardState(), new KeyboardState(Keys.Right), default, default);

        Assert.Equal(Vector2.UnitX, aimed);
        Assert.Equal(Vector2.UnitX, router.GetFrame(1).Aim);
    }

    [Fact]
    public void FireEdgesAreIndependent() {
        var router = new LocalPlayerInputRouter();
        router.Update(new KeyboardState(Keys.Enter), new KeyboardState(), default, default);

        Assert.False(router.GetFrame(0).FireJustPressed);
        Assert.True(router.GetFrame(1).FireJustPressed);
    }
}
```

Add tests for P1 left-click, P1 Space mine, P2 RightShift mine, diagonal P2 aim normalization, and default P2 aim.

**Step 2: Run tests and verify failure**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj --filter LocalPlayerInputRouterTests
```

Expected: compile failure because router types do not exist.

**Step 3: Add the input frame**

```csharp
using Microsoft.Xna.Framework;

namespace TanksRebirth.Internals.Common.Framework.Input;

public enum LocalAimSource {
    Mouse,
    Direction
}

public readonly record struct LocalPlayerInputFrame(
    Vector2 Movement,
    Vector2 Aim,
    LocalAimSource AimSource,
    bool FireJustPressed,
    bool MineJustPressed,
    bool ShotPathHeld);
```

**Step 4: Implement the router**

`LocalPlayerInputRouter` owns two frames and P2's retained aim. It must:

- map P1 W/A/S/D and the current mouse position;
- create P1 click/Space edges from current and previous snapshots;
- map P2 I/J/K/L movement;
- map P2 arrow-key aim and retain the last non-zero normalized direction;
- create P2 Return/RightShift edges;
- never call `Mouse.SetPosition`.

Use helpers with explicit key parameters:

```csharp
private static Vector2 ReadDirection(
    KeyboardState state, Keys up, Keys down, Keys left, Keys right) {
    var direction = Vector2.Zero;
    if (state.IsKeyDown(up)) direction.Y -= 1;
    if (state.IsKeyDown(down)) direction.Y += 1;
    if (state.IsKeyDown(left)) direction.X -= 1;
    if (state.IsKeyDown(right)) direction.X += 1;
    return direction == Vector2.Zero ? direction : Vector2.Normalize(direction);
}

private static bool JustPressed(KeyboardState current, KeyboardState previous, Keys key)
    => current.IsKeyDown(key) && previous.IsKeyUp(key);
```

Expose a static runtime instance without making tests depend on it:

```csharp
public static LocalPlayerInputRouter Runtime { get; } = new();
```

**Step 5: Run all tests**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test Tests/TanksRebirth.Tests/TanksRebirth.Tests.csproj
```

Expected: all tests pass.

**Step 6: Commit**

```bash
git add Internals/Common/Framework/Input/LocalPlayerInputFrame.cs Internals/Common/Framework/Input/LocalPlayerInputRouter.cs Tests/TanksRebirth.Tests/LocalPlayerInputRouterTests.cs
git commit -m "feat: route independent local player input"
```

### Task 5: Poll Local Frames and Control the Correct PlayerTank

**Files:**
- Modify: `TankGame.cs:660-683`
- Modify: `GameContent/Systems/TankSystem/PlayerTank.cs:190-404`
- Create: `Tests/TanksRebirth.Tests/LocalControlPolicyTests.cs`
- Create: `GameContent/Systems/LocalCoop/LocalControlPolicy.cs`

**Step 1: Write failing control-policy tests**

Create a pure policy that can be tested without constructing tanks:

```csharp
[Theory]
[InlineData(false, false, 0, true)]
[InlineData(false, false, 1, false)]
[InlineData(false, true, 0, true)]
[InlineData(false, true, 1, true)]
[InlineData(false, true, 2, false)]
[InlineData(true, true, 1, false)]
public void ControlEligibilityIsIsolated(
    bool networkConnected, bool localCoop, int playerId, bool expected) {
    var session = new LocalSession();
    if (localCoop) session.StartLocalCoop();

    Assert.Equal(expected,
        LocalControlPolicy.CanControlOffline(networkConnected, session, playerId));
}
```

Expected network-connected result from this offline helper is always false; connected sessions defer to `NetPlay.IsClientMatched`.

**Step 2: Implement and pass the policy tests**

```csharp
public static class LocalControlPolicy {
    public static bool CanControlOffline(
        bool networkConnected, LocalSession session, int playerId)
        => !networkConnected && session.IsActivePlayer(playerId);
}
```

**Step 3: Poll the router after `InputUtils.PollEvents()`**

In `TankGame.SubHandleLogic`, call:

```csharp
LocalPlayerInputRouter.Runtime.Update(
    InputUtils.CurrentKeySnapshot,
    InputUtils.OldKeySnapshot,
    InputUtils.CurrentMouseSnapshot,
    InputUtils.OldMouseSnapshot);
```

**Step 4: Add the local-co-op PlayerTank path**

In `PlayerTank.Update`:

- leave the existing network and ordinary single-player control path intact;
- when disconnected and `LocalGameSession.Current.IsLocalCoop`, fetch this tank's frame;
- return without input for inactive player IDs;
- process P1 mouse aim only when `AimSource == Mouse`;
- process P2 directional aim with:

```csharp
TurretRotation = -frame.Aim.ToRotation() - MathHelper.PiOver2;
```

Validate the sign visually; if the turret points opposite the arrow, correct the formula with one isolated test/manual observation rather than altering movement.

Drive movement through a new helper that accepts `frame.Movement` and reuses the existing chassis-rotation/velocity calculation. Invoke `Shoot(false)` and `LayMine()` only from that player's edge flags. Keep chat, pause, mission-state, stationary, stun, and cooldown guards.

**Step 5: Prevent P2 HUD/input checks from using global client matching**

Add a `ControlledHere` property that returns active local-slot ownership offline and `NetPlay.IsClientMatched(PlayerId)` online. Use it for control-only branches and local player extras. Do not use it to decide network packet ownership.

**Step 6: Run tests and build**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test TanksRebirth.sln
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet build TanksRebirth.sln -c Debug --no-restore
```

Expected: all tests pass and build succeeds.

**Step 7: Commit**

```bash
git add TankGame.cs GameContent/Systems/TankSystem/PlayerTank.cs GameContent/Systems/LocalCoop/LocalControlPolicy.cs Tests/TanksRebirth.Tests/LocalControlPolicyTests.cs
git commit -m "feat: control two local player tanks"
```

### Task 6: Spawn Active Local Players and Separate Lives/Kills

**Files:**
- Modify: `GameContent/Systems/Campaign.cs:110-216`
- Modify: `GameContent/Systems/TankSystem/PlayerTank.cs:84-112,405-451`
- Modify: `GameContent/Systems/AI/AITank.cs:300-338`
- Modify: `GameContent/Systems/IntermissionHandler.cs:155-239`
- Create: `GameContent/Systems/LocalCoop/LocalCampaignRules.cs`
- Create: `Tests/TanksRebirth.Tests/LocalCampaignRulesTests.cs`

**Step 1: Write failing rules tests**

Pure tests must cover:

- single-player player IDs `[0]`;
- local co-op player IDs `[0, 1]`;
- inactive IDs never spawn;
- bonus life adds to active players only;
- destroying P2 decrements P2 only;
- one living player means the local team can continue;
- zero available templates yields a clear validation failure.

Use a pure helper API such as:

```csharp
Assert.Equal(new[] { 0, 1 }, LocalCampaignRules.ActivePlayerIds(coopSession));

var lives = new[] { 3, 3, 0, 0 };
LocalCampaignRules.ChangeLife(lives, playerId: 1, delta: -1);
Assert.Equal(new[] { 3, 2, 0, 0 }, lives);
```

**Step 2: Implement minimal pure rules and pass tests**

Keep rules free of MonoGame rendering and static game state. The runtime code passes `LocalGameSession.Current` and the `PlayerTank.Lives` array into them.

**Step 3: Filter offline campaign templates**

In `Campaign.SetupLoadedMission`, before constructing an offline player tank:

```csharp
if (!Client.IsConnected() &&
    !LocalGameSession.Current.IsActivePlayer(template.PlayerType))
    continue;
```

Keep the existing connected-client branch unchanged. After constructing an active offline player, remove it if that player's lives are `<= 0` outside level-editor testing.

If a mission contains fewer active templates than the session requires, send one clear `ChatSystem` error and continue with available players.

**Step 4: Separate offline lives**

- `PlayerTank.Destroy`: decrement `Lives[PlayerId]` offline rather than calling `AddLives(-1)` for everyone.
- `SetLives`: set indices `0..<PlayerCount` and clear inactive local indices when disconnected.
- `AddLives`: add to indices `0..<PlayerCount` when disconnected so bonus-life missions reward both active players.
- Keep connected-client synchronization semantics unchanged.

**Step 5: Attribute offline kills to the source player**

In `AITank.Destroy`, when `context.Source is PlayerTank p` and the game is offline, increment `KillCounts[p.PlayerId]`. Retain the existing matched-client branch online. Remove the hardcoded offline `KillCounts[0]` fallback for player-owned damage; retain non-player fallback behavior only where intentional.

**Step 6: Make mission resolution use active local tanks**

Update local checks in `IntermissionHandler` to consider only active local player IDs. A destroyed P1 must not turn a P2 victory into defeat. Keep team-based enemy resolution and connected-session code unchanged.

**Step 7: Run tests and build**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test TanksRebirth.sln
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet build TanksRebirth.sln -c Debug --no-restore
```

Expected: all tests pass; build succeeds.

**Step 8: Commit**

```bash
git add GameContent/Systems/Campaign.cs GameContent/Systems/TankSystem/PlayerTank.cs GameContent/Systems/AI/AITank.cs GameContent/Systems/IntermissionHandler.cs GameContent/Systems/LocalCoop/LocalCampaignRules.cs Tests/TanksRebirth.Tests/LocalCampaignRulesTests.cs
git commit -m "feat: add two-player campaign rules"
```

### Task 7: Add the Local Co-op Menu Choice

**Files:**
- Modify: `GameContent/UI/MainMenu/MainMenuUI.UIManager.cs:77-182`
- Modify: `GameContent/UI/MainMenu/MainMenuUI.cs:91-105`
- Modify: `GameContent/UI/MainMenu/MainMenuUI.Campaigns.cs:30-120`

**Step 1: Add `PlayButton_LocalCoop`**

Create a `UITextButton` labeled `Local Co-op` with tooltip:

```text
Two players on this Mac: P1 uses keyboard and mouse; P2 uses I/J/K/L plus arrow keys.
```

Its click handler must call `LocalGameSession.Current.StartLocalCoop()`, disable POV difficulty if enabled, call `SetCampaignDisplay()`, and enter `UIState.Campaigns`.

**Step 2: Reset mode from other play choices**

- Single Player click calls `StartSinglePlayer()` before campaign display.
- Multiplayer and Level Editor reset local mode to single-player before entering their flows.

**Step 3: Include the button everywhere menu visibility is managed**

Add it to `_menuElements`, `HideAll`, and `SetPlayButtonsVisibility`. Adjust vertical positions so Single Player, Local Co-op, Difficulties, Level Editor, and Multiplayer do not overlap at 1920x1080 or the current scaled window.

**Step 4: Build and inspect warnings**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet build TanksRebirth.sln -c Debug --no-restore
```

Expected: build succeeds with no new warnings attributable to the menu change.

**Step 5: Commit**

```bash
git add GameContent/UI/MainMenu/MainMenuUI.UIManager.cs GameContent/UI/MainMenu/MainMenuUI.cs GameContent/UI/MainMenu/MainMenuUI.Campaigns.cs
git commit -m "feat: add local co-op menu flow"
```

### Task 8: Display Both Players' Scores and Lives

**Files:**
- Modify: `GameContent/UI/GameSceneUI.cs:15-129`
- Modify: `GameContent/Systems/IntermissionSystem.cs:384-421`
- Modify: `GameContent/Systems/TankSystem/PlayerTank.cs:581-635`

**Step 1: Use local player count offline**

Replace offline hardcoded draw counts with:

```csharp
var drawCount = Client.IsConnected()
    ? Server.CurrentClientCount
    : LocalGameSession.Current.PlayerCount;
```

Use the same rule for the intermission life display.

**Step 2: Label the players**

Offline names are `P1` and `P2`; connected names remain server-provided. Keep the Blue/Red colors and existing in-world chevrons.

**Step 3: Show kills and lives together**

Extend `DrawScore` to render a compact string such as `P1  K 4  L 2`, using `KillCounts[i]` and `Lives[i]`. Maintain left/right placement for two players and verify it does not overlap the mission info bar.

**Step 4: Build and run all tests**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test TanksRebirth.sln
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet build TanksRebirth.sln -c Release --no-restore
```

Expected: all tests pass; Release build succeeds.

**Step 5: Commit**

```bash
git add GameContent/UI/GameSceneUI.cs GameContent/Systems/IntermissionSystem.cs GameContent/Systems/TankSystem/PlayerTank.cs
git commit -m "feat: show local co-op player status"
```

### Task 9: Package a Separate macOS App

**Files:**
- Create: `scripts/package-local-coop-app.sh`
- Create: `scripts/test-local-coop-app.sh`
- Create: `packaging/macos/Info.plist`
- Create: `packaging/macos/launcher`

**Step 1: Write a failing bundle validation script**

`scripts/test-local-coop-app.sh` accepts an app path and verifies:

- `Info.plist` passes `plutil -lint`;
- bundle identifier is `com.andrewbowlus.tanksrebirth.localcoop`;
- executable is `launcher`;
- launcher and `TanksRebirth` are executable;
- `Contents/Resources/game/TanksRebirth.dll` exists;
- `libSDL2-2.0.0.dylib` and `libopenal.1.dylib` exist;
- `.dotnet-x64/host/fxr/8.0.30/libhostfxr.dylib` exists;
- original app path is not the target path.

Run it against a nonexistent staging app.

Expected: FAIL with missing bundle.

**Step 2: Add the launcher**

Use the proven launcher pattern from `/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/test-tanks-rebirth-launcher.sh`:

```zsh
#!/bin/zsh
set -eu
contents_dir="${0:A:h:h}"
game_dir="$contents_dir/Resources/game"
export DOTNET_ROOT="$game_dir/.dotnet-x64"
export DOTNET_ROOT_X64="$game_dir/.dotnet-x64"
export DYLD_LIBRARY_PATH="$game_dir${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
cd "$game_dir"
exec arch -x86_64 "$game_dir/.dotnet-x64/dotnet" "$game_dir/TanksRebirth.dll"
```

**Step 3: Add the package script**

The script must:

1. publish with the private SDK to `artifacts/local-coop/publish`;
2. stage `artifacts/Tanks Rebirth Local Co-op.app`;
3. copy the publish output into `Contents/Resources/game`;
4. copy the known-good `.dotnet-x64` runtime and any missing native dylibs from the original app without modifying it;
5. install `Info.plist` and launcher;
6. set executable bits;
7. ad-hoc sign with `codesign --force --deep --sign -`;
8. run `scripts/test-local-coop-app.sh` on the staged app.

Publish command:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet publish TanksRebirth.csproj -c Release -r osx-x64 --self-contained false -o artifacts/local-coop/publish
```

**Step 4: Run package validation**

Run:

```bash
zsh scripts/package-local-coop-app.sh
zsh scripts/test-local-coop-app.sh "artifacts/Tanks Rebirth Local Co-op.app"
```

Expected: both scripts report PASS.

**Step 5: Verify the original app is unchanged**

Run:

```bash
zsh /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/test-tanks-rebirth-launcher.sh
```

Expected: original launcher bundle still reports PASS.

**Step 6: Commit**

```bash
git add scripts/package-local-coop-app.sh scripts/test-local-coop-app.sh packaging/macos/Info.plist packaging/macos/launcher
git commit -m "build: package local co-op mac app"
```

### Task 10: Install and Perform Real Two-Keyboard Acceptance Testing

**Files:**
- Install outside repository: `/Users/andrewbowlus/Applications/Tanks Rebirth Local Co-op.app`
- Inspect logs: `/Users/andrewbowlus/Documents/My Games/Tanks Rebirth/Logs/`

**Step 1: Run the full automated gate**

Run:

```bash
arch -x86_64 /Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64/dotnet test TanksRebirth.sln -c Release
zsh scripts/package-local-coop-app.sh
zsh scripts/test-local-coop-app.sh "artifacts/Tanks Rebirth Local Co-op.app"
git diff --check
git status --short
```

Expected: tests and bundle validation pass; no whitespace errors; only intended changes are present.

**Step 2: Install without overwriting the original**

Copy the staged app to the exact separate destination:

```bash
ditto "artifacts/Tanks Rebirth Local Co-op.app" "/Users/andrewbowlus/Applications/Tanks Rebirth Local Co-op.app"
```

Expected: both original and local-co-op app bundles exist.

**Step 3: Launch and verify an on-screen window**

Open the local-co-op app, then verify with Computer Use/CoreGraphics that a Tanks Rebirth window is visible and on-screen. A process ID or initialization log alone is not sufficient.

Expected: visible main menu containing `Local Co-op`.

**Step 4: Verify the actual two-keyboard seam**

With both physical keyboards attached:

1. Choose Local Co-op and Vanilla mission 1.
2. Confirm only Blue P1 and Red P2 spawn.
3. Hold P1 W while pressing P2 J; verify each tank responds only to its own keys.
4. Aim P1 with the mouse while aiming P2 with arrows; verify the pointer does not jump.
5. Fire both players simultaneously; verify two distinct tanks shoot.
6. Place both players' mines independently.
7. Destroy P1 and complete the mission with P2; verify victory and separate lives.
8. Complete a bonus-life mission or use an isolated debug seam; verify both active players gain one life.
9. Return to menu and start Single Player; verify only P1 spawns and the original controls still work.

**Step 5: Inspect the newest log**

Read the newest client log and search for `Error`, `Exception`, SDL failures, and missing assets.

Expected: no new fatal errors from local co-op.

**Step 6: Final commit if QA required fixes**

After rerunning all gates:

```bash
git add <only-the-QA-fix-files>
git commit -m "fix: complete local co-op acceptance"
```

**Step 7: Final readback**

Report:

- installed app path;
- exact test count and result;
- bundle validation result;
- visible-window confirmation;
- two-keyboard control result;
- single-player regression result;
- any known limitations, especially fixed P2 bindings and disabled POV co-op.
