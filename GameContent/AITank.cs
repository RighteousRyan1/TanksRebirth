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
using WiiPlayTanksRemake.Graphics;

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
            public int cubeWarinessDistance = 60;
            /// <summary>How often this tank reads the obstacles around it and navigates around them.</summary>
            public int cubeReadTime = 30;
            /// <summary>How far this tank must be from a teammate before it can lay a mine or fire a bullet.</summary>
            public float teammateTankWariness = 50f;
            /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
            public bool advancedRicochetCalculations;
        }
        /// <summary>The AI parameter collection of this AI Tank.</summary>
        public Params AiParams { get; } = new();

        public Vector3 aimTarget;

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

            foreach (var tank in GameHandler.AllAITanks)
            {
                if (tank is not null && !tank.Dead)
                    if (tank.tier > highest)
                        highest = tank.tier;
            }
            return highest;
        }

        public static int CountAll()
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && !tnk.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && tnk.tier == tier && !tnk.Dead);

        private bool isIngame;

        /// <summary>
        /// Creates a new <see cref="AITank"/>.
        /// </summary>
        /// <param name="tier">The tier of this <see cref="AITank"/>.</param>
        /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
        public AITank(TankTier tier, bool setTankDefaults = true, bool isIngame = true)
        {
            treadSoundTimer += new Random().Next(-1, 2);
            for (int i = 0; i < Behaviors.Length; i++)
                Behaviors[i] = new();

            this.isIngame = isIngame;

            Behaviors[0].Label = "TankBaseMovement";
            Behaviors[1].Label = "TankBarrelMovement";
            Behaviors[2].Label = "TankEnvReader";
            Behaviors[3].Label = "TankBulletFire";
            Behaviors[4].Label = "TankMinePlacement";
            Behaviors[5].Label = "TankMineAvoidance";
            Behaviors[6].Label = "TankBulletAvoidance";

            Dead = true;

            Model = GameResources.GetRawGameAsset<Model>("Assets/tank_e"); //TankGame.TankModel_Enemy;

            #region Non-Special
            if ((int)tier <= (int)TankTier.Black)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/master/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Obsidian && (int)tier <= (int)TankTier.Marble)
                _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/enemy/fate/tank_{tier.ToString().ToLower()}");
            #endregion

            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankColorTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_commando");

                foreach (var mesh in Model.Meshes)
                {
                    foreach (var effect in mesh.Effects)
                    {
                        if (mesh.Name == "Laser_Beam")
                            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/laser");
                        if (mesh.Name == "Barrel_Laser")
                            GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
                        if (mesh.Name == "Dish")
                            GameResources.GetGameResource<Texture2D>("Assets/textures/enemy/wee/tank_commando");
                    }
                }
                // fix?
            }

            #endregion

            // _tankColorTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/sussy");

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");
             this.tier = tier;

            if (setTankDefaults)
                ApplyDefaults();

            Team = Team.Blue;

            int index = Array.IndexOf(GameHandler.AllAITanks, GameHandler.AllAITanks.First(tank => tank is null));

            AITankId = index;

            GameHandler.AllAITanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            GameHandler.OnMissionStart += OnMissionStart;
        }

        /// <summary>
        /// Applies this <see cref="AITank"/>'s defaults.
        /// </summary>
        public override void ApplyDefaults()
        {
            switch (tier)
            {
                #region VanillaTanks
                case TankTier.Brown:
                    Stationary = true;

                    AiParams.turretMeanderFrequency = 30;
                    AiParams.turretSpeed = 0.01f;
                    AiParams.inaccuracy = MathHelper.ToRadians(170);

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
                    AiParams.meanderAngle = 1.8f;
                    AiParams.meanderFrequency = 40;
                    AiParams.turretMeanderFrequency = 40;
                    AiParams.turretSpeed = 0.01f;
                    AiParams.inaccuracy = 0.24f;

                    AiParams.projectileWarinessRadius = 40;
                    AiParams.mineWarinessRadius = 40;

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
                    AiParams.meanderAngle = 0.4f;
                    AiParams.meanderFrequency = 15;
                    AiParams.turretMeanderFrequency = 10;
                    AiParams.turretSpeed = 0.1f;
                    AiParams.inaccuracy = 0.005f;

                    AiParams.projectileWarinessRadius = 40;
                    AiParams.mineWarinessRadius = 80;

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

                    AiParams.advancedRicochetCalculations = true;
                    break;

                case TankTier.Purple:
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
                    ShellCooldown = 30;
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

                    TreadPitch = -0.35f;
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
                    AiParams.meanderAngle = 0.5f;
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
                    AiParams.meanderAngle = 0.3f;
                    AiParams.meanderFrequency = 5;
                    AiParams.turretMeanderFrequency = 15;
                    AiParams.turretSpeed = 0.05f;
                    AiParams.inaccuracy = 0.3f;

                    AiParams.projectileWarinessRadius = 70;
                    AiParams.mineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver2 + 0.5f;

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

                    AiParams.advancedRicochetCalculations = true;
                    break;

                case TankTier.Gold:
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
                case TankTier.Bubblegum:
                    AiParams.meanderAngle = MathHelper.ToRadians(30);
                    AiParams.meanderFrequency = 10;
                    AiParams.turretMeanderFrequency = 60;
                    AiParams.turretSpeed = 0.045f;
                    AiParams.inaccuracy = 0.04f;

                    AiParams.projectileWarinessRadius = 140;
                    AiParams.mineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.02f;
                    AiParams.shootChance = 0.2f;
                    break;
                case TankTier.Water:
                    AiParams.meanderAngle = 0.25f;
                    AiParams.meanderFrequency = 15;
                    AiParams.turretMeanderFrequency = 10;
                    AiParams.turretSpeed = 0.03f;
                    AiParams.inaccuracy = 0.08f;

                    AiParams.projectileWarinessRadius = 90;
                    AiParams.mineWarinessRadius = 150;

                    TurningSpeed = 0.2f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 20;
                    ShellCooldown = 25;
                    ShellLimit = 2;
                    ShellSpeed = 5.5f;
                    ShellType = ShellTier.Rocket;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.14f;
                    MaxSpeed = 1.7f;
                    Acceleration = 0.4f;
                    Deceleration = 0.6f;

                    treadSoundTimer = 4;
                    break;
                case TankTier.Crimson:
                    AiParams.meanderAngle = 0.12f;
                    AiParams.meanderFrequency = 8;
                    AiParams.turretMeanderFrequency = 60;
                    AiParams.turretSpeed = 0.07f;
                    AiParams.inaccuracy = 0.04f;

                    AiParams.projectileWarinessRadius = 50;
                    AiParams.mineWarinessRadius = 50;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 1;
                    ShellCooldown = 5;
                    ShellLimit = 5;
                    ShellSpeed = 3f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 0;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    MineCooldown = 340;
                    MineLimit = 6;
                    MineStun = 3;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.02f;
                    AiParams.shootChance = 0.2f;
                    break;
                case TankTier.Tiger:
                    AiParams.meanderAngle = 0.30f;
                    AiParams.meanderFrequency = 2;
                    AiParams.turretMeanderFrequency = 40;
                    AiParams.turretSpeed = 0.1f;
                    AiParams.inaccuracy = 0.12f;

                    AiParams.projectileWarinessRadius = 90;
                    AiParams.mineWarinessRadius = 120;

                    TurningSpeed = 0.1f;
                    MaximalTurn = MathHelper.PiOver2;

                    ShootStun = 0;
                    ShellCooldown = 20;
                    ShellLimit = 4;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.14f;
                    MaxSpeed = 2f;
                    Acceleration = 0.6f;
                    Deceleration = 0.8f;

                    MineCooldown = 1;
                    MineLimit = 10;
                    MineStun = 0;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.05f;
                    break;
                case TankTier.Fade:
                    AiParams.meanderAngle = MathHelper.ToRadians(30);
                    AiParams.meanderFrequency = 8;
                    AiParams.turretMeanderFrequency = 40;
                    AiParams.turretSpeed = 0.05f;
                    AiParams.inaccuracy = 0.22f;

                    AiParams.projectileWarinessRadius = 100;
                    AiParams.mineWarinessRadius = 100;

                    TurningSpeed = 0.12f;
                    MaximalTurn = MathHelper.ToRadians(30);

                    ShootStun = 5;
                    ShellCooldown = 25;
                    ShellLimit = 5;
                    ShellSpeed = 3.5f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.2f;
                    MaxSpeed = 1.9f;
                    Acceleration = 0.6f;
                    Deceleration = 0.9f;

                    MineCooldown = 680;
                    MineLimit = 3;
                    MineStun = 10;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.05f;
                    break;
                case TankTier.Creeper:
                    AiParams.meanderAngle = 0.2f;
                    AiParams.meanderFrequency = 25;
                    AiParams.turretMeanderFrequency = 40;
                    AiParams.turretSpeed = 0.085f;
                    AiParams.inaccuracy = 1f;

                    AiParams.projectileWarinessRadius = 150;
                    AiParams.mineWarinessRadius = 110;

                    TurningSpeed = 0.3f;
                    MaximalTurn = MathHelper.PiOver4;

                    ShootStun = 20;
                    ShellCooldown = 60;
                    ShellLimit = 2;
                    ShellSpeed = 8f;
                    ShellType = ShellTier.RicochetRocket;
                    RicochetCount = 3;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = -0.07f;
                    MaxSpeed = 1f;
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

                case TankTier.Commando:
                    AiParams.meanderAngle = MathHelper.ToRadians(30);
                    AiParams.meanderFrequency = 10;
                    AiParams.turretMeanderFrequency = 60;
                    AiParams.turretSpeed = 0.045f;
                    AiParams.inaccuracy = 0.04f;

                    AiParams.projectileWarinessRadius = 140;
                    AiParams.mineWarinessRadius = 140;

                    TurningSpeed = 0.1f;
                    MaximalTurn = 0.5f;

                    ShootStun = 0;
                    ShellCooldown = 15;
                    ShellLimit = 8;
                    ShellSpeed = 4f;
                    ShellType = ShellTier.Standard;
                    RicochetCount = 1;

                    Invisible = false;
                    Stationary = false;
                    ShellHoming = new();

                    TreadPitch = 0.08f;
                    MaxSpeed = 1.3f;
                    Acceleration = 0.3f;
                    Deceleration = 0.6f;

                    MineCooldown = 940;
                    MineLimit = 1;
                    MineStun = 5;

                    AiParams.moveFromMineTime = 100;
                    AiParams.minePlacementChance = 0.02f;
                    AiParams.shootChance = 0.2f;
                    break;
            }
        }
        private void OnMissionStart()
        {
            //AiParams.targetTurretRotation -= MathHelper.TwoPi;
            //targetTankRotation -= MathHelper.TwoPi;
            if (Invisible && !Dead)
            {
                var invis = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_invisible");
                SoundPlayer.PlaySoundInstance(invis, SoundContext.Effect, 0.3f);

                var lightParticle = ParticleSystem.MakeParticle(position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/light_particle"));

                lightParticle.Scale = 0.25f;
                lightParticle.Opacity = 0f;

                lightParticle.UniqueBehavior = (lp) =>
                {
                    lp.position = position;
                    if (lp.Scale < 6.5f)
                        lp.Scale += 0.08f;
                    if (lp.Opacity < 1f && lp.Scale < 6.5f)
                        lp.Opacity += 0.02f;
                    else
                    {
                        lp.Opacity -= 0.01f;
                    }
                    if (lp.Scale < 0f)
                        lp.Destroy();
                };
            }
        }

        internal void Update()
        {
            if (Dead)
                return;

            if (curShootStun > 0)
                curShootStun--;
            if (curShootCooldown > 0)
                curShootCooldown--;
            if (curMineStun > 0)
                curMineStun--;
            if (curMineCooldown > 0)
                curMineCooldown--;

            if (curShootStun > 0 || curMineStun > 0 || Stationary && isIngame)
                velocity = Vector3.Zero;

            if (isIngame)
            {
                Projection = TankGame.GameProjection;
                View = TankGame.GameView;
            }

            World = Matrix.CreateFromYawPitchRoll(-TankRotation, 0, 0)
                * Matrix.CreateTranslation(position);

            position += velocity * 0.55f;

            DoAi();

            timeSinceLastAction++;

            if (isIngame)
                UpdateCollision();

            oldPosition = position;
        }

        public override void RemoveSilently()
        {
            Dead = true;
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
        }

        private void UpdateCollision()
        {
            CollisionBox = new(position - new Vector3(7, 15, 7), position + new Vector3(10, 15, 10));
            foreach (var tank in GameHandler.AllTanks)
            {
                if (tank is not null)
                {
                    var dummyVel = Velocity2D;
                    Collision.HandleCollisionSimple(CollisionBox2D, tank.CollisionBox2D, ref dummyVel, ref position);

                    velocity.X = dummyVel.X;
                    velocity.Z = dummyVel.Y;
                }
            }

            foreach (var c in Cube.cubes)
            {
                if (c is not null)
                {
                    var dummyVel = Velocity2D;
                    Collision.HandleCollisionSimple(CollisionBox2D, c.collider2d, ref dummyVel, ref position);

                    velocity.X = dummyVel.X;
                    velocity.Z = dummyVel.Y;
                }
            }
            position.X = MathHelper.Clamp(position.X, MapRenderer.TANKS_MIN_X, MapRenderer.TANKS_MAX_X);
            position.Z = MathHelper.Clamp(position.Z, MapRenderer.TANKS_MIN_Y, MapRenderer.TANKS_MAX_Y);
        }

        /// <summary>
        /// Causes this <see cref="AITank"/> to lay a <see cref="Mine"/> at its current position.
        /// </summary>
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

            timeSinceLastAction = 0;

            var mine = new Mine(this, position, 600);
        }

        /// <summary>
        /// Destroys this <see cref="AITank"/>.
        /// </summary>
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

            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            // TODO: play fanfare thingy i think
        }

        /// <summary>
        /// Causes this <see cref="AITank"/> to fire a <see cref="Shell"/>.
        /// </summary>
        public override void Shoot()
        {
            if (!GameHandler.InMission || !HasTurret)
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

            var p = ParticleSystem.MakeParticle(bullet.position, GameResources.GetGameResource<Texture2D>("Assets/textures/misc/bot_hit"));

            p.Scale = 0.5f;

            p.UniqueBehavior = (part) =>
            {
                part.color = Color.Orange;
                part.Opacity -= 0.02f;
            };

            OwnedShellCount++;

            timeSinceLastAction = 0;

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

        private int treadPlaceTimer;
        public bool isEnemySpotted;

        private bool seeks;
        private float seekRotation = 0;

        // make a new method for just any rectangle
        private bool IsTankInPath(Vector2 pathDir, Tank enemy = null, bool draw = false, Vector2 offset = default)
        {
            const int MAX_PATH_UNITS = 250;
            const int PATH_UNIT_LENGTH = 10;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position2D + offset.RotatedByRadians(-TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;
            int pathRicochetCount = 0;

            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector3.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    pathRicochetCount++;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    pathRicochetCount++;
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 3, (int)pathPos.Y - 3, 6, 6);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, ref pathDir, ref dummyPos, out var dir, false);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                    case Collision.CollisionDirection.Down:
                        pathDir.Y *= -1;
                        pathRicochetCount++;
                        break;
                    case Collision.CollisionDirection.Left:
                    case Collision.CollisionDirection.Right:
                        pathDir.X *= -1;
                        pathRicochetCount++;
                        break;
                }

                if (i < 3 && Cube.cubes.Any(x => x is not null && pathHitbox.Intersects(x.collider2d)))
                    return false;

                if (pathRicochetCount > RicochetCount)
                    return false;

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }

                if (enemy == null)
                    continue;
                if (enemy.CollisionBox2D.Intersects(pathHitbox))
                    return true;
            }
            return false;
        }

        private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 reflectDir, bool draw = false)
        {
            const int PATH_UNIT_LENGTH = 1;

            bool hasCollided = false;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Position2D + Vector2.Zero.RotatedByRadians(-TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;

            for (int i = 0; i < checkDist; i++)
            {
                var dummyPos = Vector3.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    hasCollided = true;
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    hasCollided = true;
                }

                var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, 1, 1);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, ref pathDir, ref dummyPos, out var dir, false);

                switch (dir)
                {
                    case Collision.CollisionDirection.Up:
                    case Collision.CollisionDirection.Down:
                        hasCollided = true;
                        pathDir.Y *= -1;
                        break;
                    case Collision.CollisionDirection.Left:
                    case Collision.CollisionDirection.Right:
                        pathDir.X *= -1;
                        hasCollided = true;
                        break;
                }

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.spriteBatch.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }
            }

            reflectDir = pathDir;
            return hasCollided;
        }

        public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true)
        {
            AiParams.timeSinceLastMinePlaced++;

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation);

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            if (GameHandler.InMission)
            {
                foreach (var behavior in Behaviors)
                    behavior.totalUpdateCount++;

                treadPlaceTimer = (int)Math.Round(14 / velocity.Length()) != 0 ? (int)Math.Round(14 / velocity.Length()) : 1;

                if (velocity != Vector3.Zero && !Stationary)
                {
                    if (TankGame.GameUpdateTime % treadSoundTimer == 0)
                    {
                        var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{new Random().Next(1, 5)}");
                        var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.05f);
                        sfx.Pitch = TreadPitch;
                    }

                    if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                    {
                        LayFootprint(tier == TankTier.White ? true : false);
                    }
                }
                enactBehavior = () =>
                {
                    var enemy = GameHandler.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Dead && (tnk.Team != Team || tnk.Team == Team.NoTeam) && tnk != this);

                    foreach (var tank in GameHandler.AllTanks)
                    {
                        if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == Team.NoTeam) && tank != this)
                            if (Vector3.Distance(tank.position, position) < Vector3.Distance(enemy.position, position))
                                if ((tank.Invisible && tank.timeSinceLastAction < 60) || !tank.Invisible)
                                    enemy = tank;
                    }

                    var tanksNearMe = new List<Tank>();

                    foreach (var tank in GameHandler.AllTanks)
                        if (tank != this && tank is not null && !tank.Dead && Vector3.Distance(tank.position, position) <= AiParams.teammateTankWariness)
                            tanksNearMe.Add(tank);

                    #region TurretHandle

                    AiParams.targetTurretRotation %= MathHelper.TwoPi;

                    TurretRotation %= MathHelper.TwoPi;

                    var diff = AiParams.targetTurretRotation - TurretRotation;

                    if (diff > MathHelper.Pi)
                        AiParams.targetTurretRotation -= MathHelper.TwoPi;
                    else if (diff < -MathHelper.Pi)
                        AiParams.targetTurretRotation += MathHelper.TwoPi;

                    TurretRotation = GameUtils.RoughStep(TurretRotation, AiParams.targetTurretRotation, seeks ? AiParams.turretSpeed * 3 : AiParams.turretSpeed);

                    if (Array.IndexOf(GameHandler.AllTanks, enemy) > -1 && enemy is not null)
                    {
                        if (!seeks)
                        {
                            if (Behaviors[1].IsModOf(AiParams.turretMeanderFrequency))
                            {
                                isEnemySpotted = false;
                                if (enemy.Invisible && enemy.timeSinceLastAction < 60)
                                {
                                    aimTarget = enemy.Position2D.Expand_Z();
                                    isEnemySpotted = true;
                                }

                                if (!enemy.Invisible)
                                {
                                    aimTarget = enemy.Position2D.Expand_Z();
                                    isEnemySpotted = true;
                                }

                                var dirVec = Position2D - aimTarget.FlattenZ();  // enemy.Position2D;
                                AiParams.targetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + new Random().NextFloat(-AiParams.inaccuracy, AiParams.inaccuracy);
                            }
                        }

                        if (doFire)
                        {
                            bool seesSelf = false;

                            seekRotation += AiParams.turretSpeed;

                            AiParams.seesTarget = false;
                            if (IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), enemy, offset: Vector2.UnitY * 20))
                                AiParams.seesTarget = true;

                            if (IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), this, offset: Vector2.UnitY * 20))
                                seesSelf = true;
                            if (AiParams.advancedRicochetCalculations)
                            {
                                if (IsTankInPath(Vector2.UnitY.RotatedByRadians(seekRotation), enemy))
                                {
                                    seeks = true;
                                    AiParams.targetTurretRotation = seekRotation - MathHelper.Pi;
                                }

                                if (TurretRotation == AiParams.targetTurretRotation)
                                    seeks = false;
                            }

                            bool checkNoTeam = Team == Team.NoTeam ? true : !tanksNearMe.Any(x => x.Team == Team);

                            if (AiParams.seesTarget && checkNoTeam && !seesSelf)
                                if (curShootCooldown <= 0)
                                    Shoot();
                        }
                    }

                    #endregion
                    if (doMovements)
                    {

                        if (Stationary)
                            return;

                        bool isBulletNear = bulletFound = TryGetShellNear(AiParams.projectileWarinessRadius, out var shell);
                        bool isMineNear = mineFound = TryGetMineNear(AiParams.mineWarinessRadius, out var mine);

                        bool movingFromMine = AiParams.timeSinceLastMinePlaced < AiParams.moveFromMineTime;
                        bool movingFromOtherMine = AiParams.timeSinceLastMineFound < AiParams.moveFromMineTime / 2;

                        #region CubeNav

                        isCubeInWay = IsObstacleInWay(AiParams.cubeWarinessDistance, Vector2.UnitY.RotatedByRadians(-targetTankRotation), out var travelPath);

                        if (isCubeInWay && Behaviors[2].IsModOf(AiParams.meanderFrequency / 3))
                        {
                            targetTankRotation += GameUtils.DirectionOf(Position2D, travelPath).ToRotation() / 4; // needs fixing i think
                        }

                        #endregion

                        #region GeneralMovement
                        if (!isMineNear && !isBulletNear && !movingFromMine && !movingFromOtherMine && !IsTurning)
                        {
                            if (!isCubeInWay)
                            {
                                if (Behaviors[0].IsModOf(AiParams.meanderFrequency))
                                {
                                    var meanderRandom = new Random().NextFloat(-AiParams.meanderAngle / 2, AiParams.meanderAngle / 2);

                                    targetTankRotation += meanderRandom;
                                }
                            }
                        }
                        #endregion

                        #region ShellAvoidance

                        var indif = 1;

                        if (Behaviors[6].IsModOf(indif) && !isMineNear)
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
                            if (Behaviors[4].IsModOf(60))
                            {
                                if (!tanksNearMe.Any(x => x.Team == Team))
                                {
                                    if (new Random().NextFloat(0, 1) <= AiParams.minePlacementChance)
                                    {
                                        targetTankRotation = new Vector2(100, 100).RotatedByRadians(new Random().NextFloat(0, MathHelper.TwoPi)).Expand_Z().ToRotation();
                                        LayMine();
                                    }
                                }
                            }
                        }
                        if (isMineNear)
                        {
                            if (Behaviors[5].IsModOf(10))
                            {
                                if (AiParams.timeSinceLastMinePlaced > AiParams.moveFromMineTime)
                                {
                                    var direction = Vector2.UnitY.RotatedByRadians(mine.Position2D.DirectionOf(Position2D, false).ToRotation());

                                    targetTankRotation = direction.ToRotation();
                                }
                            }
                        }
                        #endregion

                    }

                    #region Special Tank Behavior

                    if (tier == TankTier.Creeper)
                    {
                        if (Array.IndexOf(GameHandler.AllTanks, enemy) > -1 && enemy is not null)
                        {
                            float explosionDist = 90f;
                            if (Vector3.Distance(enemy.position, position) < explosionDist)
                            {
                                Destroy();

                                new MineExplosion(position, 10f, 0.2f);

                                foreach (var tnk in GameHandler.AllTanks)
                                    if (tnk is not null && Vector3.Distance(tnk.position, position) < explosionDist)
                                        tnk.Destroy();
                            }
                        }
                    }

                    #endregion

                    #region TankRotation

                    var targ = dummyValue = targetTankRotation + MathHelper.Pi;

                    IsTurning = false;

                    /*targetTankRotation %= MathHelper.TwoPi;
                    TankRotation %= MathHelper.TwoPi;

                    var diff2 = targetTankRotation - TankRotation;

                    if (diff2 > MathHelper.Pi)
                        targetTankRotation -= MathHelper.TwoPi;
                    else if (diff2 < -MathHelper.Pi)
                        targetTankRotation += MathHelper.TwoPi;*/

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
                            treadPlaceTimer = (int)Math.Round(14 / TurningSpeed) != 0 ? (int)Math.Round(14 / TurningSpeed) : 1;
                            if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                            {
                                LayFootprint(tier == TankTier.White ? true : false);
                            }
                            IsTurning = true;
                            velocity = Vector3.Zero;
                        }

                        TankRotation = GameUtils.RoughStep(TankRotation, targ, TurningSpeed);
                    }

                    #endregion
                };
            }
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
                    // mesh.ParentBone.Index
                    effect.World = boneTransforms[mesh.ParentBone.Index];
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    if (!HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name != "Shadow")
                    {
                        if (IsHoveredByMouse)
                            effect.EmissiveColor = Color.White.ToVector3();
                        else
                            effect.EmissiveColor = Color.Black.ToVector3();

                        var tex = _tankColorTexture;

                        effect.Texture = tex;
                        /*var ex = new Color[1024];

                        Array.Fill(ex, new Color(new Random().Next(0, 256), new Random().Next(0, 256), new Random().Next(0, 256)));

                        effect.Texture.SetData(0, new Rectangle(0, 8, 32, 15), ex, 0, 480);*/
                        if (Team != Team.NoTeam)
                        {
                            var ex = new Color[1024];

                            Array.Fill(ex, (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null));

                            tex.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            tex.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);


                        }
                    }

                    else
                    {
                        effect.Alpha = 0.5f;
                        effect.Texture = _shadowTexture;
                    }

                    effect.SetDefaultGameLighting_IngameEntities();
                }

                mesh.Draw();
            }
        }
        internal void DrawBody()
        {
            if (Dead)
                return;

            if (DebugUtils.DebugLevel == 1)
            {
                if (AiParams.advancedRicochetCalculations)
                    IsTankInPath(Vector2.UnitY.RotatedByRadians(seekRotation), default, true);
                IsTankInPath(Vector2.UnitY.RotatedByRadians(TurretRotation - MathHelper.Pi), default, true);

                if (!Stationary)
                {
                    IsObstacleInWay(AiParams.cubeWarinessDistance, Vector2.UnitY.RotatedByRadians(-targetTankRotation), out var travelPath, true);
                    DebugUtils.DrawDebugString(TankGame.spriteBatch, "D", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPath.X, 11, travelPath.Y), View, Projection), 1, centerIt: true);
                }
            }

            var info = new string[]
            {
                $"Team: {Team}",
                $"Actual / Target: {TankRotation} / {dummyValue}",
                $"TurActual / TurTarget / TurDiff: {TurretRotation} / {AiParams.targetTurretRotation} / {AiParams.targetTurretRotation - TurretRotation}",
                $"IsCubeInWay: {isCubeInWay}"
            };

            for (int i = 0; i < info.Length; i++)
                DebugUtils.DrawDebugString(TankGame.spriteBatch, info[i], GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - new Vector2(0, (info.Length * 20) + (i * 20)), 1, centerIt: true);

            //if (Invisible && GameHandler.InMission)
                //return;

            RenderModel();
        }

        public override string ToString()
            => $"tier: {tier} | mFreq: {AiParams.meanderFrequency}";

        public bool TryGetShellNear(float distance, out Shell shell)
        {
            shell = null;

            Shell bulletCompare = null;

            foreach (var blet in Shell.AllShells)
            {
                if (blet is not null)
                {
                    if (Vector3.Distance(position, blet.position) < distance)
                    {
                        if (bulletCompare == null)
                            shell = blet;
                        else
                        {
                            if (Vector3.Distance(position, blet.position).CompareTo(Vector3.Distance(position, bulletCompare.position)) < 0)
                                shell = bulletCompare;
                        }
                        // bullet = blet;
                        return true;
                    }
                }
            }
            return false;
        }
        public bool TryGetMineNear(float distance, out Mine mine)
        {
            mine = null;
            foreach (var yours in Mine.AllMines)
            {
                if (yours is not null)
                {
                    if (Vector3.Distance(position, yours.position) < distance)
                    {
                        mine = yours;
                        return true;
                    }
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