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
    /// <param name="tank">The AI tank to set the defaults of.</param>
    /// <param name="tankType">The type of the tank to retrieve the defaults from.</param>
    /// <param name="baseExp">The base experience the player will gain upon killing the AI tank.</param>
    /// <returns></returns>
    public static AiParameters GetAiDefaults(int tankType, out float baseExp) {
        var aiParams = new AiParameters();

        baseExp = 0f;

        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                aiParams.ProjectileWarinessRadius_PlayerShot = 60;

                aiParams.TurretMeanderFrequency = 30;
                aiParams.TurretSpeed = 0.01f;
                aiParams.AimOffset = MathHelper.ToRadians(170);
                aiParams.Inaccuracy = 1.6f;

                baseExp = 0.01f;
                break;

            case TankID.Ash:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 40;
                aiParams.TurretSpeed = 0.01f;
                aiParams.AimOffset = MathHelper.ToRadians(40);

                aiParams.Inaccuracy = 0.9f;

                aiParams.PursuitLevel = 0.4f;
                aiParams.PursuitFrequency = 300;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.ProjectileWarinessRadius_AIShot = 70;
                aiParams.MineWarinessRadius_PlayerLaid = 40;
                aiParams.MineWarinessRadius_AILaid = 70;

                aiParams.BlockWarinessDistance = 25;

                baseExp = 0.015f;
                break;

            case TankID.Marine:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 10;
                aiParams.TurretSpeed = 0.1f;
                aiParams.AimOffset = MathHelper.ToRadians(0);

                aiParams.Inaccuracy = 0.15f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 80;

                aiParams.PursuitFrequency = 180;
                aiParams.PursuitLevel = -0.6f;

                aiParams.BlockWarinessDistance = 40;

                baseExp = 0.04f;
                break;

            case TankID.Yellow:
                aiParams.MeanderAngle = MathHelper.ToRadians(40);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = 0.5f;

                aiParams.Inaccuracy = 1.5f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

                aiParams.MinePlacementChance = 0.3f;

                baseExp = 0.035f;

                if (Difficulties.Types["PieFactory"]) {
                    aiParams.MinePlacementChance = 1f;
                    aiParams.MineWarinessRadius_PlayerLaid = 0;
                }

                break;

            case TankID.Pink:
                aiParams.MeanderAngle = MathHelper.ToRadians(40);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 40;
                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = 0.2f;

                aiParams.Inaccuracy = 1.3f;

                aiParams.PursuitLevel = 0.7f;
                aiParams.PursuitFrequency = 180;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

                aiParams.BlockWarinessDistance = 35;

                baseExp = 0.08f;
                break;

            case TankID.Violet:
                aiParams.MeanderAngle = MathHelper.ToRadians(40);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 25;

                aiParams.Inaccuracy = 0.8f;

                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = 0.18f;

                aiParams.PursuitLevel = 0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 60;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

                aiParams.MinePlacementChance = 0.05f;

                aiParams.BlockWarinessDistance = 45;

                baseExp = 0.1f;
                break;

            case TankID.Green:
                aiParams.ProjectileWarinessRadius_PlayerShot = 70;

                aiParams.TurretMeanderFrequency = 30;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = MathHelper.ToRadians(80);
                aiParams.Inaccuracy = MathHelper.ToRadians(25);

                baseExp = 0.12f;
                break;

            case TankID.White:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = MathHelper.ToRadians(40);

                aiParams.Inaccuracy = 0.8f;

                aiParams.PursuitLevel = 0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

                aiParams.MinePlacementChance = 0.08f;

                aiParams.BlockWarinessDistance = 40; // used to be 30 but it's short

                baseExp = 0.125f;
                break;

            case TankID.Black:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = MathHelper.ToRadians(5);

                aiParams.Inaccuracy = 0.35f;

                aiParams.PursuitLevel = 0.8f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 100;
                aiParams.MineWarinessRadius_PlayerLaid = 110; // 60

                aiParams.MinePlacementChance = 0.05f;

                aiParams.BlockWarinessDistance = 60;

                baseExp = 0.145f;

                break;

            #endregion

            #region MasterMod

            case TankID.Bronze:
                aiParams.TurretMeanderFrequency = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.005f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;

                aiParams.Inaccuracy = 0.2f;

                aiParams.MinePlacementChance = 0.05f;

                baseExp = 0.025f;
                break;
            case TankID.Silver:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 60;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = 0.9f;

                aiParams.Inaccuracy = 0.4f;

                aiParams.PursuitLevel = 0.4f;
                aiParams.PursuitFrequency = 120;

                aiParams.ProjectileWarinessRadius_PlayerShot = 70;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                aiParams.MinePlacementChance = 0.05f;

                baseExp = 0.07f;
                break;
            case TankID.Sapphire:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.025f;
                aiParams.AimOffset = 0.01f;

                aiParams.Inaccuracy = 0.4f;

                aiParams.PursuitLevel = -0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 70;

                aiParams.MinePlacementChance = 0.05f;

                baseExp = 0.095f;
                break;
            case TankID.Ruby:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.025f;
                aiParams.AimOffset = 0.05f;

                aiParams.Inaccuracy = 0.6f;

                aiParams.PursuitLevel = 0.9f;
                aiParams.PursuitFrequency = 360;

                aiParams.ProjectileWarinessRadius_PlayerShot = 50;
                aiParams.MineWarinessRadius_PlayerLaid = 70;

                aiParams.MinePlacementChance = 0;

                aiParams.BlockWarinessDistance = 30;

                baseExp = 0.13f;
                break;
            case TankID.Citrine:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 30;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.035f;
                aiParams.AimOffset = 0.3f;

                aiParams.Inaccuracy = 0.25f;

                // max aggression from blud lmfao
                aiParams.PursuitLevel = 1f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 80;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                aiParams.MinePlacementChance = 0.15f;

                aiParams.ShootChance = 0.95f;

                aiParams.BlockWarinessDistance = 60;

                baseExp = 0.09f;
                break;
            case TankID.Amethyst:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 5;
                aiParams.TurretMeanderFrequency = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.3f;

                aiParams.Inaccuracy = 0.65f;

                aiParams.PursuitLevel = 0.25f;
                aiParams.PursuitFrequency = 180;

                aiParams.ProjectileWarinessRadius_PlayerShot = 70;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                aiParams.MinePlacementChance = 0.05f;

                baseExp = 0.095f;
                break;
            case TankID.Emerald:
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.04f;
                aiParams.AimOffset = 1f;

                aiParams.Inaccuracy = 0.35f;

                aiParams.SmartRicochets = true;

                baseExp = 0.14f;
                break;

            case TankID.Gold:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 20;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = 0.14f;

                aiParams.Inaccuracy = 0.4f;

                aiParams.PursuitLevel = 0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ShootChance = 0.7f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 80;
                aiParams.MineWarinessRadius_PlayerLaid = 120;

                aiParams.MinePlacementChance = 0.01f;

                baseExp = 0.16f;
                break;

            case TankID.Obsidian:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 20;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.18f;

                aiParams.Inaccuracy = 0.9f;

                aiParams.PursuitLevel = 0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 70;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                aiParams.MinePlacementChance = 0.1f;

                aiParams.BlockWarinessDistance = 50;
                aiParams.BlockReadTime = 10;

                baseExp = 0.175f;
                break;

            #endregion 
        }
        if (aiParams.ProjectileWarinessRadius_AIShot == 0)
            aiParams.ProjectileWarinessRadius_AIShot = aiParams.ProjectileWarinessRadius_PlayerShot;
        if (aiParams.MineWarinessRadius_AILaid == 0)
            aiParams.MineWarinessRadius_AILaid = aiParams.MineWarinessRadius_PlayerLaid;

        return aiParams;
    }
    public static TankProperties GetAITankProperties(int tankType) {
        var properties = new TankProperties();
        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                properties.Stationary = true;

                properties.TurningSpeed = 0f;
                properties.MaximalTurn = 0;

                properties.ShootStun = 20;
                properties.ShellCooldown = 300;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0;
                properties.MaxSpeed = 0f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;

            case TankID.Ash:
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 3;
                properties.ShellCooldown = 180;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.085f;
                properties.MaxSpeed = 1.2f; // 1.2

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;

            case TankID.Marine:
                properties.TurningSpeed = 0.2f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 20;
                properties.ShellCooldown = 180;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 6f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.085f;
                properties.MaxSpeed = 1f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;

            case TankID.Yellow:
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 20;
                properties.ShellCooldown = 90;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.085f;
                properties.MaxSpeed = 1.8f;

                properties.MineCooldown = 600;
                properties.MineLimit = 4;
                properties.MineStun = 5;

                if (Difficulties.Types["PieFactory"]) {
                    properties.InvulnerableToMines = true;
                    properties.MineCooldown = 10;
                    properties.MineLimit = 20;
                    properties.MineStun = 0;
                }

                break;

            case TankID.Pink:
                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 5;
                properties.ShellCooldown = 30;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.1f;
                properties.MaxSpeed = 1.2f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;

            case TankID.Violet:
                properties.TurningSpeed = 0.06f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 5;
                properties.ShellCooldown = 30;
                properties.ShellLimit = 5;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.2f;
                properties.MaxSpeed = 1.8f;
                properties.Acceleration = 0.3f;

                properties.MineCooldown = 700;
                properties.MineLimit = 2;
                properties.MineStun = 10;
                break;

            case TankID.Green:
                properties.Stationary = true;

                properties.TurningSpeed = 0f;
                properties.MaximalTurn = 0;

                properties.ShootStun = 5;
                properties.ShellCooldown = 60;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 6f; // 6f
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 2; // 2

                properties.Invisible = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0;
                properties.MaxSpeed = 0f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;
                break;

            case TankID.White:
                properties.TrackType = TrackID.Thick;

                properties.TurningSpeed = 0.08f;
                properties.MaximalTurn = MathHelper.ToRadians(10);

                properties.ShootStun = 5;
                properties.ShellCooldown = 30;
                properties.ShellLimit = 5;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.35f;
                properties.MaxSpeed = 1.2f;
                properties.Acceleration = 0.3f;

                properties.MineCooldown = 1000;
                properties.MineLimit = 2;
                properties.MineStun = 8;
                properties.Invisible = true;
                break;

            case TankID.Black:
                properties.TurningSpeed = 0.06f;
                properties.MaximalTurn = MathHelper.ToRadians(5);

                properties.ShootStun = 5;
                properties.ShellCooldown = 60;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 6f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.26f;
                properties.MaxSpeed = 2.4f; // 2.4
                properties.Acceleration = 0.3f; //0.3

                properties.ShootPitch = -0.2f; // 0.2f

                properties.MineCooldown = 850;
                properties.MineLimit = 2;
                properties.MineStun = 10;
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