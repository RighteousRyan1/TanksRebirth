using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.GameContent;

public static class AIManager {
    /// <summary>
    /// Fetch the default AI parameters from the given input tank type.
    /// </summary>
    /// <param name="tankType">The type of the tank to retrieve the defaults from.</param>
    /// <param name="baseExp">The base experience the player will gain upon killing the AI tank.</param>
    /// <returns></returns>
    public static AiParameters GetAiDefaults(int tankType, out float baseExp) {
        var aiParams = new AiParameters();

        baseExp = 0f;

        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                aiParams.DetectionForgivenessHostile = 1.6f;
                aiParams.AimOffset = MathHelper.ToRadians(170);
                aiParams.RandomTimerMaxShoot = 45;
                aiParams.RandomTimerMinShoot = 30;
                aiParams.TurretSpeed = 0.01f;
                aiParams.TurretMovementTimer = 60;
                aiParams.DetectionRadiusShellFriendly = 70;



                baseExp = 0.01f;
                break;

            case TankID.Ash:
                aiParams.MineObstacleAwareness = 100;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 15;
                aiParams.RandomTimerMinMove = 10;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
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
                aiParams.DetectionRadiusShellFriendly = 70;




                aiParams.RandomTimerMinMove = 15;

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessFriendlyShell = 70;
                aiParams.AwarenessHostileMine = 40;
                aiParams.AwarenessFriendlyMine = 70;

                baseExp = 0.015f;
                break;

            case TankID.Marine:
                aiParams.MineObstacleAwareness = 100;
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
                aiParams.DetectionRadiusShellFriendly = 70;





                aiParams.RandomTimerMinMove = 10;
                aiParams.AimOffset = 0; // no extra math

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessHostileMine = 80;

                aiParams.CantShootWhileFleeing = false;

                baseExp = 0.04f;
                break;

            case TankID.Yellow:
                aiParams.MineObstacleAwareness = 100;
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
                aiParams.DetectionRadiusShellFriendly = 70;



                aiParams.RandomTimerMinMove = 15;

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessHostileMine = 160;

                aiParams.ChanceMineLay = 0.3f;

                aiParams.CantShootWhileFleeing = false;

                baseExp = 0.035f;

                if (Difficulties.Types["PieFactory"]) {
                    aiParams.ChanceMineLay = 1f;
                    aiParams.AwarenessHostileMine = 0;
                }

                break;

            case TankID.Pink:
                aiParams.MineObstacleAwareness = 100;
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMaxMove = 10;
                aiParams.RandomTimerMinMove = 5;
                aiParams.AwarenessFriendlyMine = 120;
                aiParams.AwarenessFriendlyShell = 120;
                aiParams.AwarenessHostileShell = 40;
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
                aiParams.DetectionRadiusShellFriendly = 70;


                aiParams.RandomTimerMinMove = 10;

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessHostileMine = 160;

                aiParams.CantShootWhileFleeing = true;

                baseExp = 0.08f;
                break;

            case TankID.Violet:
                aiParams.MineObstacleAwareness = 200;
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
                aiParams.DetectionRadiusShellFriendly = 70;




                aiParams.RandomTimerMinMove = 10;

                aiParams.AwarenessHostileShell = 60;
                aiParams.AwarenessHostileMine = 160;

                aiParams.ChanceMineLay = 0.05f;

                aiParams.CantShootWhileFleeing = false;

                baseExp = 0.1f;
                break;

            case TankID.Green:
                aiParams.MineObstacleAwareness = 100;
                aiParams.AwarenessHostileShell = 70;
                aiParams.AimOffset = MathHelper.ToRadians(80);
                aiParams.DetectionForgivenessSelf = MathHelper.ToRadians(5);
                aiParams.DetectionForgivenessFriendly = MathHelper.ToRadians(20);
                aiParams.DetectionForgivenessHostile = MathHelper.ToRadians(20);
                aiParams.RandomTimerMaxShoot = 10;
                aiParams.RandomTimerMinShoot = 5;
                aiParams.TurretSpeed = 0.02f;
                aiParams.TurretMovementTimer = 30;
                aiParams.DetectionRadiusShellFriendly = 70;




                aiParams.CantShootWhileFleeing = true;

                baseExp = 0.12f;
                break;

            case TankID.White:
                aiParams.MineObstacleAwareness = 200;
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
                aiParams.DetectionRadiusShellFriendly = 70;


                aiParams.RandomTimerMinMove = 10;

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessHostileMine = 160;

                aiParams.ChanceMineLay = 0.08f;

                aiParams.CantShootWhileFleeing = false;

                baseExp = 0.125f;
                break;

            case TankID.Black:
                aiParams.MineObstacleAwareness = 200;
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



                aiParams.RandomTimerMinMove = 10;

                aiParams.AwarenessHostileShell = 100;
                aiParams.AwarenessHostileMine = 110;

                aiParams.ChanceMineLay = 0.05f;

                aiParams.DetectionRadiusShellFriendly = 70;

                aiParams.CantShootWhileFleeing = true;

                baseExp = 0.145f;

                break;

            #endregion

            #region MasterMod

            case TankID.Bronze:
                aiParams.TurretMovementTimer = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.005f;

                aiParams.AwarenessHostileShell = 140;

                aiParams.DetectionForgivenessHostile = 0.2f;

                aiParams.ChanceMineLay = 0.05f;

                baseExp = 0.025f;
                break;
            case TankID.Silver:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 10;
                aiParams.TurretMovementTimer = 60;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = 0.9f;

                aiParams.DetectionForgivenessHostile = 0.4f;

                aiParams.AggressivenessBias = 0.08f;

                aiParams.AwarenessHostileShell = 70;
                aiParams.AwarenessHostileMine = 140;

                aiParams.ChanceMineLay = 0.05f;

                baseExp = 0.07f;
                break;
            case TankID.Sapphire:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 15;
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.025f;
                aiParams.AimOffset = 0.01f;

                aiParams.DetectionForgivenessHostile = 0.4f;

                aiParams.AggressivenessBias = -0.1f;

                aiParams.AwarenessHostileShell = 40;
                aiParams.AwarenessHostileMine = 70;

                aiParams.ChanceMineLay = 0.05f;

                baseExp = 0.095f;
                break;
            case TankID.Ruby:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 10;
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.025f;
                aiParams.AimOffset = 0.05f;

                aiParams.DetectionForgivenessHostile = 0.6f;

                aiParams.AggressivenessBias = 0.3f;

                aiParams.AwarenessHostileShell = 50;
                aiParams.AwarenessHostileMine = 70;

                aiParams.ChanceMineLay = 0;

                aiParams.ObstacleAwarenessMovement = 30;

                baseExp = 0.13f;
                break;
            case TankID.Citrine:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 30;
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.035f;
                aiParams.AimOffset = 0.3f;

                aiParams.DetectionForgivenessHostile = 0.25f;

                aiParams.AggressivenessBias = 0.5f;

                aiParams.AwarenessHostileShell = 80;
                aiParams.AwarenessHostileMine = 140;

                aiParams.ChanceMineLay = 0.15f;

                aiParams.ObstacleAwarenessMovement = 60;

                baseExp = 0.09f;
                break;
            case TankID.Amethyst:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 5;
                aiParams.TurretMovementTimer = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.3f;

                aiParams.DetectionForgivenessHostile = 0.65f;

                aiParams.AggressivenessBias = 0.03f;

                aiParams.AwarenessHostileShell = 70;
                aiParams.AwarenessHostileMine = 140;

                aiParams.ChanceMineLay = 0.05f;

                baseExp = 0.095f;
                break;
            case TankID.Emerald:
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.04f;
                aiParams.AimOffset = 1f;

                aiParams.DetectionForgivenessHostile = 0.35f;

                aiParams.SmartRicochets = true;

                baseExp = 0.14f;
                break;

            case TankID.Gold:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 20;
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = 0.14f;

                aiParams.DetectionForgivenessHostile = 0.4f;

                aiParams.AggressivenessBias = 0.2f;

                aiParams.AwarenessHostileShell = 80;
                aiParams.AwarenessHostileMine = 120;

                aiParams.ChanceMineLay = 0.01f;

                baseExp = 0.16f;
                break;

            case TankID.Obsidian:
                aiParams.MaxAngleRandomTurn = MathHelper.ToRadians(30);
                aiParams.RandomTimerMinMove = 20;
                aiParams.TurretMovementTimer = 20;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.18f;

                aiParams.DetectionForgivenessHostile = 0.9f;

                aiParams.AggressivenessBias = 0.15f;

                aiParams.AwarenessHostileShell = 70;
                aiParams.AwarenessHostileMine = 140;

                aiParams.ChanceMineLay = 0.1f;

                aiParams.ObstacleAwarenessMovement = 50;

                baseExp = 0.175f;
                break;

            #endregion 
        }
        if (aiParams.AwarenessFriendlyShell == 0)
            aiParams.AwarenessFriendlyShell = aiParams.AwarenessHostileShell;
        if (aiParams.AwarenessFriendlyMine == 0)
            aiParams.AwarenessFriendlyMine = aiParams.AwarenessHostileMine;

