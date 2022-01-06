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
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;

namespace WiiPlayTanksRemake.GameContent
{
    public class AITank : Tank
    {
        private int treadSoundTimer = 5;
        private int curShootStun;
        private int curShootCooldown;
        private int curMineCooldown;
        private int curMineStun;
        public int TierHierarchy => (int)tier;

        public AiBehavior[] Behaviors { get; private set; } = new AiBehavior[10]; // each of these should keep track of an action the tank performs

        public TankTier tier;

        /// <summary>Should always be -1 if this tank is not modded content.</summary>
        public int modTier = -1;

        private Texture2D _tankColorTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }
        public int WorldId { get; }

        #region AiTankParams

        public record Params
        {
            /// <summary>The max angle of which this tank will "meander," or change its movement direction.</summary>
            public float meanderAngle;
            /// <summary>How often this tank will take a large turn.</summary>
            public int bigMeanderFrequency;
            /// <summary>How often this tank will take a turn at <see cref="meanderAngle"/> radians.</summary>
            public int meanderFrequency;

            /// <summary>Not implemented (yet). Determines how much this tank will move un attempt to get closer to its target.</summary>
            public float pursuitLevel;
            /// <summary>Not implemented (yet). Determines how often this tank will try to move towards its target.</summary>
            public int pursuitFrequency;

            /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="inaccuracy"/>.</summary>
            public int turretMeanderFrequency;
            /// <summary>How fast this tank's turret rotates towards its target.</summary>
            public float turretSpeed;
            /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="turretSpeed"/>.</summary>
            public float targetTurretRotation;
            /// <summary>How inaccurate (in radians) this tank is trying to aim at its target.</summary>
            public float inaccuracy;

            /// <summary>The distance of which this tank is wary of projectiles and tries to move away from them.</summary>
            public float projectileWarinessRadius;
            /// <summary>The distance of which this tank is wary of mines and tries to move away from them.</summary>
            public float mineWarinessRadius;

            /// <summary>On a given tick, it has this chance out of 1 to lay a mine. <para>Do note that this value must be greater than 0 and less than or equal to 1.</para></summary>
            public float minePlacementChance; // 0.0f to 1.0f

            /// <summary>How long (in ticks) this tank moves away from a mine that it places.</summary>
            public int moveFromMineTime;
            internal int timeSinceLastMinePlaced = 999999;
            internal int timeSinceLastMineFound = 999999;

            /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
            public bool seesTarget;
            /// <summary>How many extra rays are cast in 2 different directions. If any of these indirect rays hits a target, this tank fires.</summary>
            public int missDistance;

            /// <summary>How often this tank shoots when given the opportunity. 0 to 1 values only. Defaults to 1.</summary>
            public float shootChance = 1f;

            /// <summary>How far ahead of this tank (in the direction the tank is going) that it is aware of obstacles and navigates around them.</summary>
            public float cubeWarinessDistance = 60f;
            /// <summary>How often this tank reads the obstacles around it and navigates around them</summary>
            public int cubeReadTime = 30;
        }
        /// <summary>The AI parameter collection of this AI Tank.</summary>
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
            => WPTR.AllAITanks.Count(tnk => tnk is not null && tnk.tier == tier && !tnk.Dead);

        /// <summary>The vanilla game constructor for AI Tanks.</summary>
        public AITank(TankTier tier, bool setTankDefaults = true)
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

            Dead = true;

            Model = TankGame.TankModel_Enemy;

            if ((int)tier <= (int)TankTier.Black)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/master/tank_{tier.ToString().ToLower()}");
            else
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fate/tank_{tier.ToString().ToLower()}");

            // _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fanmade/tank_animegirl");

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
             this.tier = tier;

            if (setTankDefaults)
                ApplyDefaults();

            Team = Team.Blue;

            int index = Array.IndexOf(WPTR.AllAITanks, WPTR.AllAITanks.First(tank => tank is null));

            AITankId = index;

            WPTR.AllAITanks[index] = this;

