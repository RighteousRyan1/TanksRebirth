using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.AI;

public static class AIManager {
    // /// <summary>The AI parameter defaults for a given tank ID.</summary>
    //public static Dictionary<int, AIParameters> AIParameterDefaults = [];
    // /// <summary>The AI parameter defaults for a given tank ID.</summary>
    //public static Dictionary<int, TankProperties> AIPropertyDefaults = [];
    /// <summary>
    /// Fetch the default AI parameters from the given input tank type.
    /// </summary>
    /// <param name="tankType">The type of the tank to retrieve the defaults from.</param>
    /// <returns></returns>
    public static AIParameters GetAIParameters(int tankType) {
        var aiParams = new AIParameters();

        /*if (!AIParameterDefaults.TryGetValue(tankType, out AIParameters? value)) {
            var json = File.ReadAllText("ai/tank_" + TankID.Collection.GetKey(tankType) + ".json");
            using JsonDocument doc = JsonDocument.Parse(json);

            var parameters = doc.RootElement.GetProperty("Parameters");
            AIParameterDefaults.Add(tankType, parameters.Deserialize<AIParameters>()!);
            return AIParameterDefaults[tankType];
        }
        else {
            return value;
        }*/

        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                aiParams.DetectionForgivenessHostile = 1.6f;
                aiParams.AimOffset = MathHelper.ToRadians(170);
                aiParams.RandomTimerMaxShoot = 45;
                aiParams.RandomTimerMinShoot = 30;
                aiParams.TurretSpeed = 0.01f;
                aiParams.TurretMovementTimer = 60;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.01f;
                break;

            case TankID.Ash:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 15;
                aiParams.RandomTimerMinMove = 10;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 40;
                aiParams.AwarenessHostileShell = 40;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = 0.03f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 30;
                aiParams.AimOffset = MathHelper.ToRadians(40);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 45;
                aiParams.RandomTimerMinShoot = 30;
                aiParams.TurretSpeed = 0.01f;
                aiParams.TurretMovementTimer = 45;
                aiParams.TankAwarenessShoot = 70;


                aiParams.BaseXP = 0.015f;
                break;

            case TankID.Marine:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileShell = 40;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = -0.1f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 30;
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.05f;
                aiParams.TurretMovementTimer = 8;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.04f;
                break;

            case TankID.Yellow:
                aiParams.ObstacleAwarenessMine = 100;
                aiParams.RandomTimerMaxMine = 60;
                aiParams.RandomTimerMinMine = 40;
                aiParams.TankAwarenessMine = 100;
                aiParams.ChanceMineLayNearBreakables = 0.5f;
                aiParams.ChanceMineLay = 0.5f;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 15;
                aiParams.RandomTimerMinMove = 10;
                aiParams.AwarenessFriendlyMine = 130;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 160;
                aiParams.AwarenessHostileShell = 40;
                aiParams.CantShootWhileFleeing = true;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 30;
                aiParams.AimOffset = MathHelper.ToRadians(40);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 45;
                aiParams.RandomTimerMinShoot = 30;
                aiParams.TurretSpeed = 0.02f;
                aiParams.TurretMovementTimer = 30;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.035f;

                if (Difficulties.Types["PieFactory"]) {
                    aiParams.ChanceMineLay = 1f;
                    aiParams.AwarenessHostileMine = 0;
                }

                break;

            case TankID.Pink:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileShell = 40;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = 0.2f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 50;
                aiParams.AimOffset = MathHelper.ToRadians(40);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.02f;
                aiParams.TurretMovementTimer = 20;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.08f;
                break;

            case TankID.Violet:
                aiParams.ObstacleAwarenessMine = 200;
                aiParams.RandomTimerMaxMine = 60;
                aiParams.RandomTimerMinMine = 40;
                aiParams.TankAwarenessMine = 100;
                aiParams.ChanceMineLayNearBreakables = 0.05f;
                aiParams.ChanceMineLay = 0.03f;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 160;
                aiParams.AwarenessHostileShell = 60;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = 0.1f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 50;
                aiParams.AimOffset = MathHelper.ToRadians(40);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.03f;
                aiParams.TurretMovementTimer = 20;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.1f;
                break;

            case TankID.Green:
                aiParams.AimOffset = MathHelper.ToRadians(80);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.02f;
                aiParams.TurretMovementTimer = 30;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.12f;
                break;

            case TankID.White:
                aiParams.ObstacleAwarenessMine = 200;
                aiParams.RandomTimerMaxMine = 60;
                aiParams.RandomTimerMinMine = 40;
                aiParams.TankAwarenessMine = 100;
                aiParams.ChanceMineLayNearBreakables = 0.05f;
                aiParams.ChanceMineLay = 0.03f;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 160;
                aiParams.AwarenessHostileShell = 40;
                aiParams.CantShootWhileFleeing = false;
                aiParams.AggressivenessBias = 0.1f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 50;
                aiParams.AimOffset = MathHelper.ToRadians(40);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.03f;
                aiParams.TurretMovementTimer = 30;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.125f;
                break;

            case TankID.Black:
                aiParams.ObstacleAwarenessMine = 200;
                aiParams.RandomTimerMaxMine = 60;
                aiParams.RandomTimerMinMine = 40;
                aiParams.TankAwarenessMine = 100;
                aiParams.ChanceMineLayNearBreakables = 0.05f;
                aiParams.ChanceMineLay = 0.03f;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileShell = 100;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = 0.2f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 50;
                aiParams.AimOffset = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.03f;
                aiParams.TurretMovementTimer = 20;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.145f;

                break;

            #endregion

            #region MasterMod

            case TankID.Bronze:
                aiParams.ObstacleAwarenessMine = 200; // 2
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 30; // 28
                aiParams.AimOffset = MathHelper.ToRadians(0); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 30; // 35
                aiParams.RandomTimerMinShoot = 15; // 36
                aiParams.TurretSpeed = 0.06f; // 39
                aiParams.TurretMovementTimer = 10; // 40
                aiParams.TankAwarenessShoot = 70; // 41

                aiParams.BaseXP = 0.025f;
                break;
            case TankID.Silver:
                aiParams.ObstacleAwarenessMine = 200; // 2
                aiParams.RandomTimerMaxMine = 100; // 4
                aiParams.RandomTimerMinMine = 60; // 5
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.ChanceMineLayNearBreakables = 0.03f; // 7
                aiParams.ChanceMineLay = 0.05f; // 8
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30); // 13
                aiParams.RandomTimerMaxMove = 10; // 14
                aiParams.RandomTimerMinMove = 5; // 15
                aiParams.AwarenessFriendlyMine = 120; // 16
                aiParams.AwarenessFriendlyShell = 120; // 17
                aiParams.AwarenessHostileMine = 100; // 18
                aiParams.AwarenessHostileShell = 100; // 19
                aiParams.CantShootWhileFleeing = true; // 20
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 100; // 28
                aiParams.AimOffset = MathHelper.ToRadians(60); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 20; // 35
                aiParams.RandomTimerMinShoot = 10; // 36
                aiParams.TurretSpeed = 0.045f; // 39
                aiParams.TurretMovementTimer = 60; // 40
                aiParams.TankAwarenessShoot = 70; // 41

