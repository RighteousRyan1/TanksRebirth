using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using WiiPlayTanksRemake.Enums;
using System.Linq;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.GameContent.GameMechanics;
using WiiPlayTanksRemake.GameContent.Systems;

namespace WiiPlayTanksRemake.GameContent
{
    public class AITank : Tank
    {
        public bool Stationary { get; private set; }

        private int _treadPlaceTimer = 5;
        private int treadSoundTimer = 5;
        private int curShootStun;
        private int curShootCooldown;
        private int curMineCooldown;
        private int curMineStun;
        public int TierHierarchy => (int)tier;

        public AiBehavior[] Behaviors { get; private set; } = new AiBehavior[10]; // each of these should keep track of an action the tank performs

        public TankTier tier;

        private Texture2D _tankColorTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }
        public int WorldId { get; }

        #region AiTankParams

        public record Params {
            public float meanderAngle;
            public int bigMeanderFrequency;
            public int meanderFrequency;
            public float redirectAngle;
            public float redirectDistance;
            public float mustPivotAngle;
            public float pursuitLevel;
            public int pursuitFrequency;

            public float turretMeanderAngle;
            public int turretMeanderFrequency;
            public float turretSpeed;

            public float targetTurretRotation;

            public float inaccuracy;

            public float projectileWarinessRadius;
            public float mineWarinessRadius;

            public float minePlacementChance; // 0.0f to 1.0f

            public int moveFromMineTime;
            public int timeSinceLastMinePlaced = 999999;
            public int timeSinceLastMineFound = 999999;

            public bool seesTarget;
            public int missDistance;

            public float shootChance = 1f;
        }

        public Params AiParams { get; } = new();

        public float targetTankRotation;

        #endregion

        #region TankWatchingParams

        public Ray tankTurretRay;

        #endregion

