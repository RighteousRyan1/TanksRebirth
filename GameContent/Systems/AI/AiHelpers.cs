using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.GameContent;

public static class AiHelpers {
    public static AiParameters GetAiDefaults(this AITank tank, TankProperties properties, int tankType) {
        var baseExpValue = 0f;
        var aiParams = new AiParameters();
        switch (tankType) {
            #region VanillaTanks

            case TankID.Brown:
                properties.Stationary = true;

                aiParams.ProjectileWarinessRadius_PlayerShot = 60;

                aiParams.TurretMeanderFrequency = 30;
                aiParams.TurretSpeed = 0.01f;
                aiParams.AimOffset = MathHelper.ToRadians(170);
                aiParams.Inaccuracy = 1.6f;

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

                baseExpValue = 0.01f;

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

                aiParams.BlockWarinessDistance = 25;

                baseExpValue = 0.015f;
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

                aiParams.BlockWarinessDistance = 40;

                baseExpValue = 0.04f;
                break;

            case TankID.Yellow:
                aiParams.MeanderAngle = MathHelper.ToRadians(40);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = 0.5f;

                aiParams.Inaccuracy = 1.5f;

                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

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

                aiParams.MinePlacementChance = 0.3f;

                baseExpValue = 0.035f;

                if (Difficulties.Types["PieFactory"]) {
                    properties.VulnerableToMines = false;
                    properties.MineCooldown = 10;
                    properties.MineLimit = 20;
                    properties.MineStun = 0;
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

                aiParams.BlockWarinessDistance = 35;

                baseExpValue = 0.08f;
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

                aiParams.MinePlacementChance = 0.05f;

                aiParams.BlockWarinessDistance = 45;

                baseExpValue = 0.1f;
                break;

            case TankID.Green:
                properties.Stationary = true;

                aiParams.ProjectileWarinessRadius_PlayerShot = 70;

                aiParams.TurretMeanderFrequency = 30;
                aiParams.TurretSpeed = 0.02f;
                aiParams.AimOffset = MathHelper.ToRadians(80);
                aiParams.Inaccuracy = MathHelper.ToRadians(25);

                properties.TurningSpeed = 0f;
                properties.MaximalTurn = 0;

                properties.ShootStun = 5;
                properties.ShellCooldown = 60;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 6f; // 6f
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 6; // 2

                properties.Invisible = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0;
                properties.MaxSpeed = 0f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;

                baseExpValue = 0.12f;
                break;

            case TankID.White:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = MathHelper.ToRadians(40);

                properties.TrackType = TrackID.Thick;

                aiParams.Inaccuracy = 0.8f;

                aiParams.PursuitLevel = 0.6f;
                aiParams.PursuitFrequency = 240;

                aiParams.ProjectileWarinessRadius_PlayerShot = 40;
                aiParams.MineWarinessRadius_PlayerLaid = 160;

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

                aiParams.MinePlacementChance = 0.08f;

                aiParams.BlockWarinessDistance = 30;

                properties.Invisible = true;

                baseExpValue = 0.125f;
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

                aiParams.MinePlacementChance = 0.05f;

                aiParams.BlockWarinessDistance = 60;

                baseExpValue = 0.145f;

                break;

            #endregion

            #region MasterMod

            case TankID.Bronze:
                aiParams.TurretMeanderFrequency = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.005f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;

                aiParams.Inaccuracy = 0.2f;

                properties.DestructionColor = new(152, 96, 26);

                properties.ShellCooldown = 50;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = true;
                properties.ShellHoming = new();

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.025f;
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

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.07f;
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

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.095f;
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

                aiParams.MinePlacementChance = 0;

                aiParams.BlockWarinessDistance = 30;

                baseExpValue = 0.13f;
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

                aiParams.MinePlacementChance = 0.15f;

                aiParams.ShootChance = 0.95f;

                aiParams.BlockWarinessDistance = 60;

                baseExpValue = 0.09f;
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

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.095f;
                break;
            case TankID.Emerald:
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.04f;
                aiParams.AimOffset = 1f;

                aiParams.Inaccuracy = 0.35f;

                properties.ShellCooldown = 60;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 8f;
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 2;

                properties.Stationary = true;
                properties.Invisible = true;
                properties.ShellHoming = new();

                aiParams.SmartRicochets = true;

                baseExpValue = 0.14f;
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

                aiParams.MinePlacementChance = 0.01f;

                properties.Invisible = true;

                baseExpValue = 0.16f;
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

                aiParams.MinePlacementChance = 0.1f;

                aiParams.BlockWarinessDistance = 50;
                aiParams.BlockReadTime = 10;

                baseExpValue = 0.175f;
                break;

            #endregion

            #region MarbleMod

            case TankID.Granite:
                aiParams.MeanderAngle = 0.8f;
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.09f;
                aiParams.AimOffset = 0f;

                aiParams.Inaccuracy = 0.5f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 150;
                aiParams.MineWarinessRadius_PlayerLaid = 90;

                properties.TurningSpeed = 0.3f;
                properties.MaximalTurn = MathHelper.PiOver4;

                properties.ShootStun = 60;
                properties.ShellCooldown = 40;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 5f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.07f;
                properties.MaxSpeed = 0.9f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.4f;

                aiParams.SmartRicochets = true;

                baseExpValue = 0.02f;
                break;
            case TankID.Bubblegum:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = MathHelper.ToRadians(30);

                aiParams.Inaccuracy = 0.4f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.5f;

                properties.ShootStun = 0;
                properties.ShellCooldown = 15;
                properties.ShellLimit = 8;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.3f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 940;
                properties.MineLimit = 1;
                properties.MineStun = 5;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;

                baseExpValue = 0.035f;
                break;
            case TankID.Water:
                aiParams.MeanderAngle = 0.25f;
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 10;
                aiParams.TurretSpeed = 0.03f;
                aiParams.AimOffset = MathHelper.ToRadians(10);

                aiParams.Inaccuracy = 0.5f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 90;
                aiParams.MineWarinessRadius_PlayerLaid = 150;

                properties.TurningSpeed = 0.2f;
                properties.MaximalTurn = MathHelper.PiOver4;

                properties.ShootStun = 20;
                properties.ShellCooldown = 25;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 5.5f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.14f;
                properties.MaxSpeed = 1.7f;
                properties.Acceleration = 0.4f;
                properties.Deceleration = 0.6f;

                baseExpValue = 0.08f;
                break;
            case TankID.Crimson:
                aiParams.MeanderAngle = 0.12f;
                aiParams.MeanderFrequency = 8;
                aiParams.TurretMeanderFrequency = 60;
                aiParams.TurretSpeed = 0.07f;
                aiParams.AimOffset = 0.04f;

                aiParams.Inaccuracy = 0.2f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 50;
                aiParams.MineWarinessRadius_PlayerLaid = 50;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.5f;

                properties.ShootStun = 1;
                properties.ShellCooldown = 5;
                properties.ShellLimit = 5;
                properties.ShellSpeed = 3f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.3f;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.8f;

                properties.MineCooldown = 340;
                properties.MineLimit = 6;
                properties.MineStun = 3;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;

                baseExpValue = 0.095f;
                break;
            case TankID.Tiger:
                aiParams.MeanderAngle = 0.30f;
                aiParams.MeanderFrequency = 2;
                aiParams.TurretMeanderFrequency = 40;
                aiParams.TurretSpeed = 0.1f;
                aiParams.AimOffset = 0.12f;

                aiParams.Inaccuracy = 0.7f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 90;
                aiParams.MineWarinessRadius_PlayerLaid = 120;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = MathHelper.PiOver2;

                properties.ShootStun = 0;
                properties.ShellCooldown = 20;
                properties.ShellLimit = 4;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.14f;
                properties.MaxSpeed = 2f;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.8f;

                properties.MineCooldown = 1;
                properties.MineLimit = 10;
                properties.MineStun = 0;

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.1f;
                break;
            case TankID.Fade:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 8;
                aiParams.TurretMeanderFrequency = 40;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.22f;

                aiParams.Inaccuracy = 1.1f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 100;
                aiParams.MineWarinessRadius_PlayerLaid = 100;

                properties.TurningSpeed = 0.12f;
                properties.MaximalTurn = MathHelper.ToRadians(30);

                properties.ShootStun = 5;
                properties.ShellCooldown = 25;
                properties.ShellLimit = 5;
                properties.ShellSpeed = 3.5f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.2f;
                properties.MaxSpeed = 1.9f;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.9f;

                properties.MineCooldown = 680;
                properties.MineLimit = 3;
                properties.MineStun = 10;

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.105f;
                break;
            case TankID.Creeper:
                aiParams.MeanderAngle = 0.2f;
                aiParams.MeanderFrequency = 25;
                aiParams.TurretMeanderFrequency = 40;
                aiParams.TurretSpeed = 0.085f;
                aiParams.AimOffset = 1f;

                aiParams.SmartRicochets = true;

                aiParams.Inaccuracy = 0.6f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 150;
                aiParams.MineWarinessRadius_PlayerLaid = 110;

                properties.TurningSpeed = 0.3f;
                properties.MaximalTurn = MathHelper.PiOver4;

                properties.ShootStun = 20;
                properties.ShellCooldown = 60;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 8f;
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 3;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.07f;
                properties.MaxSpeed = 1f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.4f;

                baseExpValue = 0.17f;
                break;
            case TankID.Gamma:
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.08f;
                aiParams.AimOffset = 0.01f;

                aiParams.Inaccuracy = 0.15f;

                properties.Invisible = false;
                properties.ShellHoming = new();

                properties.ShellCooldown = 40;
                properties.ShellLimit = 6;
                properties.ShellSpeed = 12.5f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 0;

                properties.Stationary = true;

                baseExpValue = 0.13f;
                break;
            case TankID.Marble:
                aiParams.MeanderAngle = MathHelper.PiOver2;
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 20;
                aiParams.TurretSpeed = 0.08f;
                aiParams.AimOffset = 0.11f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 70;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = MathHelper.PiOver4;

                aiParams.Inaccuracy = 0.6f;

                properties.ShootStun = 5;
                properties.ShellCooldown = 25;
                properties.ShellLimit = 3;
                properties.ShellSpeed = 10f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 1;

                properties.TreadPitch = -0.26f;
                properties.MaxSpeed = 2.6f;
                properties.Acceleration = 0.6f;
                properties.Deceleration = 0.8f;

                properties.Stationary = false;
                properties.Invisible = false;
                properties.ShellHoming = new();

                properties.MineCooldown = 850;
                properties.MineLimit = 2;
                properties.MineStun = 10;

                aiParams.MinePlacementChance = 0.05f;

                baseExpValue = 0.195f;
                break;

            #endregion

            // unimplemented XP values

            #region Special

            case TankID.Explosive:
                properties.Armor = new(tank, 3);
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 60;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = 0.04f;

                aiParams.Inaccuracy = 1.2f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.4f;

                properties.ShootStun = 0;
                properties.ShellCooldown = 90;
                properties.ShellLimit = 2;
                properties.ShellSpeed = 2f;
                properties.ShellType = ShellID.Explosive;
                properties.RicochetCount = 0;

                properties.ShootPitch = -0.1f;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.8f;
                properties.MaxSpeed = 0.8f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 940;
                properties.MineLimit = 1;
                properties.MineStun = 5;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;
                break;

            case TankID.Electro:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 60;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = 0.04f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.5f;

                properties.ShootStun = 0;
                properties.ShellCooldown = 15;
                properties.ShellLimit = 8;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.3f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 940;
                properties.MineLimit = 1;
                properties.MineStun = 5;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;
                break;

            case TankID.RocketDefender:
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 60;
                aiParams.TurretSpeed = 0.045f;
                aiParams.AimOffset = 0.04f;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.5f;

                properties.ShootStun = 0;
                properties.ShellCooldown = 15;
                properties.ShellLimit = 8;
                properties.ShellSpeed = 4f;
                properties.ShellType = ShellID.Standard;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = 0.08f;
                properties.MaxSpeed = 1.3f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 940;
                properties.MineLimit = 1;
                properties.MineStun = 5;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;
                break;

            case TankID.Assassin:
                aiParams.MeanderAngle = MathHelper.ToRadians(40);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 1;
                aiParams.TurretSpeed = 0.1f;
                aiParams.AimOffset = 0f;

                aiParams.Inaccuracy = 0.25f;
                aiParams.BounceReset = false;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.2f;

                properties.ShootStun = 25;
                properties.ShellCooldown = 100;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 9.5f;
                properties.ShellType = ShellID.Supressed;
                properties.RicochetCount = 1;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.4f;
                properties.MaxSpeed = 1.2f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;

                aiParams.SmartRicochets = true;
                break;
            case TankID.Cherry:
                aiParams.MeanderAngle = MathHelper.ToRadians(20);
                aiParams.MeanderFrequency = 15;
                aiParams.TurretMeanderFrequency = 10;
                aiParams.TurretSpeed = 0.1f;
                aiParams.AimOffset = 0.08f;

                aiParams.Inaccuracy = 0.4f;
                aiParams.BounceReset = false;

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.1f;
                properties.MaximalTurn = 0.2f;

                properties.ShootStun = 0;
                properties.ShellCooldown = 10;
                properties.ShellLimit = 10;
                properties.ShellSpeed = 6f;
                properties.ShellType = ShellID.Rocket;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.1f;
                properties.MaxSpeed = 1.35f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;

                aiParams.SmartRicochets = false;
                break;

            case TankID.Commando:
                properties.Armor = new(tank, 3);
                aiParams.MeanderAngle = MathHelper.ToRadians(30);
                aiParams.MeanderFrequency = 10;
                aiParams.TurretMeanderFrequency = 15;
                aiParams.TurretSpeed = 0.05f;
                aiParams.AimOffset = 0.03f;
                aiParams.Inaccuracy = MathHelper.ToRadians(10);

                aiParams.ProjectileWarinessRadius_PlayerShot = 140;
                aiParams.MineWarinessRadius_PlayerLaid = 140;

                properties.TurningSpeed = 0.05f;
                properties.MaximalTurn = MathHelper.ToRadians(20);

                properties.ShootStun = 25;
                properties.ShellCooldown = 50;
                properties.ShellLimit = 1;
                properties.ShellSpeed = 6f;
                properties.ShellType = ShellID.TrailedRocket;
                properties.RicochetCount = 0;

                properties.Invisible = false;
                properties.Stationary = false;
                properties.ShellHoming = new();

                properties.TreadPitch = -0.08f;
                properties.MaxSpeed = 1.4f;
                properties.Acceleration = 0.3f;
                properties.Deceleration = 0.6f;

                properties.MineCooldown = 0;
                properties.MineLimit = 0;
                properties.MineStun = 0;

                aiParams.MinePlacementChance = 0.02f;
                aiParams.ShootChance = 0.2f;
                break;

            #endregion
        }

        tank.BaseExpValue = baseExpValue;
        return aiParams;
    }

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

    public static int CountAll(Func<AITank, bool>? predicate = null) {
        var cnt = 0;
        Span<AITank> tanks = GameHandler.AllAITanks;

        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        for (var i = 0; i < tanks.Length; i++) {
            var tank = Unsafe.Add(ref tanksSearchSpace, i);
            if (tank is not null && !tank.Dead && (predicate is not null && predicate.Invoke(tank))) cnt++;
        }

        return cnt;
    }

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