        return aiParams;
    }
    public static TankProperties GetAITankProperties(int tankType) {
        var properties = new TankProperties();
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
                properties.DestructionColor = new(152, 96, 26);

                properties.ShellCooldown = 50;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = true;
                properties.ShellHoming = new();
                break;
            case TankID.Silver:
                properties.TurningSpeed = 0.13f;
                properties.MaximalTurn = MathHelper.PiOver2;

                properties.ShootStun = 0;
                properties.ShellCooldown = 15;
                properties.ShellLimit = 8;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.2f;
                properties.MaxSpeed = 1.6f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 60 * 20;
                properties.MineLimit = 1;
                properties.MineStun = 10;
                break;
            case TankID.Sapphire:
                properties.TurningSpeed = 0.15f;
                properties.MaximalTurn = MathHelper.PiOver2;

                properties.ShootStun = 20;
                properties.ShellCooldown = 10;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 5.5f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.4f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 1000;
                properties.MineLimit = 1;
                properties.MineStun = 0;
                break;
            case TankID.Ruby:
                properties.TurningSpeed = 0.5f;
                properties.MaximalTurn = MathHelper.PiOver2;

                properties.ShootStun = 0;
                properties.ShellCooldown = 8;
                properties.ShellLimit = 10;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.2f; // 1.2
                properties.Acceleration = 0.4f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;
            case TankID.Citrine:
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = 1.4f;

                properties.ShootStun = 10;
                properties.ShellCooldown = 60;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 6f; // 6
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.08f;
                properties.MaxSpeed = 3.2f; // 3.2
                properties.Acceleration = 0.2f;
                properties.Deceleration = 0.4f;

                properties.MineCooldown = 360;
                properties.MineLimit = 4;
                properties.MineStun = 5;
                break;
            case TankID.Amethyst:
                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = MathHelper.ToRadians(30);

                properties.ShootStun = 5;
                properties.ShellCooldown = 25;
                properties.ShellLimit = 5;
                properties.ShellSpeed = 3.5f; // 3.5
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1; // 1

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.2f;
                properties.MaxSpeed = 2f; // 2
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.9f;

                properties.MineCooldown = 360;
                properties.MineLimit = 3;
                properties.MineStun = 10;
                break;
            case TankID.Emerald:
                properties.ShellCooldown = 60;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 8f;
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 2;

                properties.Stationary = true;
                properties.Invisible = true;
                properties.ShellHoming = new();
                break;

            case TankID.Gold:
                properties.CanLayTread = false;

                properties.TurningSpeed = 0.06f;
                properties.MaximalTurn = 1.4f;

                properties.ShootStun = 5;
                properties.ShellCooldown = 30;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.1f;
                properties.MaxSpeed = 0.9f;
                properties.Acceleration = 0.8f;
                properties.Deceleration = 0.5f;

                properties.MineCooldown = 700;
                properties.MineLimit = 2;
                properties.MineStun = 10;

                properties.Invisible = true;
                break;

            case TankID.Obsidian:
                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = MathHelper.PiOver4;

                properties.ShootStun = 5;
                properties.ShellCooldown = 25;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 8.5f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 2;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.26f;
                properties.MaxSpeed = 3f;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.8f;

                properties.MineCooldown = 850;
                properties.MineLimit = 2;
                properties.MineStun = 10;
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

            if (tank is null || tank.Dead || (predicate is not null && !predicate.Invoke(tank))) continue;

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
            if (tank is not null && !tank.Dead && ((predicate is not null && predicate.Invoke(tank)) || predicate is null)) cnt++;
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
            if (tnk is not null && tnk.AiTankType == tier && !tnk.Dead) cnt++;
        }

        return cnt;
    }
}