        public Vector3 oldPosition;

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in WPTR.AllAITanks.Where(tnk => tnk is not null && !tnk.Dead))
            {
                if (tank.tier > highest)
                    highest = tank.tier;
            }
            return highest;
        }

        public static int CountAll()
            => WPTR.AllAITanks.Count(tnk => tnk is not null && !tnk.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => WPTR.AllAITanks.Count(tnk => tnk is not null && tnk.tier == tier);

        public AITank(TankTier tier = TankTier.None, bool setTankDefaults = true)
        {
            treadSoundTimer += new Random().Next(-1, 2);
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i] = new();

            Behaviors[0].Label = "TankBaseMovement";
            Behaviors[1].Label = "TankBarrelMovement";
            Behaviors[2].Label = "TankEnvReader";
            Behaviors[3].Label = "TankBulletFire";
            Behaviors[4].Label = "TankMinePlacement";
            Behaviors[5].Label = "TankMineAvoidance";
            Behaviors[6].Label = "TankBulletAvoidance";

            Model = TankGame.TankModel_Enemy;

            if ((int)tier <= (int)TankTier.Black)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/master/tank_{tier.ToString().ToLower()}");
            else
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fate/tank_{tier.ToString().ToLower()}");
            CannonMesh = Model.Meshes["polygon0.001"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
            this.tier = tier;

            void setDefaults()
            {
                switch (tier)
                {
                    #region VanillaTanks
                    case TankTier.Brown:
                        Stationary = true;

                        AiParams.turretMeanderFrequency = 30;
                        AiParams.turretSpeed = 0.01f;
                        AiParams.inaccuracy = 1.2f;

                        TurningSpeed = 0f;
                        MaximalTurn = 0;

                        ShootStun = 20;
                        ShellCooldown = 300;
                        ShellLimit = 1;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0;
                        MaxSpeed = 0f;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;

                        AiParams.missDistance = 0;

                        break;

                    case TankTier.Ash:
                        AiParams.bigMeanderFrequency = 120;
                        AiParams.meanderAngle = 1.8f;
                        AiParams.meanderFrequency = 40;
                        AiParams.turretMeanderFrequency = 40;
                        AiParams.turretSpeed = 0.01f;
                        AiParams.inaccuracy = 0.24f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 0;

                        TurningSpeed = 0.08f;
                        MaximalTurn = 0.7f;

                        ShootStun = 3;
                        ShellCooldown = 180;
                        ShellLimit = 1;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1.2f;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
                        break;

                    case TankTier.Marine:
                        AiParams.bigMeanderFrequency = 90;
                        AiParams.meanderAngle = 0.4f;
                        AiParams.meanderFrequency = 15;
                        AiParams.turretMeanderFrequency = 10;
                        AiParams.turretSpeed = 0.1f;
                        AiParams.inaccuracy = 0.005f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 0;

                        TurningSpeed = 0.2f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 20;
                        ShellCooldown = 180;
                        ShellLimit = 1;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1f;

                        _treadPlaceTimer = 8;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;

                        break;

                    case TankTier.Yellow:
                        AiParams.bigMeanderFrequency = 150;
                        AiParams.meanderAngle = MathHelper.Pi;
                        AiParams.meanderFrequency = 30;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.02f;
                        AiParams.inaccuracy = 0.5f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 20;
                        ShellCooldown = 90;
                        ShellLimit = 1;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.085f;
                        MaxSpeed = 1.8f;

                        MineCooldown = 600;
                        MineLimit = 4;
                        MineStun = 5;

                        AiParams.minePlacementChance = 0.3f;
                        AiParams.moveFromMineTime = 120;
                        break;

                    case TankTier.Pink:
                        AiParams.bigMeanderFrequency = 100;
                        AiParams.meanderAngle = 0.3f;
                        AiParams.meanderFrequency = 15;
                        AiParams.turretMeanderFrequency = 40;
                        AiParams.turretSpeed = 0.03f;
                        AiParams.inaccuracy = 0.2f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 30;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.1f;
                        MaxSpeed = 1.2f;

                        treadSoundTimer = 6;
                        _treadPlaceTimer = 6;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
                        break;

                    case TankTier.Green:
                        Stationary = true;

                        AiParams.turretMeanderFrequency = 30;
                        AiParams.turretSpeed = 0.02f;
                        AiParams.inaccuracy = 0.4f;

                        TurningSpeed = 0f;
                        MaximalTurn = 0;

                        ShootStun = 5;
                        ShellCooldown = 60;
                        ShellLimit = 2;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.RicochetRocket;
                        RicochetCount = 2;

                        TreadPitch = 0;
                        MaxSpeed = 0f;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;
                        break;

                    case TankTier.Purple:
                        AiParams.bigMeanderFrequency = 90;
                        AiParams.meanderAngle = 1f;
                        AiParams.meanderFrequency = 20;
                        AiParams.turretMeanderFrequency = 25;

                        AiParams.turretSpeed = 0.03f;
                        AiParams.inaccuracy = 0.18f;

                        AiParams.projectileWarinessRadius = 60;
                        AiParams.mineWarinessRadius = 160;

                        TurningSpeed = 0.06f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 90;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = -0.2f;
                        MaxSpeed = 1.8f;
                        Acceleration = 0.3f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 700;
                        MineLimit = 2;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 60;
                        AiParams.minePlacementChance = 0.05f;
                        break;

                    case TankTier.White:
                        AiParams.bigMeanderFrequency = 120;
                        AiParams.meanderAngle = 0.9f;
                        AiParams.meanderFrequency = 60;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.03f;
                        AiParams.inaccuracy = 0.2f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 160;

                        TurningSpeed = 0.08f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 30;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = -0.18f;
                        MaxSpeed = 1.2f;
                        Acceleration = 0.3f;

                        treadSoundTimer = 6;
                        _treadPlaceTimer = 15;

                        MineCooldown = 1000;
                        MineLimit = 2;
                        MineStun = 8;

                        AiParams.moveFromMineTime = 40;
                        AiParams.minePlacementChance = 0.08f;

                        Invisible = true;
                        break;

                    case TankTier.Black:
                        AiParams.bigMeanderFrequency = 80;
                        AiParams.meanderAngle = MathHelper.Pi;
                        AiParams.meanderFrequency = 45;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.03f;
                        AiParams.inaccuracy = 0.12f;

                        AiParams.projectileWarinessRadius = 100;
                        AiParams.mineWarinessRadius = 60;

                        TurningSpeed = 0.06f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 90;
                        ShellLimit = 3;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = -0.26f;
                        MaxSpeed = 2.4f;
                        Acceleration = 0.3f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 850;
                        MineLimit = 2;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.05f;

                        // ShellHoming.power = 1f;
                        // ShellHoming.radius = 300f;
                        // ShellHoming.speed = ShellSpeed;
                        break;
                    #endregion
                    #region MasterMod
                    case TankTier.Bronze:
                        AiParams.turretMeanderFrequency = 15;
                        AiParams.turretSpeed = 0.05f;
                        AiParams.inaccuracy = 0.005f;

                        ShellCooldown = 50;
                        ShellLimit = 2;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0f;

                        Stationary = true;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.05f;
                        break;
                    case TankTier.Silver:
                        AiParams.bigMeanderFrequency = 60;
                        AiParams.meanderAngle = 0.5f;
                        AiParams.meanderFrequency = 10;
                        AiParams.turretMeanderFrequency = 60;
                        AiParams.turretSpeed = 0.045f;
                        AiParams.inaccuracy = 0.9f;

                        AiParams.projectileWarinessRadius = 70;
                        AiParams.mineWarinessRadius = 140;

                        TurningSpeed = 0.13f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 0;
                        ShellCooldown = 15;
                        ShellLimit = 8;
                        ShellSpeed = 4f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = 0.2f;
                        MaxSpeed = 1.6f;
                        Acceleration = 0.3f;
                        Deceleration = 0.6f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 60 * 20;
                        MineLimit = 1;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.05f;
                        break;
                    case TankTier.Sapphire:
                        AiParams.bigMeanderFrequency = 120;
                        AiParams.meanderAngle = 0.25f;
                        AiParams.meanderFrequency = 15;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.025f;
                        AiParams.inaccuracy = 0.01f;

                        AiParams.projectileWarinessRadius = 40;
                        AiParams.mineWarinessRadius = 70;

                        TurningSpeed = 0.15f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 20;
                        ShellCooldown = 10;
                        ShellLimit = 3;
                        ShellSpeed = 5.5f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 0;

                        TreadPitch = 0.08f;
                        MaxSpeed = 1.4f;
                        Acceleration = 0.3f;
                        Deceleration = 0.6f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 5;

                        MineCooldown = 1000;
                        MineLimit = 1;
                        MineStun = 0;

                        AiParams.moveFromMineTime = 90;
                        AiParams.minePlacementChance = 0.05f;
                        break;
                    case TankTier.Ruby:
                        AiParams.bigMeanderFrequency = 50;
                        AiParams.meanderAngle = 0.2f;
                        AiParams.meanderFrequency = 10;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.025f;
                        AiParams.inaccuracy = 0.05f;

                        AiParams.projectileWarinessRadius = 50;
                        AiParams.mineWarinessRadius = 0;

                        TurningSpeed = 0.5f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 0;
                        ShellCooldown = 8;
                        ShellLimit = 10;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 0;

                        TreadPitch = 0.08f;
                        MaxSpeed = 1.2f;
                        Acceleration = 0.4f;
                        Deceleration = 0.6f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 5;

                        MineCooldown = 0;
                        MineLimit = 0;
                        MineStun = 0;

                        AiParams.moveFromMineTime = 0;
                        AiParams.minePlacementChance = 0;
                        break;
                    case TankTier.Citrine:
                        AiParams.bigMeanderFrequency = 90;
                        AiParams.meanderAngle = 0.7f;
                        AiParams.meanderFrequency = 30;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.035f;
                        AiParams.inaccuracy = 0.3f;

                        AiParams.projectileWarinessRadius = 80;
                        AiParams.mineWarinessRadius = 140;

                        TurningSpeed = 0.08f;
                        MaximalTurn = 1.4f;

                        ShootStun = 10;
                        ShellCooldown = 60;
                        ShellLimit = 3;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 0;

                        TreadPitch = -0.08f;
                        MaxSpeed = 3.2f;
                        Acceleration = 0.2f;
                        Deceleration = 0.4f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 360;
                        MineLimit = 4;
                        MineStun = 5;

                        AiParams.moveFromMineTime = 40;
                        AiParams.minePlacementChance = 0.15f;

                        AiParams.shootChance = 0.95f;
                        break;
                    case TankTier.Amethyst:
                        AiParams.bigMeanderFrequency = 40;
                        AiParams.meanderAngle = 0.3f;
                        AiParams.meanderFrequency = 2;
                        AiParams.turretMeanderFrequency = 15;
                        AiParams.turretSpeed = 0.05f;
                        AiParams.inaccuracy = 0.3f;

                        AiParams.projectileWarinessRadius = 70;
                        AiParams.mineWarinessRadius = 140;

                        TurningSpeed = 0.1f;
                        MaximalTurn = MathHelper.PiOver2;

                        ShootStun = 5;
                        ShellCooldown = 25;
                        ShellLimit = 5;
                        ShellSpeed = 3f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = -0.2f;
                        MaxSpeed = 2f;
                        Acceleration = 0.6f;
                        Deceleration = 0.9f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 360;
                        MineLimit = 3;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.05f;
                        break;
                    case TankTier.Emerald:
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.04f;
                        AiParams.inaccuracy = 1f;

                        ShellCooldown = 60;
                        ShellLimit = 3;
                        ShellSpeed = 8f;
                        ShellType = ShellTier.RicochetRocket;
                        RicochetCount = 2;

                        Stationary = true;
                        Invisible = true;
                        break;

                    case TankTier.Gold:
                        AiParams.bigMeanderFrequency = 80;
                        AiParams.meanderAngle = 1.8f;
                        AiParams.meanderFrequency = 20;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.02f;
                        AiParams.inaccuracy = 0.14f;

                        AiParams.shootChance = 0.7f;

                        AiParams.projectileWarinessRadius = 80;
                        AiParams.mineWarinessRadius = 120;

                        TurningSpeed = 0.06f;
                        MaximalTurn = 1.4f;

                        ShootStun = 5;
                        ShellCooldown = 30;
                        ShellLimit = 3;
                        ShellSpeed = 4f;
                        ShellType = ShellTier.Regular;
                        RicochetCount = 1;

                        TreadPitch = -0.1f;
                        MaxSpeed = 0.9f;
                        Acceleration = 0.8f;
                        Deceleration = 0.5f;

                        treadSoundTimer = 5;
                        _treadPlaceTimer = int.MaxValue;

                        MineCooldown = 700;
                        MineLimit = 2;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.01f;

                        Invisible = true;
                        break;

                    case TankTier.Obsidian:
                        AiParams.bigMeanderFrequency = 80;
                        AiParams.meanderAngle = 1.2f;
                        AiParams.meanderFrequency = 20;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.05f;
                        AiParams.inaccuracy = 0.18f;

                        AiParams.projectileWarinessRadius = 70;
                        AiParams.mineWarinessRadius = 140;

                        TurningSpeed = 0.1f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 25;
                        ShellLimit = 3;
                        ShellSpeed = 6f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 2;

                        TreadPitch = -0.26f;
                        MaxSpeed = 3f;
                        Acceleration = 0.6f;
                        Deceleration = 0.8f;

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 850;
                        MineLimit = 2;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.1f;
                        break;
                    #endregion
                    #region AdvancedMod
                    case TankTier.Marble:
                        AiParams.bigMeanderFrequency = 100;
                        AiParams.meanderAngle = MathHelper.PiOver2;
                        AiParams.meanderFrequency = 10;
                        AiParams.turretMeanderFrequency = 20;
                        AiParams.turretSpeed = 0.08f;
                        AiParams.inaccuracy = 0.11f;

                        AiParams.projectileWarinessRadius = 70;
                        AiParams.mineWarinessRadius = 140;

                        TurningSpeed = 0.1f;
                        MaximalTurn = MathHelper.PiOver4;

                        ShootStun = 5;
                        ShellCooldown = 25;
                        ShellLimit = 3;
                        ShellSpeed = 10f;
                        ShellType = ShellTier.Rocket;
                        RicochetCount = 1;

                        TreadPitch = -0.26f;
                        MaxSpeed = 2.6f;
                        Acceleration = 0.6f;
                        // decel = 0.8f

                        treadSoundTimer = 4;
                        _treadPlaceTimer = 4;

                        MineCooldown = 850;
                        MineLimit = 2;
                        MineStun = 10;

                        AiParams.moveFromMineTime = 100;
                        AiParams.minePlacementChance = 0.05f;
                        break;
                        #endregion
                }
                Team = Team.Blue;
            }
            if (setTankDefaults)
                setDefaults();

            int index = Array.IndexOf(WPTR.AllAITanks, WPTR.AllAITanks.First(tank => tank is null));

            AITankId = index;

            WPTR.AllAITanks[index] = this;

            int index2 = Array.IndexOf(WPTR.AllTanks, WPTR.AllTanks.First(tank => tank is null));

            WorldId = index2;

            WPTR.AllTanks[index2] = this;

            WPTR.OnMissionStart += OnMissionStart;
        }

        private void OnMissionStart()
        {
            targetTankRotation = TankRotation;
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Sound, 0.3f);
            }
        }

        internal void Update()
        {
            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineStun > 0)
                curMineStun--;
            if (curMineCooldown > 0)
                curMineCooldown--;

            position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
            position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);

            if (curShootStun > 0 || curMineStun > 0)
                velocity = Vector3.Zero;

            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation , 0, 0)
                * Matrix.CreateTranslation(position);

            position += velocity * 0.55f;

            DoAi();

            UpdateCollision();

            oldPosition = position;

            // TODO: fix old state nutsack
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 15, 7), position + new Vector3(10, 15, 10));
            /*if (Old is null)
                return;
            if (WPTR.AllAITanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox) && tnk != this))
            {
                position = Old.position;
            }
            if (WPTR.AllPlayerTanks.Any(tnk => tnk is not null && tnk.CollisionBox.Intersects(CollisionBox)))
            {
                position = Old.position;
            }*/

            var dummyVel = Velocity2D;
            foreach (var c in Cube.cubes.Where(c => c is not null))
            {
                Collision.HandleCollisionSimple(CollisionBox2D, c.collider2d, ref dummyVel, ref position);

                velocity.X = dummyVel.X;
                velocity.Z = dummyVel.Y;
            }
        }

        public override void LayMine()
        {
            if (curMineCooldown > 0 || OwnedMineCount >= MineLimit)
                return;

            curMineCooldown = MineCooldown;
            curMineStun = MineStun;
            var sound = GameResources.GetGameResource<SoundEffect>("Assets/sounds/mine_place");
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Sound, 0.5f);
            OwnedMineCount++;
            AiParams.timeSinceLastMinePlaced = 0;
            var mine = new Mine(this, position, 600);
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy_enemy");

            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Sound, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Sound, 0.3f);

            new TankDeathMark(TankDeathMark.CheckColor.White)
            {
                location = position + new Vector3(0, 0.1f, 0)
            };

            WPTR.AllAITanks[AITankId] = null;
            WPTR.AllTanks[WorldId] = null;
            // TODO: play fanfare thingy i think
        }

        public override void Shoot()
        {
            if (!WPTR.InMission)
                return;

            if (curShootCooldown > 0 || OwnedBulletCount >= ShellLimit)
                return;

            SoundEffectInstance sfx;

            sfx = ShellType switch
            {
                ShellTier.Regular => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Sound, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Sound, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Sound, 0.3f),
                _ => throw new NotImplementedException()
            };
            sfx.Pitch = ShootPitch;

            var bullet = new Shell(position, Vector3.Zero, homing: ShellHoming);
            var new2d = Vector2.UnitY.RotatedByRadians(TurretRotation);

            var newPos = Position2D + new Vector2(0, 20).RotatedByRadians(-TurretRotation);

            bullet.position = new Vector3(newPos.X, 11, newPos.Y);

            bullet.velocity = new Vector3(-new2d.X, 0, new2d.Y) * ShellSpeed;

            bullet.owner = this;
            bullet.ricochets = RicochetCount;

            OwnedBulletCount++;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        public override void LayFootprint(bool alt)
        {
            new TankFootprint(alt)
            {
                location = position + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }

        private bool mineFound;
        private bool oldMineFound;

        public bool MineJustFound() => mineFound && !oldMineFound;

        public bool isTurningTowards;

        public Ray tankPathRay;
        public void DoAi_Old()
        {
            // work on pivot - work on bounce calcs

            AiParams.timeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (!WPTR.InMission)
                return;

            if (velocity != Vector3.Zero && !Stationary)
            {
                if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.05f);
                    sfx.Pitch = TreadPitch;
                }
                if (TankGame.GameUpdateTime % _treadPlaceTimer == 0)
                {
                    LayFootprint(tier == TankTier.White ? true : false);
                }
            }

            foreach (var behavior in Behaviors)
                behavior.totalUpdateCount++;

            enactBehavior = () =>
            {
                // TODO: Work on why mines cause the tanks to freeze
                // TODO: MAJOR - Work on meandering ai and avoidance ai.


                // rot = 0.5 rad
                // target = 2 rad

                if (!Dead)
                {
                    var enemy = WPTR.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && tnk.Team != Team && tnk != this);

                    tankPathRay = GeometryUtils.CreateRayFrom2D(Position2D, Vector2.UnitY.RotatedByRadians(-TankRotation));

                    if (!Stationary)
                    {
                        AiParams.timeSinceLastMineFound++;

                        bool movingFromMine = AiParams.timeSinceLastMinePlaced < AiParams.moveFromMineTime;
                        bool movingFromOtherMine = AiParams.timeSinceLastMineFound < AiParams.moveFromMineTime / 2;

                        bool isBulletNear = TryGetBulletNear(AiParams.projectileWarinessRadius, out var shell);
                        bool isMineNear = mineFound = TryGetMineNear(AiParams.mineWarinessRadius, out var mine);

                        if (!movingFromMine && !movingFromOtherMine)
                        {
                            if (Behaviors[6].IsBehaviourModuloOf(5))
                            {
                                if (isBulletNear && shell.lifeTime > 30)
                                {
                                    var dir = new Vector2(0, -5).RotatedByRadians(shell.Position2D.DirectionOf(Position2D, true).ToRotation());

                                    TankRotation = dir.ToRotation();

                                    // MoveTo(dir.Expand_Z());
                                    velocity.X = dir.X;
                                    velocity.Z = dir.Y;

                                    velocity.Normalize();

                                    velocity *= MaxSpeed;
                                }
                            }
                        }

                        if (MineLimit > 0)
                        {
                            if (Behaviors[4].IsBehaviourModuloOf(60))
                            {
                                if (new Random().NextFloat(0, 1) <= AiParams.minePlacementChance)
                                {
                                    //targetPosition = new Vector2(100, 100).Expand_Z();
                                    LayMine();
                                }
                            }
                        }

                        // if (!movingFromMine)
                        {
                            if (Behaviors[5].IsBehaviourModuloOf(5))
                            {
                                if (isMineNear)
                                {
                                    // targetPosition = new Vector2(0, -5).RotatedByRadians(mine.Position2D.DirectionOf(Position2D).ToRotation()).Expand_Z() * 5; //new Vector2(100, 100).Expand_Z();
                                    AiParams.timeSinceLastMineFound = 0;
                                    /*var dir = new Vector2(0, -5).RotatedByRadians(mine.Position2D.DirectionOf(Position2D).ToRotation());

                                    // MoveTo(dir.Expand_Z());

                                    velocity.X = dir.X;
                                    velocity.Z = dir.Y;

                                    velocity.Normalize();

                                    velocity *= MaxSpeed;*/
                                }
                            }
                            if (!isBulletNear)
                            {
                                if (Behaviors[0].IsBehaviourModuloOf(AiParams.meanderFrequency / 2))
                                {
                                    var meanderRandom = new Random().NextFloat(-AiParams.meanderAngle / 10, AiParams.meanderAngle / 10);

                                    TankRotation = meanderRandom;

                                    var dir = Position2D.RotatedByRadians(TankRotation);

                                    velocity.X = dir.X;
                                    velocity.Z = dir.Y;

                                    velocity.Normalize();

                                    velocity *= MaxSpeed;
                                }
                                if (Behaviors[0].IsBehaviourModuloOf(AiParams.meanderFrequency))
                                {
                                    var meanderRandom = new Random().NextFloat(-AiParams.meanderAngle / 2, AiParams.meanderAngle / 2);

                                    TankRotation = meanderRandom;

                                    var dir = Position2D.RotatedByRadians(TankRotation);

                                    velocity.X = dir.X;
                                    velocity.Z = dir.Y;

                                    velocity.Normalize();

                                    velocity *= MaxSpeed;
                                }
                            }
                        }
                    }
                }
                oldMineFound = mineFound;
            };
            enactBehavior?.Invoke();
        }

        public void DoAi()
        {
            AiParams.timeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + MathHelper.TwoPi + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (!WPTR.InMission)
                return;

            foreach (var behavior in Behaviors)
                behavior.totalUpdateCount++;

            if (velocity != Vector3.Zero && !Stationary)
            {
                if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Sound, 0.05f);
                    sfx.Pitch = TreadPitch;
                }
                if (TankGame.GameUpdateTime % _treadPlaceTimer == 0)
                {
                    LayFootprint(tier == TankTier.White ? true : false);
                }
            }

            enactBehavior = () =>
            {
                var enemy = WPTR.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && tnk.Team != Team && tnk != this);

                #region TurretHandle
                TurretRotation = GameUtils.RoughStep(TurretRotation, AiParams.targetTurretRotation, AiParams.turretSpeed);
                if (Array.IndexOf(WPTR.AllTanks, enemy) > -1 && enemy is not null)
                {
                    if (Behaviors[1].IsBehaviourModuloOf(AiParams.turretMeanderFrequency))
                    {
                        var dirVec = Position2D - enemy.Position2D;
                        AiParams.targetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + new Random().NextFloat(-AiParams.inaccuracy, AiParams.inaccuracy);

                        // const float ThreeFourthsPi = MathHelper.Pi + MathHelper.PiOver2;

                        /*if (targetTurretRotation - last_spot_angle > ThreeFourthsPi || last_spot_angle - targetTurretRotation < MathHelper.PiOver2)
                        {
                            // target is at pi/2 and last angle is at pi/4

                            TurretRotation = targetTurretRotation;
                        }*/

                        // last_spot_angle = targetTurretRotation;
                    }

                    var turRotationReal = Vector2.UnitY.RotatedByRadians(-TurretRotation);

                    tankTurretRay = GeometryUtils.CreateRayFrom2D(Position2D, turRotationReal, 0f);

                    List<Ray> rays = new();

                    rays.Add(tankTurretRay);

                    // Create a few rays here to simulate "shot is close to target" effect
                    for (int k = 0; k < AiParams.missDistance; k++)
                    {
                        rays.Add(GeometryUtils.CreateRayFrom2D(Position2D, turRotationReal.RotatedByRadians(AiParams.missDistance * k)));
                        rays.Add(GeometryUtils.CreateRayFrom2D(Position2D, turRotationReal.RotatedByRadians(-AiParams.missDistance * k)));
                    }
                    for (int i = 0; i < RicochetCount; i++)
                    {
                        if (tankTurretRay.Intersects(MapRenderer.BoundsRenderer.BoundaryBox).HasValue)
                        {
                            // normal is up usually
                            var r = GeometryUtils.Reflect(tankTurretRay, tankTurretRay.Intersects(MapRenderer.BoundsRenderer.BoundaryBox).Value);
                            tankTurretRay = r;
                            rays.Add(tankTurretRay);
                        }
                    }

                    raysMarched = rays;

                    // check if friendly intersected is LESS than enemy intersected, if true then prevent fire

                    AiParams.seesTarget = rays.Any(r => r.Intersects(enemy.CollisionBox).HasValue);

                    if (AiParams.seesTarget)
                    {
                        if (curShootCooldown <= 0)
                        {
                            Shoot();
                        }
                    }
                }
                #endregion

                #region MovementHandle

                if (Stationary)
                    return;

                bool isBulletNear = TryGetBulletNear(AiParams.projectileWarinessRadius, out var shell);
                bool isMineNear = mineFound = TryGetMineNear(AiParams.mineWarinessRadius, out var mine);

                bool movingFromMine = AiParams.timeSinceLastMinePlaced < AiParams.moveFromMineTime;
                bool movingFromOtherMine = AiParams.timeSinceLastMineFound < AiParams.moveFromMineTime / 2;

                if (!isMineNear && !isBulletNear && !movingFromMine && !movingFromOtherMine && !IsTurning)
                {
                    if (Behaviors[0].IsBehaviourModuloOf(AiParams.meanderFrequency))
                    {
                        var meanderRandom = new Random().NextFloat(-AiParams.meanderAngle / 2, AiParams.meanderAngle / 2);

                        targetTankRotation += meanderRandom;

                        /*if (targetTankRotation > MathHelper.TwoPi)
                            targetTankRotation = targetTankRotation - MathHelper.TwoPi;
                        else if (targetTankRotation < 0)
                            targetTankRotation = MathHelper.TwoPi + targetTankRotation;*/
                    }


                    if (Behaviors[0].IsBehaviourModuloOf(AiParams.bigMeanderFrequency))
                    {
                        var meanderRandom = new Random().NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);

                        targetTankRotation += meanderRandom;

                        /*if (targetTankRotation > MathHelper.TwoPi)
                            targetTankRotation = targetTankRotation - MathHelper.TwoPi;
                        else if (targetTankRotation < 0)
                            targetTankRotation = MathHelper.TwoPi + targetTankRotation;*/
                    }
                }


                #region ShellAvoidance

                if (Behaviors[6].IsBehaviourModuloOf(5))
                {
                    if (isBulletNear)
                    {

                        if (shell.owner == this)
                        {
                            if (shell.lifeTime > 30)
                            {
                                var dire = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Position2D, false).ToRotation());

                                targetTankRotation = dire.ToRotation();
                            }
                        }
                        else
                        {
                            var direction = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Position2D, false).ToRotation());

                            targetTankRotation = direction.ToRotation();
                        }
                    }
                }

                #region MineHandle / MineAvoidance
                if (MineLimit > 0)
                {
                    if (Behaviors[4].IsBehaviourModuloOf(60))
                    {
                        if (new Random().NextFloat(0, 1) <= AiParams.minePlacementChance)
                        {
                            targetTankRotation = new Vector2(100, 100).RotatedByRadians(new Random().NextFloat(0, MathHelper.TwoPi)).Expand_Z().ToRotation();
                            LayMine();
                        }
                    }

                    if (Behaviors[5].IsBehaviourModuloOf(10))
                    {
                        if (isMineNear)
                        {
                            if (AiParams.timeSinceLastMinePlaced > AiParams.moveFromMineTime)
                            {
                                var direction = Vector2.UnitY.RotatedByRadians(mine.Position2D.DirectionOf(Position2D, false).ToRotation());

                                targetTankRotation = direction.ToRotation();
                            }
                        }
                    }
                }
                #endregion

                #endregion

                var targ = dummyValue = targetTankRotation + MathHelper.Pi;

                IsTurning = false;

                if (TankRotation > targ - MaximalTurn && TankRotation < targ + MaximalTurn)
                {
                    // TankRotation = targ;
                    var dir = Vector2.UnitY.RotatedByRadians(TankRotation);
                    velocity.X = dir.X;
                    velocity.Z = dir.Y;

                    velocity.Normalize();
                    velocity *= MaxSpeed;
                }
                else
                {
                    IsTurning = true;
                    velocity = Vector3.Zero;
                }
                TankRotation = GameUtils.RoughStep(TankRotation, targ, TurningSpeed);
                #endregion
            };
            enactBehavior?.Invoke();
        }
        float dummyValue;
        public List<Ray> raysMarched = new();
        private void RenderModel()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    if (mesh.Name != "polygon1")
                        effect.Texture = _tankColorTexture;

                    else
                        effect.Texture = _shadowTexture;
                }

                mesh.Draw();
            }
        }
        internal void DrawBody()
        {
            if (Dead)
                return;

            var info = new string[]
            {
                $"Team: {Team}",
                $"ViewsTarget: {AiParams.seesTarget}",
                $"Actual / Target: {TankRotation} / {dummyValue}",
                $"IsTurning: {IsTurning}"
            };

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centerIt: true);

            DebugUtils.DrawDebugString(TankGame.spriteBatch, "0", GeometryUtils.ConvertWorldToScreen(tankTurretRay.Position, World, View, Projection), 3, centerIt: true);
            DebugUtils.DrawDebugString(TankGame.spriteBatch, "1", GeometryUtils.ConvertWorldToScreen((tankTurretRay.Position + tankTurretRay.Direction) * 30, World, View, Projection), 3, centerIt: true);

            /*foreach (var ray in raysMarched)
            {
                DebugUtils.DrawDebugString(TankGame.spriteBatch, "0", GeometryUtils.ConvertWorldToScreen(ray.Position, World, View, Projection), 1, centerIt: true);
                DebugUtils.DrawDebugString(TankGame.spriteBatch, "1", GeometryUtils.ConvertWorldToScreen(ray.Position + ray.Direction * 30, World, View, Projection), 1, centerIt: true);
            }*/

            if (Invisible && WPTR.InMission)
                return;
            RenderModel();
        }

        public override string ToString()
            => $"tier: {tier} | mFreq: {AiParams.meanderFrequency}";

        public bool TryGetBulletNear(float distance, out Shell bullet)
        {
            bullet = null;

            Shell bulletCompare = null;

            foreach (var blet in Shell.AllShells.Where(shel => shel is not null))
            {
                if (Vector3.Distance(position, blet.position) < distance)
                {
                    if (bulletCompare == null)
                        bullet = blet;
                    else
                    {
                        if (Vector3.Distance(position, blet.position).CompareTo(Vector3.Distance(position, bulletCompare.position)) < 0)
                            bullet = bulletCompare;
                    }
                    // bullet = blet;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetMineNear(float distance, out Mine mine)
        {
            mine = null;
            foreach (var yours in Mine.AllMines.Where(mine => mine is not null))
            {
                if (Vector3.Distance(position, yours.position) < distance)
                {
                    mine = yours;
                    return true;
                }
            }
            return false;
        }

        public static TankTier PICK_ANY_THAT_ARE_IMPLEMENTED()
        {
            TankTier[] workingTiers = 
            { 
                TankTier.Brown, TankTier.Marine, TankTier.Yellow, TankTier.Black, TankTier.White, TankTier.Pink, TankTier.Purple, TankTier.Green, TankTier.Ash, 
                TankTier.Bronze, TankTier.Silver, TankTier.Sapphire, TankTier.Ruby, TankTier.Citrine, TankTier.Amethyst, TankTier.Emerald, TankTier.Gold, TankTier.Obsidian 
            };

            return workingTiers[new Random().Next(0, workingTiers.Length)];
        }
    }
}