                aiParams.BaseXP = 0.07f;
                break;
            case TankID.Sapphire:
                aiParams.ObstacleAwarenessMine = 100; // 2
                aiParams.RandomTimerMaxMine = 40; // 4
                aiParams.RandomTimerMinMine = 30; // 5
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.ChanceMineLayNearBreakables = 0.04f; // 7
                aiParams.ChanceMineLay = 0.08f; // 8
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30); // 13
                aiParams.RandomTimerMaxMove = 15; // 14
                aiParams.RandomTimerMinMove = 10; // 15
                aiParams.AwarenessFriendlyMine = 120; // 16
                aiParams.AwarenessFriendlyShell = 60; // 17
                aiParams.AwarenessHostileMine = 40; // 18
                aiParams.AwarenessHostileShell = 70; // 19
                aiParams.CantShootWhileFleeing = false; // 20
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 60; // 28
                aiParams.AimOffset = MathHelper.ToRadians(10); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 10; // 35
                aiParams.RandomTimerMinShoot = 5; // 36
                aiParams.TurretSpeed = 0.01f; // 39
                aiParams.TurretMovementTimer = 3; // 40
                aiParams.TankAwarenessShoot = 70; // 41

                aiParams.BaseXP = 0.095f;
                break;
            case TankID.Ruby:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(45);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 0;
                aiParams.AwarenessHostileShell = 50;
                aiParams.AggressivenessBias = 0.3f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 20;
                aiParams.AimOffset = MathHelper.ToRadians(3);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.025f;
                aiParams.TurretMovementTimer = 10;
                aiParams.TankAwarenessShoot = 70;


                aiParams.CantShootWhileFleeing = true;

                aiParams.BaseXP = 0.13f;
                break;
            case TankID.Citrine:
                aiParams.ObstacleAwarenessMine = 100; // 2
                aiParams.RandomTimerMaxMine = 60; // 4
                aiParams.RandomTimerMinMine = 40; // 5
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.ChanceMineLayNearBreakables = 0.1f; // 7
                aiParams.ChanceMineLay = 0.2f; // 8
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30); // 13
                aiParams.RandomTimerMaxMove = 30; // 14
                aiParams.RandomTimerMinMove = 10; // 15
                aiParams.AwarenessFriendlyMine = 160; // 16
                aiParams.AwarenessFriendlyShell = 160; // 17
                aiParams.AwarenessHostileMine = 140; // 18
                aiParams.AwarenessHostileShell = 80; // 19
                aiParams.CantShootWhileFleeing = true; // 20
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 30; // 28
                aiParams.AimOffset = MathHelper.ToRadians(20); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 10; // 35
                aiParams.RandomTimerMinShoot = 5; // 36
                aiParams.TurretSpeed = 0.035f; // 39
                aiParams.TurretMovementTimer = 30; // 40
                aiParams.TankAwarenessShoot = 70; // 41


                aiParams.BaseXP = 0.09f;
                break;
            case TankID.Amethyst:
                aiParams.ObstacleAwarenessMine = 150;
                aiParams.RandomTimerMaxMine = 60;
                aiParams.RandomTimerMinMine = 40;
                aiParams.TankAwarenessMine = 100;
                aiParams.ChanceMineLayNearBreakables = 0.05f;
                aiParams.ChanceMineLay = 0.1f;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 5;
                aiParams.RandomTimerMinMove = 2;
                aiParams.AwarenessFriendlyMine = 160;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileMine = 80;
                aiParams.AwarenessHostileShell = 100;
                aiParams.CantShootWhileFleeing = true;
                aiParams.AggressivenessBias = 0.03f;
                aiParams.MaxQueuedMovements = 4;
                aiParams.ObstacleAwarenessMovement = 35;
                aiParams.AimOffset = MathHelper.ToRadians(60);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 20;
                aiParams.RandomTimerMinShoot = 10;
                aiParams.TurretSpeed = 0.03f;
                aiParams.TurretMovementTimer = 20;
                aiParams.TankAwarenessShoot = 70;

                aiParams.BaseXP = 0.095f;
                break;
            case TankID.Emerald:
                aiParams.ObstacleAwarenessMine = 100; // 2
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 50; // 28
                aiParams.AimOffset = MathHelper.ToRadians(80); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 10; // 35
                aiParams.RandomTimerMinShoot = 5; // 36
                aiParams.TurretSpeed = 0.04f; // 39
                aiParams.TurretMovementTimer = 30; // 40
                aiParams.TankAwarenessShoot = 70; // 41

                aiParams.BaseXP = 0.14f;
                break;

            case TankID.Gold:
                aiParams.ObstacleAwarenessMine = 200; // 2
                aiParams.RandomTimerMaxMine = 100; // 4
                aiParams.RandomTimerMinMine = 60; // 5
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.ChanceMineLayNearBreakables = 0.05f; // 7
                aiParams.ChanceMineLay = 0.03f; // 8
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30); // 13
                aiParams.RandomTimerMaxMove = 10; // 14
                aiParams.RandomTimerMinMove = 5; // 15
                aiParams.AwarenessFriendlyMine = 120; // 16
                aiParams.AwarenessFriendlyShell = 100; // 17
                aiParams.AwarenessHostileMine = 120; // 18
                aiParams.AwarenessHostileShell = 80; // 19
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 50; // 28
                aiParams.AimOffset = MathHelper.ToRadians(40); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 10; // 35
                aiParams.RandomTimerMinShoot = 5; // 36
                aiParams.TurretSpeed = 0.02f; // 39
                aiParams.TurretMovementTimer = 15; // 40
                aiParams.TankAwarenessShoot = 70; // 41


                aiParams.BaseXP = 0.16f;
                break;

            case TankID.Obsidian:
                aiParams.ObstacleAwarenessMine = 50; // 2
                aiParams.RandomTimerMaxMine = 60; // 4
                aiParams.RandomTimerMinMine = 40; // 5
                aiParams.TankAwarenessMine = 100; // 6
                aiParams.ChanceMineLayNearBreakables = 0.08f; // 7
                aiParams.ChanceMineLay = 0.05f; // 8
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30); // 13
                aiParams.RandomTimerMaxMove = 40; // 14
                aiParams.RandomTimerMinMove = 20; // 15
                aiParams.AwarenessFriendlyMine = 160; // 16
                aiParams.AwarenessFriendlyShell = 140; // 17
                aiParams.AwarenessHostileMine = 140; // 18
                aiParams.AwarenessHostileShell = 70; // 19
                aiParams.CantShootWhileFleeing = true; // 20
                aiParams.MaxQueuedMovements = 4; // 22
                aiParams.ObstacleAwarenessMovement = 75; // 28
                aiParams.AimOffset = MathHelper.ToRadians(20); // 29
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5); // 31 
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20); // 32 
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20); // 33
                aiParams.RandomTimerMaxShoot = 10; // 35
                aiParams.RandomTimerMinShoot = 5; // 36
                aiParams.TurretSpeed = 0.05f; // 39
                aiParams.TurretMovementTimer = 20; // 40
                aiParams.TankAwarenessShoot = 70; // 41

                aiParams.BaseXP = 0.175f;
                break;

            #endregion 
        }

        return aiParams;
    }
    public static TankProperties GetAITankProperties(int tankType) {
        var properties = new TankProperties();

        /*if (!AIPropertyDefaults.TryGetValue(tankType, out TankProperties? value)) {
            var json = File.ReadAllText("ai/tank_" + TankID.Collection.GetKey(tankType) + ".json");
            using JsonDocument doc = JsonDocument.Parse(json);

            var parameters = doc.RootElement.GetProperty("Properties");
            AIPropertyDefaults.Add(tankType, parameters.Deserialize<TankProperties>()!);
            return AIPropertyDefaults[tankType];
        }
        else {
            return value;
        }*/

        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                properties.Invisible = false;
                properties.Stationary = true;
                properties.ShellLimit = 1;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 300;
                properties.ShellSpeed = 3f;


                properties.ShellType = ShellID.Standard;
                properties.ShellHoming = new();
                break;

            case TankID.Ash:
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1.2f;
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 1;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 180;
                properties.ShellSpeed = 3f;
                properties.ShootStun = 10;




                properties.ShellType = ShellID.Standard;
                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();
                properties.TreadPitch = 0.085f;
                break;

            case TankID.Marine:
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1f;
                properties.TurningSpeed = 0.2f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 1;
                properties.ShellCooldown = 180;
                properties.ShellSpeed = 6f;
                properties.ShootStun = 20;



                properties.ShellType = ShellID.Rocket;
                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();
                properties.TreadPitch = 0.085f;
                break;

            case TankID.Yellow:
                properties.MineLimit = 4;
                properties.MineStun = 1;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1.8f;
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 1;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 180;
                properties.ShellSpeed = 3f;
                properties.ShootStun = 10;


                properties.ShellType = ShellID.Standard;
                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();
                properties.TreadPitch = 0.085f;

                if (Difficulties.Types["PieFactory"]) {
                    properties.InvulnerableToMines = true;
                    properties.MineCooldown = 10;
                    properties.MineLimit = 20;
                    properties.MineStun = 0;
                }

                break;

            case TankID.Pink:
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1.2f;
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 3;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 30;
                properties.ShellSpeed = 3f;
                properties.ShootStun = 5;



                properties.ShellType = ShellID.Standard;
                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();
                properties.TreadPitch = 0.1f;
                break;

            case TankID.Violet:
                properties.MineLimit = 2;
                properties.MineStun = 1;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1.8f;
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 5;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 30;
                properties.ShellSpeed = 3f;
                properties.ShootStun = 5;


                properties.ShellType = ShellID.Standard;
                properties.ShellHoming = new();
                properties.TreadPitch = -0.2f;
                break;

            case TankID.Green:
                properties.ShellLimit = 2;
                properties.RicochetCount = 2;
                properties.ShellCooldown = 60;
                properties.ShellSpeed = 6f;


                properties.ShellType = ShellID.TrailedRocket;
                properties.Stationary = true;
                properties.ShellHoming = new();
                break;

            case TankID.White:
                properties.Invisible = true;
                properties.MineLimit = 2;
                properties.MineStun = 1;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 1.2f;
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);
                properties.ShellLimit = 5;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 30;
                properties.ShellSpeed = 3f;
                properties.ShootStun = 5;





                properties.TrackType = TrackID.Thick;
                properties.ShellType = ShellID.Standard;
                properties.ShellHoming = new();
                properties.TreadPitch = -0.35f;
                break;

            case TankID.Black:
                properties.MineLimit = 2;
                properties.MineStun = 1;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;
                properties.MaxSpeed = 2.4f;
                properties.TurningSpeed = 0.06f;
                properties.MaximalTurn = MathHelper.ToRadians(5);
                properties.ShellLimit = 3;
                properties.ShellCooldown = 60;
                properties.ShellSpeed = 6f;
                properties.ShootStun = 10;





                properties.ShellType = ShellID.Rocket;
                properties.ShellHoming = new();
                properties.TreadPitch = -0.26f;
                properties.ShootPitch = -0.2f;
                break;

            #endregion

            #region MasterMod

            case TankID.Bronze:
                properties.ShellLimit = 4; // 30
                properties.RicochetCount = 1; // 34
                properties.ShellCooldown = 40; // 37
                properties.ShellSpeed = 4f; // 38

                properties.ShellType = ShellID.Standard;
                properties.Stationary = true;
                break;
            case TankID.Silver:
                properties.MineLimit = 1; // 3
                properties.MineStun = 5; // 10
                properties.Acceleration = 0.3f; // 11
                properties.Deceleration = 0.6f; // 12
                properties.MaxSpeed = 1.6f; // 23
                properties.TurningSpeed = 0.13f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(15); // 27 
                properties.ShellLimit = 8; // 30
                properties.RicochetCount = 1; // 34
                properties.ShellCooldown = 15; // 37
                properties.ShellSpeed = 4f; // 38
                properties.ShootStun = 0; // 42


                properties.ShellType = ShellID.Standard;
                properties.TreadPitch = 0.2f;
                properties.MaxSpeed = 1.6f;
                break;
            case TankID.Sapphire:
                properties.MineLimit = 1; // 3
                properties.MineStun = 0; // 10
                properties.Acceleration = 0.3f; // 11
                properties.Deceleration = 0.6f; // 12
                properties.MaxSpeed = 1.4f; // 23
                properties.TurningSpeed = 0.15f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(20); // 27 
                properties.ShellLimit = 3; // 30
                properties.ShellCooldown = 10; // 37
                properties.ShellSpeed = 5.5f; // 38
                properties.ShootStun = 20; // 42


                properties.ShellType = ShellID.Rocket;
                properties.TreadPitch = 0.08f;
                break;
            case TankID.Ruby:
                properties.Acceleration = 0.4f; // 11
                properties.Deceleration = 0.6f; // 12
                properties.MaxSpeed = 1.2f; // 23
                properties.TurningSpeed = 0.5f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(45); // 27 
                properties.ShellLimit = 10; // 30
                properties.ShellCooldown = 8; // 37
                properties.ShellSpeed = 3f; // 38


                properties.ShellType = ShellID.Standard;
                properties.TreadPitch = 0.08f;
                break;
            case TankID.Citrine:
                properties.MineLimit = 4; // 3
                properties.MineStun = 1; // 10
                properties.Acceleration = 0.2f; // 11
                properties.Deceleration = 0.4f; // 12
                properties.MaxSpeed = 3.2f; // 23
                properties.TurningSpeed = 0.08f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(40); // 27 
                properties.ShellLimit = 3; // 30
                properties.RicochetCount = 0; // 34
                properties.ShellCooldown = 60; // 37
                properties.ShellSpeed = 6f; // 38
                properties.ShootStun = 10; // 42

                properties.ShellType = ShellID.Standard;
                properties.TreadPitch = -0.08f;
                break;
            case TankID.Amethyst:
                properties.MineLimit = 3;
                properties.MineStun = 10;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.9f;
                properties.MaxSpeed = 2f;
                properties.TurningSpeed = 0.16f;
                properties.MaximalTurn = MathHelper.ToRadians(30);
                properties.ShellLimit = 5;
                properties.RicochetCount = 1;
                properties.ShellCooldown = 25;
                properties.ShellSpeed = 3.5f;
                properties.ShootStun = 5;


                properties.ShellType = ShellID.Standard;
                properties.TreadPitch = -0.2f;
                break;
            case TankID.Emerald:
                properties.ShellLimit = 3; // 30
                properties.RicochetCount = 2; // 34
                properties.ShellCooldown = 60; // 37
                properties.ShellSpeed = 8f; // 38


                properties.ShellType = ShellID.TrailedRocket;
                properties.Stationary = true;
                properties.Invisible = true;
                break;

            case TankID.Gold:
                properties.MineLimit = 2; // 3
                properties.MineStun = 1; // 10
                properties.Acceleration = 0.8f; // 11
                properties.Deceleration = 0.5f; // 12
                properties.MaxSpeed = 0.9f; // 23
                properties.TurningSpeed = 0.06f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(15); // 27 
                properties.ShellLimit = 3; // 30
                properties.RicochetCount = 1; // 34
                properties.ShellCooldown = 30; // 37
                properties.ShellSpeed = 4f; // 38
                properties.ShootStun = 5; // 42

                properties.CanLayTread = false;
                properties.ShellType = ShellID.Standard;
                properties.TreadPitch = -0.1f;
                properties.Invisible = true;
                break;

            case TankID.Obsidian:
                properties.MineLimit = 2; // 3
                properties.MineStun = 1; // 10
                properties.Acceleration = 0.6f; // 11
                properties.Deceleration = 0.8f; // 12
                properties.MaxSpeed = 3f; // 23
                properties.TurningSpeed = 0.1f; // 26
                properties.MaximalTurn = MathHelper.ToRadians(20); // 27 
                properties.ShellLimit = 3; // 30
                properties.RicochetCount = 2; // 34
                properties.ShellCooldown = 25; // 37
                properties.ShellSpeed = 8.5f; // 38
                properties.ShootStun = 0; // 42


                properties.ShellType = ShellID.Rocket;
                properties.TreadPitch = -0.26f;
                break;

                #endregion
        }
        return properties;
    }
    /// <summary>
    /// Gets the highest tier that is present in-game, following the pattern you give.
    /// </summary>
    /// <param name="predicate">The pattern to take account for when searching. If null, just finds the highest active.</param>
    /// <returns></returns>
    public static int GetHighestTierActive(Func<AITank, bool>? predicate = null) {
        var highest = TankID.None;

        Span<AITank> tanks = GameHandler.AllAITanks;
        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);

        for (var i = 0; i < GameHandler.AllAITanks.Length; i++) {
            var tank = Unsafe.Add(ref tanksSearchSpace, i);

            if (tank is null || tank.IsDestroyed || predicate is not null && !predicate.Invoke(tank)) continue;

            if (tank.AiTankType > highest)
                highest = tank.AiTankType;
        }

        return highest;
    }
    /// <summary>
    /// Counts every AI tank present in-game, following the pattern you give.
    /// </summary>
    /// <param name="predicate">The pattern to take account for when searching. If null, just counts every AI tank.</param>
    /// <returns></returns>
    public static int CountAll(Func<AITank, bool>? predicate = null) {
        var cnt = 0;
        Span<AITank> tanks = GameHandler.AllAITanks;

        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        for (var i = 0; i < tanks.Length; i++) {
            var tank = Unsafe.Add(ref tanksSearchSpace, i);
            if (tank is not null && !tank.IsDestroyed && (predicate is not null && predicate.Invoke(tank) || predicate is null)) cnt++;
        }

        return cnt;
    }
    /// <summary>
    /// Counts all tanks of a given tank type.
    /// </summary>
    /// <param name="tier">The tank type to search for and count.</param>
    /// <returns></returns>
    public static int GetTankCountOfType(int tier) {
        var cnt = 0;
        Span<AITank> tanks = GameHandler.AllAITanks;

        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        for (var i = 0; i < tanks.Length; i++) {
            var tnk = Unsafe.Add(ref tanksSearchSpace, i);
            if (tnk is not null && tnk.AiTankType == tier && !tnk.IsDestroyed) cnt++;
        }

        return cnt;
    }

    public const int SECTION_1 = 20;
    public const int SECTION_2 = 40;
    public const int SECTION_3 = 60;
    //static Stopwatch s = new();
    //static List<double> msList = [];

    public static Thread AIThread1 { get; } = new Thread(ProcessAISection1) {
        Name = "AIThread1",
        IsBackground = true,
        Priority = ThreadPriority.AboveNormal
    };
    public static Thread AIThread2 { get; } = new Thread(ProcessAISection2) {
        Name = "AIThread2",
        IsBackground = true,
        Priority = ThreadPriority.AboveNormal
    };
    public static Thread AIThread3 { get; } = new Thread(ProcessAISection3) {
        Name = "AIThread3",
        IsBackground = true,
        Priority = ThreadPriority.AboveNormal
    };
    public static bool RunThreads = true;
    internal static void UpdateAITanks() {

        if (ModLoader.Status != LoadStatus.Complete)
            return;

        if (!GameScene.ShouldRenderAll)
            return;

        //if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.H)) {
        //    msList = [];
        //    msList.Clear();
        //}

        Span<AITank> aiTanks = GameHandler.AllAITanks;
        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(aiTanks);
        for (var i = 0; i < aiTanks.Length; i++) {
            var tank = Unsafe.Add(ref tanksSearchSpace, i);
            if (tank is null || tank.IsDestroyed) continue;

            tank.Update();
            tank.HandleTankMetaData();
        }

        // if you're not connected to a server or you aren't *the* server
        // if (!Client.IsHost() && Client.IsConnected()) return;

        /*Parallel.For(0, GameHandler.ActiveAITankCount, new ParallelOptions() {
            MaxDegreeOfParallelism = 3,
        }, i => {
            var tank = GameHandler.AllAITanks[i];
            if (tank.IsDestroyed) return;

            tank.DoAI();

            // only does anything if you're in a multiplayer context.
            Client.SyncAITank(tank);
        });*/

        // ProcessAI();
        //s.Restart();
        //s.Stop();
        //double ms = (s.ElapsedTicks * 1_000_000.0) / Stopwatch.Frequency / 1000;
        //Console.WriteLine($"AI time this frame: {ms:0.000}ms | Average: {(msList.Sum() / msList.Count):0.000}");

        //msList?.Add(ms);
    }
    public static void ProcessAISection1() {
        while (RunThreads) {
            int sleepTime = TankGame.LastGameTime is null ? 1 : Math.Max(1, (int)TankGame.LastGameTime.ElapsedGameTime.TotalMilliseconds);
            Thread.Sleep(sleepTime);

            if (GameHandler.ActiveAITankCount == 0) continue;
            if ((!TankGame.Instance.IsActive || GameUI.Paused) && !Client.IsConnected()) continue;
            if (!Client.IsHost() && Client.IsConnected()) continue;

            Span<AITank> aiTanks = GameHandler.AllAITanks;
            ref var tanksSearchSpace = ref MemoryMarshal.GetReference(aiTanks);

            // start of the array to SECTION_1
            for (var i = 0; i < SECTION_1; i++) {
                var tank = Unsafe.Add(ref tanksSearchSpace, i);
                if (tank is null || tank.IsDestroyed) continue;

                tank.DoAI();

                // only does anything if you're in a multiplayer context.
                Client.SyncAITank(tank);
            }
        }
    }
    public static void ProcessAISection2() {
        while (RunThreads) {
            int sleepTime = TankGame.LastGameTime is null ? 1 : Math.Max(1, (int)TankGame.LastGameTime.ElapsedGameTime.TotalMilliseconds);
            Thread.Sleep(sleepTime);

            if (GameHandler.ActiveAITankCount < SECTION_1) continue;
            if ((!TankGame.Instance.IsActive || GameUI.Paused) && !Client.IsConnected()) continue;
            if (!Client.IsHost() && Client.IsConnected()) continue;

            Span<AITank> aiTanks = GameHandler.AllAITanks;
            ref var tanksSearchSpace = ref MemoryMarshal.GetReference(aiTanks);

            // SECTION_1 to SECTION_2
            for (var i = SECTION_1; i < SECTION_2; i++) {
                var tank = Unsafe.Add(ref tanksSearchSpace, i);
                if (tank is null || tank.IsDestroyed) continue;

                tank.DoAI();

                // only does anything if you're in a multiplayer context.
                Client.SyncAITank(tank);
            }
        }
    }
    public static void ProcessAISection3() {
        while (RunThreads) {
            int sleepTime = TankGame.LastGameTime is null ? 1 : Math.Max(1, (int)TankGame.LastGameTime.ElapsedGameTime.TotalMilliseconds);
            Thread.Sleep(sleepTime);

            if (GameHandler.ActiveAITankCount < SECTION_2) continue;
            if ((!TankGame.Instance.IsActive || GameUI.Paused) && !Client.IsConnected()) continue;
            if (!Client.IsHost() && Client.IsConnected()) continue;

            Span<AITank> aiTanks = GameHandler.AllAITanks;
            ref var tanksSearchSpace = ref MemoryMarshal.GetReference(aiTanks);

            // SECTION_2 to the end of the array
            for (var i = SECTION_2; i < aiTanks.Length; i++) {
                var tank = Unsafe.Add(ref tanksSearchSpace, i);
                if (tank is null || tank.IsDestroyed) continue;

                tank.DoAI();

                // only does anything if you're in a multiplayer context.
                Client.SyncAITank(tank);
            }
        }
    }
}