            int index2 = Array.IndexOf(WPTR.AllTanks, WPTR.AllTanks.First(tank => tank is null));

            WorldId = index2;

            WPTR.AllTanks[index2] = this;

            WPTR.OnMissionStart += OnMissionStart;
        }
        /// <summary>The modded constructor for AI Tanks.</summary>
        public AITank(int modTier, bool setTankDefaults = true)
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

            Dead = true;

            Model = TankGame.TankModel_Enemy;

            if ((int)tier <= (int)TankTier.Black)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/master/tank_{tier.ToString().ToLower()}");
            else
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fate/tank_{tier.ToString().ToLower()}");
            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
            this.modTier = modTier;

            if (setTankDefaults)
                ApplyDefaults();

            Team = Team.Blue;

            int index = Array.IndexOf(WPTR.AllAITanks, WPTR.AllAITanks.First(tank => tank is null));

            AITankId = index;

            WPTR.AllAITanks[index] = this;

            int index2 = Array.IndexOf(WPTR.AllTanks, WPTR.AllTanks.First(tank => tank is null));

            WorldId = index2;

            WPTR.AllTanks[index2] = this;

            WPTR.OnMissionStart += OnMissionStart;
        }

        public override void ApplyDefaults()
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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    ShellHoming = new();

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

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

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.085f;
                    MaxSpeed = 1f;

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.085f;
                    MaxSpeed = 1.8f;

                    MineCooldown = 600;
                    MineLimit = 4;
                    MineStun = 5;

                    treadSoundTimer = 4;

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.1f;
                    MaxSpeed = 1.2f;

                    treadSoundTimer = 6;

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

                    Invisible = false;
                    ShellHoming = new();

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
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 5;
                    ShellCooldown = 90;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 1.8f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 4;

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.18f;
                    MaxSpeed = 1.2f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 6;

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

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.26f;
                    MaxSpeed = 2.4f;
                    Acceleration = 0.3f;

                    treadSoundTimer = 4;

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = true;
                    ShellHoming = new();

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.2f;
                    MaxSpeed = 1.6f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

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

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.4f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

                    MineCooldown = 1000;
                    MineLimit = 1;
                    MineStun = 0;

                    AiParams.moveFromMineTime = 90;
                    AiParams.minePlacementChance = 0.05f;
                    break;
                case TankTier.Ruby:
                    AiParams.bigMeanderFrequency = 120;
                    AiParams.meanderAngle = 0.35f;
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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.2f;
                    Acceleration = 0.4f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;

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
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.08f;
                    MaxSpeed = 3.2f;
                    Acceleration = 0.2f;
                    Deceleration = 0.4f;

                    treadSoundTimer = 4;

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
                    AiParams.meanderFrequency = 4;
                    AiParams.turretMeanderFrequency = 15;
                    AiParams.turretSpeed = 0.05f;
                    AiParams.inaccuracy = 0.3f;

                    AiParams.projectileWarinessRadius = 70;
                    AiParams.mineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.Pi;

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 2f;
                    Acceleration = 0.6f;
                    Deceleration = 0.9f;

                    treadSoundTimer = 4;

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
                    ShellHoming = new();
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

                    CanLayTread = false;

                    TurningSpeed = 0.06f;
                    MaximalTurn = 1.4f;

                    ShootStun = 5;
                    ShellCooldown = 30;
                    ShellLimit = 3;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.1f;
                    MaxSpeed = 0.9f;
                    Acceleration = 0.8f;
                    Deceleration = 0.5f;

                    treadSoundTimer = 5;

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

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.26f;
                    MaxSpeed = 3f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    treadSoundTimer = 4;

                    MineCooldown = 850;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.1f;
                    break;
                #endregion
                #region AdvancedMod
                case TankTier.Granite:
                    AiParams.bigMeanderFrequency = 100;
                    AiParams.meanderAngle = 0.8f;
                    AiParams.meanderFrequency = 10;
                    AiParams.turretMeanderFrequency = 20;
                    AiParams.turretSpeed = 0.09f;
                    AiParams.inaccuracy = 0f;

                    AiParams.projectileWarinessRadius = 150;
                    AiParams.mineWarinessRadius = 90;

                    TurningSpeed = 0.3f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 60;
                    ShellCooldown = 40;
                    ShellLimit = 2;
                    ShellSpeed = 5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.07f;
                    MaxSpeed = 0.9f;
                    Acceleration = 0.3f;
                    Deceleration = 0.4f;

                    treadSoundTimer = 4;
                    break;
                case TankTier.Gamma:
                    AiParams.turretMeanderFrequency = 20;
                    AiParams.turretSpeed = 0.08f;
                    AiParams.inaccuracy = 0.01f;

                    Invisible = false;
                    ShellHoming = new();

                    ShellCooldown = 40;
                    ShellLimit = 6;
                    ShellSpeed = 12.5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Stationary = true;
                    break;
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
                    Deceleration = 0.8f;

                    Stationary = false;
                    Invisible = false;
                    ShellHoming = new();

                    treadSoundTimer = 4;

                    MineCooldown = 850;
                    MineLimit = 2;
                    MineStun = 10;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.05f;
                    break;
                    #endregion
            }
        }
        private void OnMissionStart()
        {
            targetTankRotation = TankRotation;
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);
            }
        }

        internal void Update()
        {
            if (Dead)
                return;

            UpdateCollision();

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

            if (curShootStun > 0 || curMineStun > 0 || Stationary)
                velocity = Vector3.Zero;

            Projection = TankGame.GameProjection;
            View = TankGame.GameView;

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(position);

            position += velocity * 0.55f;

            DoAi();

            oldPosition = position;
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 15, 7), position + new Vector3(10, 15, 10));

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
            SoundPlayer.PlaySoundInstance(sound, SoundContext.Effect, 0.5f);
            OwnedMineCount++;
            AiParams.timeSinceLastMinePlaced = 0;

            var mine = new Mine(this, position, 600);
        }

        public override void Destroy()
        {
            Dead = true;
            var killSound1 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy");
            var killSound2 = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_destroy_enemy");

            SoundPlayer.PlaySoundInstance(killSound1, SoundContext.Effect, 0.2f);
            SoundPlayer.PlaySoundInstance(killSound2, SoundContext.Effect, 0.3f);

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

            if (curShootCooldown > 0 || OwnedShellCount >= ShellLimit)
                return;

            SoundEffectInstance sfx;

            sfx = ShellType switch
            {
                ShellTier.Standard => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_regular_2"), SoundContext.Effect, 0.3f),
                ShellTier.Rocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_rocket"), SoundContext.Effect, 0.3f),
                ShellTier.RicochetRocket => SoundPlayer.PlaySoundInstance(GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_shoot_ricochet_rocket"), SoundContext.Effect, 0.3f),
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

            OwnedShellCount++;

            curShootStun = ShootStun;
            curShootCooldown = ShellCooldown;
        }

        public override void LayFootprint(bool alt)
        {
            if (!CanLayTread)
                return;
            new TankFootprint(alt)
            {
                location = position + new Vector3(0, 0.1f, 0),
                rotation = -TankRotation
            };
        }

        private bool mineFound;

        private bool bulletFound;

        public Ray tankPathRay;
        
        public bool isCubeInWay;
        private bool wasCubeInWay;


        private float treadPlaceTimer;
        public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doAim = true)
        {
            AiParams.timeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (!WPTR.InMission)
                return;

            foreach (var behavior in Behaviors)
                behavior.totalUpdateCount++;

            // treadPlaceTimer += MaxSpeed / (tier == TankTier.White ? 10 : 5);
            treadPlaceTimer += MaxSpeed - (MaxSpeed * (tier == TankTier.White ? 0.92f : 0.85f));
            if (velocity != Vector3.Zero && !Stationary)
            {
                if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                {
                    var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                    var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.05f);
                    sfx.Pitch = TreadPitch;
                }

                if (treadPlaceTimer > MaxSpeed)
                {
                    treadPlaceTimer = 0;
                    LayFootprint(tier == TankTier.White ? true : false);
                }
            }
            enactBehavior = () =>
            {
                var enemy = WPTR.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && (tnk.Team != Team || tnk.Team == Team.NoTeam) && tnk != this);

                if (doAim)
                {
                    #region TurretHandle
                    TurretRotation = GameUtils.RoughStep(TurretRotation, AiParams.targetTurretRotation, AiParams.turretSpeed);
                    if (Array.IndexOf(WPTR.AllTanks, enemy) > -1 && enemy is not null)
                    {
                        if (Behaviors[1].IsBehaviourModuloOf(AiParams.turretMeanderFrequency))
                        {
                            var dirVec = Position2D - enemy.Position2D;
                            AiParams.targetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + new Random().NextFloat(-AiParams.inaccuracy, AiParams.inaccuracy);

                            //const float ThreeFourthsPi = MathHelper.Pi + MathHelper.PiOver2;

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
                            if (MapRenderer.BoundsRenderer.enclosingBoxes.Any(c => tankTurretRay.Intersects(c).HasValue) || Cube.cubes.Any(x => x is not null && tankTurretRay.Intersects(x.collider).HasValue))
                            {
                                // normal is up usually
                                var r = GeometryUtils.Reflect(tankTurretRay, tankTurretRay.Intersects(MapRenderer.BoundsRenderer.BoundaryBox).Value);
                                tankTurretRay = r;
                                rays.Add(tankTurretRay);
                            }
                        }

                        raysMarched = rays;

                        // check if friendly intersected is LESS than enemy intersected, if true then prevent fire

                        /*float? cubeInter = 0f;
                        float? tnkInter = 0f;

                        if (tankTurretRay.Intersects(enemy.CollisionBox).HasValue)
                            tnkInter = tankTurretRay.Intersects(enemy.CollisionBox);
                        foreach (var cube in Cube.cubes.Where(c => c is not null))
                        {
                            if (tankTurretRay.Intersects(cube.collider).HasValue)
                            {
                                cubeInter = tankTurretRay.Intersects(cube.collider);
                            }
                        }

                        WPTR.ClientLog.Write($"tnk: {tier} | c: {cubeInter} | t: {tnkInter}", LogType.Debug);

                        if (cubeInter is not null && tnkInter is not null)
                        {
                            if (cubeInter < tnkInter)
                            {
                                AiParams.seesTarget = false;
                            }
                            else
                                AiParams.seesTarget = true;
                        }
                        if (tnkInter is null)
                            AiParams.seesTarget = false;*/
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
                }
                if (doMovements)
                {

                    if (Stationary)
                        return;

                    bool isBulletNear = bulletFound = TryGetBulletNear(AiParams.projectileWarinessRadius, out var shell);
                    bool isMineNear = mineFound = TryGetMineNear(AiParams.mineWarinessRadius, out var mine);

                    bool movingFromMine = AiParams.timeSinceLastMinePlaced < AiParams.moveFromMineTime;
                    bool movingFromOtherMine = AiParams.timeSinceLastMineFound < AiParams.moveFromMineTime / 2;

                    #region CubeNav

                    var rays = new List<Ray>();

                    /*for (int i = 0; i < 1; i++)
                    {
                        var tnkRay = GeometryUtils.CreateRayFrom2D(Position2D, Vector2.UnitY.RotatedByRadians(TankRotation + MathHelper.ToRadians(i)));
                        var tnkRay2 = GeometryUtils.CreateRayFrom2D(Position2D, Vector2.UnitY.RotatedByRadians(TankRotation - MathHelper.ToRadians(i)));

                        rays.Add(tnkRay);
                        rays.Add(tnkRay2);
                    }*/

                    var tnkRayBase = GeometryUtils.CreateRayFrom2D(Position2D, Vector2.UnitY.RotatedByRadians(TankRotation));
                    isCubeInWay = IsCubeInRayPath(tnkRayBase, AiParams.cubeWarinessDistance);

                    //isCubeInWay = rays.Any(r => IsCubeInRayPath(r, AiParams.cubeWarinessDistance));

                    if (isCubeInWay && Behaviors[2].IsBehaviourModuloOf(AiParams.cubeReadTime))
                    {
                        /*float goodRot = 0f;

                        for (int i = 0; i < 4; i++)
                        {
                            var tnkRay = GeometryUtils.CreateRayFrom2D(position, Vector2.UnitY.RotatedByRadians(targetTankRotation + MathHelper.PiOver2 * i));

                            // i dont get why this makes tanks go only up into cubes
                            if (!IsCubeInRayPath(tnkRay, AiParams.cubeWarinessDistance))
                            {
                                goodRot = MathHelper.PiOver2 * i;
                                break;
                            }
                        }
                        WPTR.ClientLog.Write(goodRot, LogType.Debug);
                        targetTankRotation = goodRot;*/

                        /*var turnInvar = 0.15f;// MathHelper.PiOver2;

                        if (velocity.X > 0 && velocity.Z > 0) // yes
                        {
                            // down right

                            targetTankRotation += turnInvar;//0.1f;
                        }
                        else if (velocity.X >= 0 && velocity.Z <= 0) // yes
                        {
                            // up right

                            targetTankRotation += turnInvar;
                        }
                        else if (velocity.X <= 0 && velocity.Z >= 0) // yes
                        {
                            // down left

                            targetTankRotation -= turnInvar;
                        }
                        else if (velocity.X < 0 && velocity.Z < 0) // yes
                        {
                            // up left

                            targetTankRotation -= turnInvar;
                        }*/
                    }

                    wasCubeInWay = isCubeInWay;

                    // adjust angles

                    #endregion

                    #region GeneralMovement
                    if (!isMineNear && !isBulletNear && !movingFromMine && !movingFromOtherMine && !IsTurning)
                    {
                        // if (!isCubeInWay)
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
                    }
                    #endregion

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

                    #endregion

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

                }

                #region TankRotation

                var targ = dummyValue = targetTankRotation + MathHelper.Pi;

                IsTurning = false;

                if (doMoveTowards)
                {
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
                        if (treadPlaceTimer > MaxSpeed)
                        {
                            treadPlaceTimer = 0;
                            LayFootprint(tier == TankTier.White ? true : false);
                        }
                        IsTurning = true;
                        velocity = Vector3.Zero;
                    }

                    TankRotation = GameUtils.RoughStep(TankRotation, targ, TurningSpeed);
                }

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

                    if (mesh.Name != "Shadow")
                    {
                        if (IsHoveredByMouse)
                            effect.EmissiveColor = Color.White.ToVector3();
                        else
                            effect.EmissiveColor = Color.Black.ToVector3();
                        effect.Texture = _tankColorTexture;
                        /*var ex = new Color[1024];

                        Array.Fill(ex, new Color(new Random().Next(0, 256), new Random().Next(0, 256), new Random().Next(0, 256)));

                        effect.Texture.SetData(0, new Rectangle(0, 8, 32, 15), ex, 0, 480);*/
                        if (Team != Team.NoTeam)
                        {
                            var ex = new Color[1024];

                            Array.Fill(ex, (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null));

                            effect.Texture.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            effect.Texture.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }
                    }

                    else
                    {
                        effect.Texture = _shadowTexture;
                    }
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
                $"Vel: {velocity}   ",
                $"ViewsTarget: {AiParams.seesTarget}",
                $"Actual / Target: {TankRotation} / {dummyValue}",
                $"Mine: {mineFound} | Bullet: {bulletFound}",
                $"IsCubeInWay: {isCubeInWay}"
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

        public static bool IsCubeInRayPath(Ray navigator, float maxDist)
        {
            return Cube.cubes.Any(c => c is not null && navigator.Intersects(c.collider) is not null && navigator.Intersects(c.collider).Value <= maxDist) 
                || (MapRenderer.BoundsRenderer.enclosingBoxes.Any(c => navigator.Intersects(c).HasValue && navigator.Intersects(c).Value <= maxDist));
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