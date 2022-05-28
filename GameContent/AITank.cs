using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Enums;
using System.Linq;
using TanksRebirth.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.IO;
using System.Reflection;
using TanksRebirth.Net;
using TanksRebirth.IO;
using TanksRebirth.GameContent.Properties;

namespace TanksRebirth.GameContent
{
    public class AITank : Tank
    {
        public AiBehavior[] Behaviors { get; private set; } // each of these should keep track of an action the tank performs
        public AiBehavior[] SpecialBehaviors { get; private set; }

        public TankTier Tier;

        private Texture2D _tankTexture, _shadowTexture;

        public Action enactBehavior;

        public int AITankId { get; }

        #region AiTankParams

        public record Params
        {
            /// <summary>The max angle of which this tank will "meander," or change its movement direction.</summary>
            public float MeanderAngle { get; set; }
            /// <summary>How often this tank will take a turn at <see cref="MeanderAngle"/> radians.</summary>
            public int MeanderFrequency { get; set; }

            /// <summary>Not implemented (yet). Determines how much this tank will move un attempt to get closer to its target.</summary>
            public float PursuitLevel { get; set; }
            /// <summary>Not implemented (yet). Determines how often this tank will try to move towards its target.</summary>
            public int PursuitFrequency { get; set; }

            /// <summary>How often this tank will move its turret in the target's direction. It will be inaccurate at the measure of <see cref="AimOffset"/>.</summary>
            public int TurretMeanderFrequency { get; set; }
            /// <summary>How fast this tank's turret rotates towards its target.</summary>
            public float TurretSpeed { get; set; }
            /// <summary>How inaccurate (in radians) this tank is trying to aim at its target.</summary>
            public float AimOffset { get; set; }

            /// <summary>The distance of which this tank is wary of projectiles and tries to move away from them.</summary>
            public float ProjectileWarinessRadius { get; set; }
            /// <summary>The distance of which this tank is wary of mines and tries to move away from them.</summary>
            public float MineWarinessRadius { get; set; }

            /// <summary>On a given tick, it has this chance out of 1 to lay a mine. <para>Do note that this value must be greater than 0 and less than or equal to 1.</para></summary>
            public float MinePlacementChance { get; set; } // 0.0f to 1.0f

            /// <summary>How long (in ticks) this tank moves away from a mine that it places.</summary>
            public int MoveFromMineTime { get; set; }

            /// <summary>The distance from the main shot calculation ray an enemy must be before this tank is allowed to fire.</summary>
            public float Inaccuracy { get; set; }

            /// <summary>How often this tank shoots when given the opportunity. 0 to 1 values only. Defaults to 1.</summary>
            public float ShootChance { get; set; } = 1f;

            /// <summary>How far ahead of this tank (in the direction the tank is going) that it is aware of obstacles and navigates around them.</summary>
            public int BlockWarinessDistance { get; set; } = 50;
            /// <summary>How often this tank reads the obstacles around it and navigates around them.</summary>
            public int BlockReadTime { get; set; } = 3;
            /// <summary>How far this tank must be from a teammate before it can lay a mine or fire a bullet.</summary>
            public float TankWarinessRadius { get; set; } = 50f;
            /// <summary>Whether or not this tank tries to find calculations all around it. This is not recommended for mobile tanks.</summary>
            public bool SmartRicochets { get; set; }
            /// <summary>Whether or not this tank attempts to lay mines near destructible obstacles rather than randomly. Useless for stationary tanks.</summary>
            public bool SmartMineLaying { get; set; }
            /// <summary>Whether or not this tank's shot raycast resets it's distance check per-bounce.</summary>
            public bool BounceReset { get; set; } = true;
            /// <summary>When this tank finds a wall in its path, it moves away at this angle every <see cref="BlockReadTime"/> ticks.</summary>
            public float RedirectAngle { get; set; } = MathHelper.ToRadians(5);
            /// <summary>Whether or not this tank predics the future position of its target.</summary>
            public bool PredictsPositions { get; set; }

            // TODO: make friendly check distances separate for bullets and mines
        }
        /// <summary>The AI parameter collection of this AI Tank.</summary>
        public Params AiParams { get; } = new();

        public Vector2 Aimtarget;

        /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
        public bool SeesTarget { get; set; }

        /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="TurretSpeed"/>.</summary>
        public float TargetTurretRotation;

        private Vector2 _oldPosition;

        public float BaseExpValue { get; set; }

        #endregion

        #region ModelBone & ModelMesh
        public Matrix[] boneTransforms;

        public ModelMesh CannonMesh;
        #endregion

        public static TankTier GetHighestTierActive()
        {
            var highest = TankTier.None;

            foreach (var tank in GameHandler.AllAITanks)
            {
                if (tank is not null && !tank.Properties.Dead)
                    if (tank.Tier > highest)
                        highest = tank.Tier;
            }
            return highest;
        }

        public static int CountAll()
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && !tnk.Properties.Dead);

        public static int GetTankCountOfType(TankTier tier)
            => GameHandler.AllAITanks.Count(tnk => tnk is not null && tnk.Tier == tier && !tnk.Properties.Dead);

        public void Swap(TankTier tier, bool setDefaults = true)
        {
            this.Tier = tier;

            if ((int)tier <= (int)TankTier.Marble)
                _tankTexture = Assets[$"tank_" + tier.ToString().ToLower()];
            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");

                foreach (var mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        if (mesh.Name == "Laser_Beam")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/laser");
                        if (mesh.Name == "Barrel_Laser")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
                        if (mesh.Name == "Dish")
                            effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");
                    }
                }
                // fix?
            }
            else if (tier == TankTier.Assassin)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin");
            }
            else if (tier == TankTier.RocketDefender)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
            }
            else if (tier == TankTier.Electro)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro");
            }
            else if (tier == TankTier.Explosive)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive");
            }
            else
            {
                Model = GameResources.GetGameResource<Model>("Assets/tank_e");
            }

            #endregion

            if (setDefaults)
                ApplyDefaults();
        }

        /// <summary>
        /// Creates a new <see cref="AITank"/>.
        /// </summary>
        /// <param name="tier">The tier of this <see cref="AITank"/>. If '<see cref="TankTier.Random"/>', it will be randomly chosen.</param>
        /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
        /// /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
        public AITank(TankTier tier, Range<TankTier> tankRange = default, bool setTankDefaults = true, bool isIngame = true)
        {
            if (isIngame)
            {
                if (Difficulties.Types["BumpUp"])
                    tier += 1;
                if (Difficulties.Types["MeanGreens"])
                    tier = TankTier.Green;
                if (Difficulties.Types["MasterModBuff"] && !Difficulties.Types["MarbleModBuff"])
                    tier += 9;
                if (Difficulties.Types["MarbleModBuff"] && !Difficulties.Types["MasterModBuff"])
                    tier += 18;
                if (Difficulties.Types["RandomizedTanks"])
                {
                    tier = TankTier.Random;
                    tankRange = new Range<TankTier>(TankTier.Brown, TankTier.Marble); // set to commando when the time comes
                }
            }
            if (tier == TankTier.Random)
                tier = (TankTier)GameHandler.GameRand.Next((int)tankRange.Min, (int)tankRange.Max + 1);
            Properties.IsIngame = isIngame;
            if (isIngame)
            {
                Behaviors = new AiBehavior[10];
                SpecialBehaviors = new AiBehavior[3];

                for (int i = 0; i < Behaviors.Length; i++)
                    Behaviors[i] = new();

                for (int i = 0; i < SpecialBehaviors.Length; i++)
                    SpecialBehaviors[i] = new();

                Behaviors[0].Label = "TankBaseMovement";
                Behaviors[1].Label = "TankBarrelMovement";
                Behaviors[2].Label = "TankEnvReader";
                Behaviors[3].Label = "TankBulletFire";
                Behaviors[4].Label = "TankMinePlacement";
                Behaviors[5].Label = "TankMineAvoidance";
                Behaviors[6].Label = "TankBulletAvoidance";

                SpecialBehaviors[0].Label = "SpecialBehavior1"; // for special tanks (such as commando, etc)
                SpecialBehaviors[1].Label = "SpecialBehavior2";
                SpecialBehaviors[2].Label = "SpecialBehavior3";

                Properties.Dead = true;
            }

            /*if ((int)tier <= (int)TankTier.Black)
                _tankTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Black && (int)tier <= (int)TankTier.Obsidian)
                _tankTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/master/tank_{tier.ToString().ToLower()}");
            else if ((int)tier > (int)TankTier.Obsidian && (int)tier <= (int)TankTier.Marble)
                _tankTexture = GameResources.GetGameResource<Texture2D>($"Assets/textures/tank/marble/tank_{tier.ToString().ToLower()}");*/

            if ((int)tier <= (int)TankTier.Marble)
                _tankTexture = Assets[$"tank_" + tier.ToString().ToLower()];
            
            #region Special

            if (tier == TankTier.Commando)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_commando");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_commando");
                // fix?
            }
            else if (tier == TankTier.Assassin)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_assassin");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_assassin");
            }
            else if (tier == TankTier.RocketDefender)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_rocket");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_rocket");
            }
            else if (tier == TankTier.Electro)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_electro");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_electro");
            }
            else if (tier == TankTier.Explosive)
            {
                Model = GameResources.GetGameResource<Model>("Assets/specialtanks/tank_explosive");

                _tankTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank/wee/tank_explosive");
            }
            else
            {
                Model = GameResources.GetGameResource<Model>("Assets/tank_e");
            }

            #endregion

            CannonMesh = Model.Meshes["Cannon"];

            boneTransforms = new Matrix[Model.Bones.Count];

            _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

            this.Tier = tier;
            if (isIngame)
            {

                if (setTankDefaults)
                    ApplyDefaults();

            }
            int index = Array.IndexOf(GameHandler.AllAITanks, GameHandler.AllAITanks.First(tank => tank is null));

            AITankId = index;

            GameHandler.AllAITanks[index] = this;

            int index2 = Array.IndexOf(GameHandler.AllTanks, GameHandler.AllTanks.First(tank => tank is null));

            WorldId = index2;

            GameHandler.AllTanks[index2] = this;

            GameProperties.OnMissionStart += OnMissionStart;
            
            base.Initialize();
        }

        private void OnMissionStart()
        {
            // other things can be done here
            //UpdateAim(new List<Tank>(), true);
        }

        public override void ApplyDefaults()
        {
            switch (Tier)
            {
                #region VanillaTanks
                case TankTier.Brown:
                    Properties.Stationary = true;

                    Properties.DestructionColor = new(152, 96, 26);

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.AimOffset = MathHelper.ToRadians(170);
                    AiParams.Inaccuracy = 1.6f;

                    Properties.TurningSpeed = 0f;
                    Properties.MaximalTurn = 0;

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 300;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0;
                    Properties.MaxSpeed = 0f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    BaseExpValue = 0.01f;

                    break;

                case TankTier.Ash:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.01f;
                    AiParams.AimOffset = MathHelper.ToRadians(40);

                    AiParams.Inaccuracy = 0.9f;

                    Properties.DestructionColor = Color.Gray;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 40;

                    Properties.TurningSpeed = 0.08f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 3;
                    Properties.ShellCooldown = 180;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.085f;
                    Properties.MaxSpeed = 1.2f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 25;

                    BaseExpValue = 0.015f;
                    break;

                case TankTier.Marine:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = MathHelper.ToRadians(0);

                    Properties.DestructionColor = Color.Teal;

                    AiParams.Inaccuracy = 0.15f;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 80;

                    Properties.TurningSpeed = 0.2f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 180;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 6f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.085f;
                    Properties.MaxSpeed = 1f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 40;

                    BaseExpValue = 0.04f;
                    break;

                case TankTier.Yellow:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = 0.5f;

                    AiParams.Inaccuracy = 1.5f;

                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.DestructionColor = Color.Yellow;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    Properties.TurningSpeed = 0.08f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 90;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.085f;
                    Properties.MaxSpeed = 1.8f;

                    Properties.MineCooldown = 600;
                    Properties.MineLimit = 4;
                    Properties.MineStun = 5;

                    AiParams.MinePlacementChance = 0.3f;
                    AiParams.MoveFromMineTime = 120;

                    BaseExpValue = 0.035f;

                    if (Difficulties.Types["PieFactory"])
                    {
                        Properties.VulnerableToMines = false;
                        Properties.MineCooldown = 10;
                        Properties.MineLimit = 20;
                        Properties.MineStun = 0;
                        AiParams.MinePlacementChance = 1f;
                        AiParams.MineWarinessRadius = 0;
                    }
                    break;

                case TankTier.Pink:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = 0.2f;

                    AiParams.Inaccuracy = 1.3f;

                    Properties.DestructionColor = Color.Pink;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    Properties.TurningSpeed = 0.08f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 30;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.1f;
                    Properties.MaxSpeed = 1.2f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.BlockWarinessDistance = 35;

                    BaseExpValue = 0.08f;
                    break;

                case TankTier.Purple:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 25;

                    AiParams.Inaccuracy = 0.8f;

                    Properties.DestructionColor = Color.Purple;

                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = 0.18f;

                    AiParams.ProjectileWarinessRadius = 60;
                    AiParams.MineWarinessRadius = 160;

                    Properties.TurningSpeed = 0.06f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 30;
                    Properties.ShellLimit = 5;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.2f;
                    Properties.MaxSpeed = 1.8f;
                    Properties.Acceleration = 0.3f;

                    Properties.MineCooldown = 700;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 60;
                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.BlockWarinessDistance = 45;

                    BaseExpValue = 0.1f;
                    break;

                case TankTier.Green:
                    Properties.Stationary = true;

                    AiParams.TurretMeanderFrequency = 30;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = MathHelper.ToRadians(80);
                    AiParams.Inaccuracy = MathHelper.ToRadians(25);

                    Properties.DestructionColor = Color.LimeGreen;

                    Properties.TurningSpeed = 0f;
                    Properties.MaximalTurn = 0;

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 60;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 6f; // 6f
                    Properties.ShellType = ShellType.TrailedRocket;
                    Properties.RicochetCount = 2; // 2

                    Properties.Invisible = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0;
                    Properties.MaxSpeed = 0f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    BaseExpValue = 0.12f;
                    break;

                case TankTier.White:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(40);

                    AiParams.Inaccuracy = 0.8f;

                    Properties.DestructionColor = Color.White;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 160;

                    Properties.TurningSpeed = 0.08f;
                    Properties.MaximalTurn = MathHelper.ToRadians(10);

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 30;
                    Properties.ShellLimit = 5;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.35f;
                    Properties.MaxSpeed = 1.2f;
                    Properties.Acceleration = 0.3f;

                    Properties.MineCooldown = 1000;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 8;

                    AiParams.MoveFromMineTime = 40;
                    AiParams.MinePlacementChance = 0.08f;

                    AiParams.BlockWarinessDistance = 30;

                    Properties.Invisible = true;

                    BaseExpValue = 0.125f;
                    break;

                case TankTier.Black:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(5);

                    AiParams.Inaccuracy = 0.35f;

                    Properties.DestructionColor = Color.Black;

                    AiParams.ProjectileWarinessRadius = 100;
                    AiParams.MineWarinessRadius = 60;

                    Properties.TurningSpeed = 0.06f;
                    Properties.MaximalTurn = MathHelper.ToRadians(5);

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 90;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 6f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.26f;
                    Properties.MaxSpeed = 2.4f;
                    Properties.Acceleration = 0.3f;

                    Properties.ShootPitch = -0.2f;

                    Properties.MineCooldown = 850;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    AiParams.BlockWarinessDistance = 60;

                    BaseExpValue = 0.145f;
                    break;
                #endregion
                #region MasterMod
                case TankTier.Bronze:
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.005f;

                    AiParams.Inaccuracy = 0.2f;

                    Properties.DestructionColor = new(152, 96, 26);

                    Properties.ShellCooldown = 50;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = true;
                    Properties.ShellHoming = new();

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.025f;
                    break;
                case TankTier.Silver:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.9f;

                    AiParams.Inaccuracy = 0.4f;

                    Properties.DestructionColor = Color.Silver;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.13f;
                    Properties.MaximalTurn = MathHelper.PiOver2;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 15;
                    Properties.ShellLimit = 8;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.2f;
                    Properties.MaxSpeed = 1.6f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 60 * 20;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.07f;
                    break;
                case TankTier.Sapphire:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.AimOffset = 0.01f;

                    AiParams.Inaccuracy = 0.4f;

                    Properties.DestructionColor = Color.DeepSkyBlue;

                    AiParams.ProjectileWarinessRadius = 40;
                    AiParams.MineWarinessRadius = 70;

                    Properties.TurningSpeed = 0.15f;
                    Properties.MaximalTurn = MathHelper.PiOver2;

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 10;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 5.5f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.4f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 1000;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 0;

                    AiParams.MoveFromMineTime = 90;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Ruby:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.025f;
                    AiParams.AimOffset = 0.05f;

                    AiParams.Inaccuracy = 0.6f;

                    Properties.DestructionColor = Color.IndianRed;

                    //AiParams.PursuitLevel = 0.1f;
                    //AiParams.PursuitFrequency = 30;

                    AiParams.ProjectileWarinessRadius = 50;
                    AiParams.MineWarinessRadius = 70;

                    Properties.TurningSpeed = 0.5f;
                    Properties.MaximalTurn = MathHelper.PiOver2;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 8;
                    Properties.ShellLimit = 10;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.2f;
                    Properties.Acceleration = 0.4f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.MoveFromMineTime = 0;
                    AiParams.MinePlacementChance = 0;

                    AiParams.BlockWarinessDistance = 30;

                    BaseExpValue = 0.13f;
                    break;
                case TankTier.Citrine:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 30;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.035f;
                    AiParams.AimOffset = 0.3f;

                    AiParams.Inaccuracy = 0.25f;

                    Properties.DestructionColor = Color.Yellow;

                    AiParams.ProjectileWarinessRadius = 80;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.08f;
                    Properties.MaximalTurn = 1.4f;

                    Properties.ShootStun = 10;
                    Properties.ShellCooldown = 60;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 6f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.08f;
                    Properties.MaxSpeed = 3.2f;
                    Properties.Acceleration = 0.2f;
                    Properties.Deceleration = 0.4f;

                    Properties.MineCooldown = 360;
                    Properties.MineLimit = 4;
                    Properties.MineStun = 5;

                    AiParams.MoveFromMineTime = 40;
                    AiParams.MinePlacementChance = 0.15f;

                    AiParams.ShootChance = 0.95f;

                    AiParams.BlockWarinessDistance = 60;

                    BaseExpValue = 0.09f;
                    break;
                case TankTier.Amethyst:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 5;
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.3f;

                    AiParams.Inaccuracy = 0.65f;

                    Properties.DestructionColor = Color.Purple;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = MathHelper.PiOver2 + 0.5f;

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 25;
                    Properties.ShellLimit = 5;
                    Properties.ShellSpeed = 3.5f; // 3.5
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1; // 1

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.2f;
                    Properties.MaxSpeed = 2f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.9f;

                    Properties.MineCooldown = 360;
                    Properties.MineLimit = 3;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Emerald:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.04f;
                    AiParams.AimOffset = 1f;

                    AiParams.Inaccuracy = 0.35f;

                    Properties.DestructionColor = Color.Green;

                    Properties.ShellCooldown = 60;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 8f;
                    Properties.ShellType = ShellType.TrailedRocket;
                    Properties.RicochetCount = 2;

                    Properties.Stationary = true;
                    Properties.Invisible = true;
                    Properties.ShellHoming = new();

                    AiParams.SmartRicochets = true;

                    BaseExpValue = 0.14f;
                    break;

                case TankTier.Gold:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.02f;
                    AiParams.AimOffset = 0.14f;

                    AiParams.Inaccuracy = 0.4f;

                    Properties.DestructionColor = Color.Gold;

                    AiParams.ShootChance = 0.7f;

                    AiParams.ProjectileWarinessRadius = 80;
                    AiParams.MineWarinessRadius = 120;

                    Properties.CanLayTread = false;

                    Properties.TurningSpeed = 0.06f;
                    Properties.MaximalTurn = 1.4f;

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 30;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.1f;
                    Properties.MaxSpeed = 0.9f;
                    Properties.Acceleration = 0.8f;
                    Properties.Deceleration = 0.5f;

                    Properties.MineCooldown = 700;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.01f;

                    Properties.Invisible = true;

                    BaseExpValue = 0.16f;
                    break;

                case TankTier.Obsidian:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 20;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.18f;

                    AiParams.Inaccuracy = 0.9f;

                    Properties.DestructionColor = Color.Black;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = MathHelper.PiOver4;

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 25;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 6f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 2;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.26f;
                    Properties.MaxSpeed = 3f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.8f;

                    Properties.MineCooldown = 850;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.1f;

                    AiParams.BlockWarinessDistance = 50;
                    AiParams.BlockReadTime = 30;

                    BaseExpValue = 0.175f;
                    break;
                #endregion
                #region AdvancedMod
                case TankTier.Granite:
                    AiParams.MeanderAngle = 0.8f;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.09f;
                    AiParams.AimOffset = 0f;

                    AiParams.Inaccuracy = 0.5f;

                    AiParams.ProjectileWarinessRadius = 150;
                    AiParams.MineWarinessRadius = 90;

                    Properties.TurningSpeed = 0.3f;
                    Properties.MaximalTurn = MathHelper.PiOver4;

                    Properties.ShootStun = 60;
                    Properties.ShellCooldown = 40;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 5f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.07f;
                    Properties.MaxSpeed = 0.9f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.4f;

                    AiParams.SmartRicochets = true;

                    BaseExpValue = 0.02f;
                    break;
                case TankTier.Bubblegum:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = MathHelper.ToRadians(30);

                    AiParams.Inaccuracy = 0.4f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.5f;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 15;
                    Properties.ShellLimit = 8;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.3f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 940;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    BaseExpValue = 0.035f;
                    break;
                case TankTier.Water:
                    AiParams.MeanderAngle = 0.25f;
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 10;
                    AiParams.TurretSpeed = 0.03f;
                    AiParams.AimOffset = MathHelper.ToRadians(10);

                    AiParams.Inaccuracy = 0.5f;

                    AiParams.ProjectileWarinessRadius = 90;
                    AiParams.MineWarinessRadius = 150;

                    Properties.TurningSpeed = 0.2f;
                    Properties.MaximalTurn = MathHelper.PiOver4;

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 25;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 5.5f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.14f;
                    Properties.MaxSpeed = 1.7f;
                    Properties.Acceleration = 0.4f;
                    Properties.Deceleration = 0.6f;

                    BaseExpValue = 0.08f;
                    break;
                case TankTier.Crimson:
                    AiParams.MeanderAngle = 0.12f;
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.07f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.Inaccuracy = 0.2f;

                    AiParams.ProjectileWarinessRadius = 50;
                    AiParams.MineWarinessRadius = 50;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.5f;

                    Properties.ShootStun = 1;
                    Properties.ShellCooldown = 5;
                    Properties.ShellLimit = 5;
                    Properties.ShellSpeed = 3f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.3f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.8f;

                    Properties.MineCooldown = 340;
                    Properties.MineLimit = 6;
                    Properties.MineStun = 3;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    BaseExpValue = 0.095f;
                    break;
                case TankTier.Tiger:
                    AiParams.MeanderAngle = 0.30f;
                    AiParams.MeanderFrequency = 2;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = 0.12f;

                    AiParams.Inaccuracy = 0.7f;

                    AiParams.ProjectileWarinessRadius = 90;
                    AiParams.MineWarinessRadius = 120;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = MathHelper.PiOver2;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 20;
                    Properties.ShellLimit = 4;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.14f;
                    Properties.MaxSpeed = 2f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.8f;

                    Properties.MineCooldown = 1;
                    Properties.MineLimit = 10;
                    Properties.MineStun = 0;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.1f;
                    break;
                case TankTier.Fade:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 8;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.22f;

                    AiParams.Inaccuracy = 1.1f;

                    AiParams.ProjectileWarinessRadius = 100;
                    AiParams.MineWarinessRadius = 100;

                    Properties.TurningSpeed = 0.12f;
                    Properties.MaximalTurn = MathHelper.ToRadians(30);

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 25;
                    Properties.ShellLimit = 5;
                    Properties.ShellSpeed = 3.5f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.2f;
                    Properties.MaxSpeed = 1.9f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.9f;

                    Properties.MineCooldown = 680;
                    Properties.MineLimit = 3;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.105f;
                    break;
                case TankTier.Creeper:
                    AiParams.MeanderAngle = 0.2f;
                    AiParams.MeanderFrequency = 25;
                    AiParams.TurretMeanderFrequency = 40;
                    AiParams.TurretSpeed = 0.085f;
                    AiParams.AimOffset = 1f;

                    AiParams.SmartRicochets = true;

                    AiParams.Inaccuracy = 0.6f;

                    AiParams.ProjectileWarinessRadius = 150;
                    AiParams.MineWarinessRadius = 110;

                    Properties.TurningSpeed = 0.3f;
                    Properties.MaximalTurn = MathHelper.PiOver4;

                    Properties.ShootStun = 20;
                    Properties.ShellCooldown = 60;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 8f;
                    Properties.ShellType = ShellType.TrailedRocket;
                    Properties.RicochetCount = 3;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.07f;
                    Properties.MaxSpeed = 1f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.4f;

                    BaseExpValue = 0.17f;
                    break;
                case TankTier.Gamma:
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.AimOffset = 0.01f;

                    AiParams.Inaccuracy = 0.15f;

                    Properties.Invisible = false;
                    Properties.ShellHoming = new();

                    Properties.ShellCooldown = 40;
                    Properties.ShellLimit = 6;
                    Properties.ShellSpeed = 12.5f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 0;

                    Properties.Stationary = true;

                    BaseExpValue = 0.13f;
                    break;
                case TankTier.Marble:
                    AiParams.MeanderAngle = MathHelper.PiOver2;
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 20;
                    AiParams.TurretSpeed = 0.08f;
                    AiParams.AimOffset = 0.11f;

                    AiParams.ProjectileWarinessRadius = 70;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = MathHelper.PiOver4;

                    AiParams.Inaccuracy = 0.6f;

                    Properties.ShootStun = 5;
                    Properties.ShellCooldown = 25;
                    Properties.ShellLimit = 3;
                    Properties.ShellSpeed = 10f;
                    Properties.ShellType = ShellType.Rocket;
                    Properties.RicochetCount = 1;

                    Properties.TreadPitch = -0.26f;
                    Properties.MaxSpeed = 2.6f;
                    Properties.Acceleration = 0.6f;
                    Properties.Deceleration = 0.8f;

                    Properties.Stationary = false;
                    Properties.Invisible = false;
                    Properties.ShellHoming = new();

                    Properties.MineCooldown = 850;
                    Properties.MineLimit = 2;
                    Properties.MineStun = 10;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.05f;

                    BaseExpValue = 0.195f;
                    break;
                #endregion
                // unimplemented XP values
                #region Special
                case TankTier.Explosive:
                    Properties.Armor = new(this, 3);
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.Inaccuracy = 1.2f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.4f;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 90;
                    Properties.ShellLimit = 2;
                    Properties.ShellSpeed = 2f;
                    Properties.ShellType = ShellType.Explosive;
                    Properties.RicochetCount = 0;

                    Properties.ShootPitch = -0.1f;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.8f;
                    Properties.MaxSpeed = 0.8f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 940;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Electro:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.5f;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 15;
                    Properties.ShellLimit = 8;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.3f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 940;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.RocketDefender:
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 60;
                    AiParams.TurretSpeed = 0.045f;
                    AiParams.AimOffset = 0.04f;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.5f;

                    Properties.ShootStun = 0;
                    Properties.ShellCooldown = 15;
                    Properties.ShellLimit = 8;
                    Properties.ShellSpeed = 4f;
                    Properties.ShellType = ShellType.Standard;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = 0.08f;
                    Properties.MaxSpeed = 1.3f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 940;
                    Properties.MineLimit = 1;
                    Properties.MineStun = 5;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;

                case TankTier.Assassin:
                    AiParams.MeanderAngle = MathHelper.ToRadians(40);
                    AiParams.MeanderFrequency = 15;
                    AiParams.TurretMeanderFrequency = 1;
                    AiParams.TurretSpeed = 0.1f;
                    AiParams.AimOffset = 0f;

                    AiParams.Inaccuracy = 0.25f;
                    AiParams.BounceReset = false;

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.1f;
                    Properties.MaximalTurn = 0.2f;

                    Properties.ShootStun = 25;
                    Properties.ShellCooldown = 100;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 9.5f;
                    Properties.ShellType = ShellType.Supressed;
                    Properties.RicochetCount = 1;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.4f;
                    Properties.MaxSpeed = 1.2f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;

                    AiParams.SmartRicochets = true;
                    break;

                case TankTier.Commando:
                    Properties.Armor = new(this, 3);
                    AiParams.MeanderAngle = MathHelper.ToRadians(30);
                    AiParams.MeanderFrequency = 10;
                    AiParams.TurretMeanderFrequency = 15;
                    AiParams.TurretSpeed = 0.05f;
                    AiParams.AimOffset = 0.03f;
                    AiParams.Inaccuracy = MathHelper.ToRadians(10);

                    AiParams.ProjectileWarinessRadius = 140;
                    AiParams.MineWarinessRadius = 140;

                    Properties.TurningSpeed = 0.05f;
                    Properties.MaximalTurn = MathHelper.ToRadians(20);

                    Properties.ShootStun = 25;
                    Properties.ShellCooldown = 50;
                    Properties.ShellLimit = 1;
                    Properties.ShellSpeed = 6f;
                    Properties.ShellType = ShellType.TrailedRocket;
                    Properties.RicochetCount = 0;

                    Properties.Invisible = false;
                    Properties.Stationary = false;
                    Properties.ShellHoming = new();

                    Properties.TreadPitch = -0.08f;
                    Properties.MaxSpeed = 1.4f;
                    Properties.Acceleration = 0.3f;
                    Properties.Deceleration = 0.6f;

                    Properties.MineCooldown = 0;
                    Properties.MineLimit = 0;
                    Properties.MineStun = 0;

                    AiParams.MoveFromMineTime = 100;
                    AiParams.MinePlacementChance = 0.02f;
                    AiParams.ShootChance = 0.2f;
                    break;
                    #endregion
            }
            if (Difficulties.Types["TanksAreCalculators"])
                if (Properties.RicochetCount >= 1)
                    if (Properties.HasTurret)
                        AiParams.SmartRicochets = true;

            if (Difficulties.Types["UltraMines"])
                AiParams.MineWarinessRadius *= 3;

            if (Difficulties.Types["AllInvisible"])
            {
                Properties.Invisible = true;
                Properties.CanLayTread = false;
            }
            if (Difficulties.Types["AllStationary"])
                Properties.Stationary = true;

            if (Difficulties.Types["AllHoming"])
            {
                Properties.ShellHoming = new();
                Properties.ShellHoming.Radius = 200f;
                Properties.ShellHoming.Speed = Properties.ShellSpeed;
                Properties.ShellHoming.Power = 0.1f * Properties.ShellSpeed;
                // ShellHoming.isHeatSeeking = true;

                AiParams.Inaccuracy *= 4;
            }

            if (Difficulties.Types["Armored"])
            {
                if (Properties.Armor is null)
                    Properties.Armor = new(this, 3);
                else
                    Properties.Armor = new(this, Properties.Armor.HitPoints + 3);
            }

            if (Difficulties.Types["Predictions"])
                AiParams.PredictsPositions = true;

            /*foreach (var aifld in AiParams.GetType().GetProperties())
                if (aifld.GetValue(AiParams) is int)
                    aifld.SetValue(AiParams, GameHandler.GameRand.Next(1, 60));
                else if (aifld.GetValue(AiParams) is float)
                    aifld.SetValue(AiParams, GameHandler.GameRand.NextFloat(0.01f, 2f));
                else if (aifld.GetValue(AiParams) is bool)
                    aifld.SetValue(AiParams, GameHandler.GameRand.Next(0, 2) == 0);
            foreach (var fld in GetType().GetProperties())
            {
                if (fld.SetMethod != null && fld == typeof(Enum) && !fld.Name.ToLower().Contains("behavior") && !fld.Name.Contains("Id"))
                {
                    if (fld.GetValue(this) is int)
                        fld.SetValue(this, GameHandler.GameRand.Next(1, 60));
                    else if (fld.GetValue(this) is float)
                        fld.SetValue(this, GameHandler.GameRand.NextFloat(0.01f, 60));
                    else if (fld.GetValue(this) is bool && fld.Name != "Dead")
                        fld.SetValue(this, GameHandler.GameRand.Next(0, 2) == 0);
                }
            }*/
        }
        public override void Update()
        {
            base.Update();

            CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(Properties.TurretRotation + Properties.TankRotation);

            if (Tier == TankTier.Commando)
            {
                Model.Meshes["Laser_Beam"].ParentBone.Transform = Matrix.CreateRotationY(Properties.TurretRotation + Properties.TankRotation);
                Model.Meshes["Barrel_Laser"].ParentBone.Transform = Matrix.CreateRotationY(Properties.TurretRotation + Properties.TankRotation);
                Model.Meshes["Dish"].ParentBone.Transform = Matrix.CreateRotationY(Properties.TurretRotation + Properties.TankRotation);
            }

            if (!Properties.Dead && Properties.IsIngame)
            {

                // TargetTankRotation = (GeometryUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - GameUtils.MousePosition).ToRotation() - MathHelper.PiOver2;

                timeSinceLastAction++;

                if (!GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission)
                {
                    Properties.Velocity = Vector2.Zero;
                }
                else
                    DoAi(true, true, true);
            }

            _oldPosition = Properties.Position;

            Model.Root.Transform = World;

            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);
        }

        public override void Remove()
        {
            GameProperties.OnMissionStart -= OnMissionStart;
            Properties.Dead = true;
            Behaviors = null;
            SpecialBehaviors = null;
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Remove();
        }
        public override void Destroy(ITankHurtContext context)
        {
            if (!Client.IsConnected())
            {
                PlayerTank.KillCount++;

                if (!PlayerTank.TanksKillDict.ContainsKey(Tier))
                    PlayerTank.TanksKillDict.Add(Tier, 1);
                else
                    PlayerTank.TanksKillDict[Tier]++;

                if (context.IsPlayer)
                {
                    if (context is TankHurtContext_Bullet cxt1)
                    {
                        //if (cxt.Bounces > 0)
                        TankGame.GameData.BulletKills++;
                        TankGame.GameData.TotalKills++;

                        if (cxt1.Bounces > 0)
                            TankGame.GameData.BounceKills++;

                    }
                    if (context is TankHurtContext_Mine cxt2)
                    {
                        TankGame.GameData.MineKills++;
                        TankGame.GameData.TotalKills++;
                    }

                    TankGame.GameData.TankKills[Tier]++;
                    // TankGame.GameData.KillCountsTiers[(int)Tier] = Tier;

                    var gain = BaseExpValue + GameHandler.GameRand.NextFloat(-(BaseExpValue * 0.2f), BaseExpValue * 0.2f) * GameData.UniversalExpMultiplier;
                    TankGame.GameData.ExpLevel += gain;

                    var p = ParticleSystem.MakeParticle(Position3D + new Vector3(0, 30, 0), $"+{gain:0.00} XP");

                    p.Scale = new(0.5f);
                    p.Roll = MathHelper.Pi;
                    p.Origin2D = TankGame.TextFont.MeasureString($"+{gain:0.00} XP") / 2;

                    p.UniqueBehavior = (p) =>
                    {
                        p.Position.Y += 0.1f;

                        p.Opacity -= 0.01f;

                        if (p.Opacity <= 0)
                            p.Destroy();
                    };
                }


            }
            else
            {
                    // check if player id matches client id, if so, increment that player's kill count, then sync to the server
                    // TODO: convert TankHurtContext into a struct and use it here
                    // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
                    //
                    // if (context.PlayerId == Client.PlayerId)
                    // {
                    //    PlayerTank.KillCount++;
                    //   Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); // not a bad idea actually
            }
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
            base.Destroy(context);
        }
        
        public bool pathBlocked;

        public bool isEnemySpotted;

        private bool seeks;
        private float seekRotation = 0;

        public bool nearDestructibleObstacle;

        // make a new method for just any rectangle
        private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2 rayEndpoint, bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool> pattern = null, bool doBounceReset = true)
        {
            rayEndpoint = new(-999999, -999999);
            List<Tank> tanks = new();
            if (pattern is null)
                pattern = (c) => c.IsSolid || c.Type == Block.BlockType.Teleporter;

            const int MAX_PATH_UNITS = 1000;
            const int PATH_UNIT_LENGTH = 8;

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Properties.Position + offset.RotatedByRadians(-Properties.TurretRotation);

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;
            int pathRicochetCount = 0;

            int uninterruptedIterations = 0;

            bool goneThroughTeleporter = false;
            int tpidx = -1;
            Vector2 tpos = Vector2.Zero;

            for (int i = 0; i < MAX_PATH_UNITS; i++)
            {
                var dummyPos = Vector2.Zero;

                uninterruptedIterations++;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    pathRicochetCount++;
                    resetIterations();
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    pathRicochetCount++;
                    resetIterations();
                }

                var pathHitbox = new Rectangle((int)pathPos.X - 5, (int)pathPos.Y - 5, 8, 8);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, pattern);
                if (corner)
                    return tanks;

                if (block is not null)
                {
                    if (block.Type == Block.BlockType.Teleporter)
                    {
                        if (!goneThroughTeleporter)
                        {
                            var otherTp = Block.AllBlocks.FirstOrDefault(bl => bl != null && bl != block && bl.TpLink == block.TpLink);

                            if (Array.IndexOf(Block.AllBlocks, otherTp) > -1)
                            {
                                //pathPos = otherTp.Position;
                                tpos = otherTp.Position;
                                goneThroughTeleporter = true;
                                tpidx = i + 1;
                            }
                        }
                    }
                    else
                    {
                        switch (dir)
                        {
                            case CollisionDirection.Up:
                            case CollisionDirection.Down:
                                pathDir.Y *= -1;
                                pathRicochetCount++;
                                resetIterations();
                                break;
                            case CollisionDirection.Left:
                            case CollisionDirection.Right:
                                pathDir.X *= -1;
                                pathRicochetCount++;
                                resetIterations();
                                break;
                        }
                    }
                }

                if (goneThroughTeleporter && i == tpidx)
                {
                    pathPos = tpos;
                }

                void resetIterations() { if (doBounceReset) uninterruptedIterations = 0; }

                if (i == 0 && Block.AllBlocks.Any(x => x is not null && x.Hitbox.Intersects(pathHitbox) && pattern is not null ? pattern.Invoke(x) : false))
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                if (i < (int)Properties.ShellSpeed / 2 && pathRicochetCount > 0)
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                if (pathRicochetCount > Properties.RicochetCount)
                {
                    rayEndpoint = pathPos;
                    return tanks;
                }

                pathPos += pathDir;
                var realMiss = 1f + (missDist * uninterruptedIterations);
                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White * 0.9f, 0, whitePixel.Size() / 2, /*2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.1f) * */realMiss, default, default);
                    // DebugUtils.DrawDebugString(TankGame.spriteBatch, $"{goneThroughTeleporter}:{(block is not null ? $"{block.Type}" : "N/A")}", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pathPos.X, 0, pathPos.Y), View, Projection), 1, centered: true);
                }

                foreach (var enemy in GameHandler.AllTanks)
                    if (enemy is not null)
                    {
                        if (!tanks.Contains(enemy))
                        {
                            if (i > 15)
                            {
                                if (Vector2.Distance(enemy.Properties.Position, pathPos) <= realMiss)
                                    tanks.Add(enemy);
                            }
                            else if (enemy.Properties.CollisionBox2D.Intersects(pathHitbox))
                            {
                                tanks.Add(enemy);
                            }
                        }
                    }

            }
            return tanks;
        }

        private bool IsObstacleInWay(int checkDist, Vector2 pathDir, out Vector2 endpoint, out Vector2[] reflectPoints, bool draw = false)
        {
            const int PATH_UNIT_LENGTH = 1;

            bool hasCollided = false;

            var list = new List<Vector2>();

            // 20, 30

            var whitePixel = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
            var pathPos = Properties.Position;

            pathDir.Y *= -1;
            pathDir *= PATH_UNIT_LENGTH;

            for (int i = 0; i < checkDist; i++)
            {
                var dummyPos = Vector2.Zero;

                if (pathPos.X < MapRenderer.MIN_X || pathPos.X > MapRenderer.MAX_X)
                {
                    pathDir.X *= -1;
                    hasCollided = true;
                    list.Add(pathPos);
                }
                if (pathPos.Y < MapRenderer.MIN_Y || pathPos.Y > MapRenderer.MAX_Y)
                {
                    pathDir.Y *= -1;
                    hasCollided = true;
                    list.Add(pathPos);
                }

                var pathHitbox = new Rectangle((int)pathPos.X, (int)pathPos.Y, 1, 1);

                // Why is velocity passed by reference here lol
                Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out var block, out bool corner, false, null);

                switch (dir)
                {
                    case CollisionDirection.Up:
                    case CollisionDirection.Down:
                        hasCollided = true;
                        pathDir.Y *= -1;
                        list.Add(pathPos);
                        break;
                    case CollisionDirection.Left:
                    case CollisionDirection.Right:
                        pathDir.X *= -1;
                        hasCollided = true;
                        list.Add(pathPos);
                        break;
                }

                pathPos += pathDir;

                if (draw)
                {
                    var pathPosScreen = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y), TankGame.GameView, TankGame.GameProjection);
                    TankGame.SpriteRenderer.Draw(whitePixel, pathPosScreen, null, Color.White, 0, whitePixel.Size() / 2, 2 + (float)Math.Sin(i * Math.PI / 5 - TankGame.GameUpdateTime * 0.3f), default, default);
                }
            }
            reflectPoints = list.ToArray();
            endpoint = pathDir;
            return hasCollided;
        }

        private bool _predicts;

        public void UpdateAim(List<Tank> tanksNear, bool fireWhen)
        {
            _predicts = false;
            SeesTarget = false;

            bool tooCloseToExplosiveShell = false;

            List<Tank> tanksDef;

            if (Properties.ShellType == ShellType.Explosive)
            {
                tanksDef = GetTanksInPath(Vector2.UnitY.RotatedByRadians(Properties.TurretRotation - MathHelper.Pi), out var rayEndpoint, offset: Vector2.UnitY * 20, pattern: x => (!x.IsDestructible && x.IsSolid) || x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                if (Vector2.Distance(rayEndpoint, Properties.Position) < 150f) // TODO: change from hardcode to normalcode :YES:
                    tooCloseToExplosiveShell = true;
            }
            else
                tanksDef = GetTanksInPath( 
                    Vector2.UnitY.RotatedByRadians(Properties.TurretRotation - MathHelper.Pi), 
                    out var rayEndpoint, offset: Vector2.UnitY * 20,
                    missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
            if (AiParams.PredictsPositions)
            {
                if (TargetTank is not null)
                {
                    var calculation = Properties.Position.QuickDistance(TargetTank.Properties.Position) / (float)(Properties.ShellSpeed * 1.2f);
                    float rot = -GameUtils.DirectionOf(Properties.Position,
                        GeometryUtils.PredictFuturePosition(TargetTank.Properties.Position, TargetTank.Properties.Velocity, calculation))
                        .ToRotation() - MathHelper.PiOver2;

                    tanksDef = GetTanksInPath(
                    Vector2.UnitY.RotatedByRadians(-GameUtils.DirectionOf(Properties.Position, TargetTank.Properties.Position).ToRotation() - MathHelper.PiOver2),
                    out var rayEndpoint, offset: AiParams.PredictsPositions ? Vector2.Zero : Vector2.UnitY * 20,
                    missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    var targ = GeometryUtils.PredictFuturePosition(TargetTank.Properties.Position, TargetTank.Properties.Velocity, calculation);
                    var posPredict = GetTanksInPath(Vector2.UnitY.RotatedByRadians(rot), out var rayEndpoint2, offset: Vector2.UnitY * 20, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    if (tanksDef.Contains(TargetTank))
                    {
                        _predicts = true;
                        TargetTurretRotation = rot + MathHelper.Pi;
                    }
                }
            }
            var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Properties.Team != Properties.Team || tnk.Properties.Team == TankTeam.NoTeam) && tnk != this);
            var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
            var findsFriendly = tanksDef.Any(tnk => tnk is not null && (tnk.Properties.Team == Properties.Team && tnk.Properties.Team != TankTeam.NoTeam));

            if (findsEnemy && !tooCloseToExplosiveShell)
                SeesTarget = true;

            // ChatSystem.SendMessage($"tier: {tier} | enemy: {findsEnemy} | self: {findsSelf} | friendly: {findsFriendly} | Count: {tanksDef.Count}", Color.White);

            if (AiParams.SmartRicochets)
            {
                //if (!seeks)
                seekRotation += AiParams.TurretSpeed;
                var canShoot = !(CurShootCooldown > 0 || Properties.OwnedShellCount >= Properties.ShellLimit);
                if (canShoot)
                {
                    var tanks = GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, false, default, AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);

                    var findsEnemy2 = tanks.Any(tnk => tnk is not null && (tnk.Properties.Team != Properties.Team || tnk.Properties.Team == TankTeam.NoTeam) && tnk != this);
                    // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                    var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Properties.Team == Properties.Team && tnk.Properties.Team != TankTeam.NoTeam));
                    // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                    if (findsEnemy2/* && !findsFriendly2*/)
                    {
                        seeks = true;
                        TargetTurretRotation = seekRotation - MathHelper.Pi;
                    }
                }

                if (Properties.TurretRotation == TargetTurretRotation || !canShoot)
                    seeks = false;
            }

            bool checkNoTeam = Properties.Team == TankTeam.NoTeam || !tanksNear.Any(x => x.Properties.Team == Properties.Team);

            if (AiParams.PredictsPositions)
            {
                if (SeesTarget && checkNoTeam && fireWhen)
                    if (CurShootCooldown <= 0)
                    {
                        Shoot();
                    }
            }
            else
            {
                if (SeesTarget && checkNoTeam && !findsSelf && !findsFriendly && fireWhen)
                    if (CurShootCooldown <= 0)
                    {
                        Shoot();
                    }
            }
        }

        public Tank TargetTank;

        public void DoAi(bool doMoveTowards = true, bool doMovements = true, bool doFire = true)
        {
            if (GameProperties.InMission)
            {
                for (int i = 0; i < Behaviors.Length; i++)
                    Behaviors[i].Value++;

                var treadPlaceTimer = (int)Math.Round(14 / Properties.Velocity.Length()) != 0 ? (int)Math.Round(14 / Properties.Velocity.Length()) : 1;

                if (Properties.Position - _oldPosition != Vector2.Zero && !Properties.Stationary)
                {
                    if (!Properties.IsSilent)
                    {
                        if (TankGame.GameUpdateTime % MathHelper.Clamp(treadPlaceTimer / 2, 4, 6) == 0)
                        {
                            var treadPlace = GameResources.GetGameResource<SoundEffect>($"Assets/sounds/tnk_tread_place_{GameHandler.GameRand.Next(1, 5)}");
                            var sfx = SoundPlayer.PlaySoundInstance(treadPlace, SoundContext.Effect, 0.05f);
                            sfx.Pitch = Properties.TreadPitch;
                        }
                    }

                    if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                        LayFootprint(Tier == TankTier.White ? true : false);
                }
                enactBehavior = () =>
                {
                    TargetTank = GameHandler.AllTanks.FirstOrDefault(tnk => tnk is not null && !tnk.Properties.Dead && (tnk.Properties.Team != Properties.Team || tnk.Properties.Team == TankTeam.NoTeam) && tnk != this);

                    foreach (var tank in GameHandler.AllTanks)
                    {
                        if (tank is not null && !tank.Properties.Dead && (tank.Properties.Team != Properties.Team || tank.Properties.Team == TankTeam.NoTeam) && tank != this)
                            if (Vector2.Distance(tank.Properties.Position, Properties.Position) < Vector2.Distance(TargetTank.Properties.Position, Properties.Position))
                                if ((tank.Properties.Invisible && tank.timeSinceLastAction < 60) || !tank.Properties.Invisible)
                                    TargetTank = tank;
                    }

                    bool isShellNear = TryGetShellNear(AiParams.ProjectileWarinessRadius, out var shell);
                    bool isMineNear = TryGetMineNear(AiParams.MineWarinessRadius, out var mine);

                    var tanksNearMe = new List<Tank>();
                    var cubesNearMe = new List<Block>();

                    foreach (var tank in GameHandler.AllTanks)
                        if (tank != this && tank is not null && !tank.Properties.Dead && Vector2.Distance(tank.Properties.Position, Properties.Position) <= AiParams.TankWarinessRadius)
                            tanksNearMe.Add(tank);

                    foreach (var block in Block.AllBlocks)
                        if (block is not null && Vector2.Distance(Properties.Position, block.Position) < AiParams.BlockWarinessDistance)
                            cubesNearMe.Add(block);

                    #region TurretHandle

                    TargetTurretRotation %= MathHelper.TwoPi;

                    Properties.TurretRotation %= MathHelper.TwoPi;

                    var diff = TargetTurretRotation - Properties.TurretRotation;

                    if (diff > MathHelper.Pi)
                        TargetTurretRotation -= MathHelper.TwoPi;
                    else if (diff < -MathHelper.Pi)
                        TargetTurretRotation += MathHelper.TwoPi;

                    Properties.TurretRotation = GameUtils.RoughStep(Properties.TurretRotation, TargetTurretRotation, seeks ? AiParams.TurretSpeed * 3 : AiParams.TurretSpeed);
                    bool targetExists = Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null;
                    if (targetExists)
                    {
                        if (!seeks && !_predicts)
                        {
                            if (Behaviors[1].IsModOf(AiParams.TurretMeanderFrequency))
                            {
                                isEnemySpotted = false;
                                if (TargetTank.Properties.Invisible && TargetTank.timeSinceLastAction < 60)
                                {
                                    Aimtarget = TargetTank.Properties.Position;
                                    isEnemySpotted = true;
                                }

                                if (!TargetTank.Properties.Invisible)
                                {
                                    Aimtarget = TargetTank.Properties.Position;
                                    isEnemySpotted = true;
                                }

                                var dirVec = Properties.Position - Aimtarget;
                                TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + GameHandler.GameRand.NextFloat(-AiParams.AimOffset, AiParams.AimOffset);
                            }
                        }

                        if (doFire)
                        {
                            UpdateAim(tanksNearMe, !isMineNear);
                        }
                    }

                    #endregion
                    if (doMovements)
                    {
                        if (Properties.Stationary)
                            return;

                        #region CubeNav
                        if (Behaviors[2].IsModOf(AiParams.BlockReadTime) && !isMineNear && !isShellNear)
                        {
                            pathBlocked = IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-Properties.TargetTankRotation), out var travelPath, out var refPoints);
                            if (pathBlocked)
                            {
                                if (refPoints.Length > 0)
                                {
                                    // float AngleSmoothStep(float angle, float target, float amount) => GameUtils.AngleLerp(angle, target, amount * amount * (3f - 2f * amount));
                                    // why does this never work no matter what i do
                                    var refAngle = GameUtils.DirectionOf(Properties.Position, travelPath - new Vector2(400, 0)).ToRotation();

                                    // AngleSmoothStep(TargetTankRotation, refAngle, refAngle / 3);
                                    GameUtils.RoughStep(ref Properties.TargetTankRotation, Properties.TargetTankRotation <= 0 ? -refAngle : refAngle, refAngle / 6);
                                }
                            }

                            // TODO: i literally do not understand this
                        }

                        #endregion

                        #region GeneralMovement

                        if (!isMineNear && !isShellNear && !Properties.IsTurning && CurMineStun <= 0 && CurShootStun <= 0)
                        {
                            if (!pathBlocked)
                            {
                                if (Behaviors[0].IsModOf(AiParams.MeanderFrequency))
                                {
                                    float dir = -100;

                                    if (targetExists)
                                        dir = GameUtils.DirectionOf(Properties.Position, TargetTank.Properties.Position).ToRotation();

                                    var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                    Properties.TargetTankRotation += random;
                                }
                                if (targetExists)
                                {
                                    if (AiParams.PursuitFrequency != 0)
                                    {
                                        if (Behaviors[0].IsModOf(AiParams.PursuitFrequency))
                                        {
                                            float dir = -100;

                                            if (targetExists)
                                                dir = GameUtils.DirectionOf(Properties.Position, TargetTank.Properties.Position).ToRotation();

                                            var random = GameHandler.GameRand.NextFloat(-AiParams.MeanderAngle / 2, AiParams.MeanderAngle / 2);

                                            var meanderRandom = dir != -100 ? random + (dir + MathHelper.PiOver2) + (0.2f * AiParams.PursuitLevel) : random;

                                            Properties.TargetTankRotation = meanderRandom;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region ShellAvoidance

                        var indif = 3;

                        if (isShellNear)
                        {
                            if (Behaviors[6].IsModOf(indif))
                            {
                                var direction = Vector2.UnitY.RotatedByRadians(shell.Position2D.DirectionOf(Properties.Position, false).ToRotation());

                                Properties.TargetTankRotation = direction.ToRotation();
                            }
                        }

                        #endregion

                        #region MineHandle / MineAvoidance
                        if (Properties.MineLimit > 0)
                        {
                            if (Behaviors[4].IsModOf(60))
                            {
                                if (!tanksNearMe.Any(x => x.Properties.Team == Properties.Team))
                                {
                                    nearDestructibleObstacle = cubesNearMe.Any(c => c.IsDestructible);
                                    if (AiParams.SmartMineLaying)
                                    {
                                        if (nearDestructibleObstacle)
                                        {
                                            Properties.TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ExpandZ().ToRotation();
                                            LayMine();
                                        }
                                    }
                                    else
                                    {
                                        if (GameHandler.GameRand.NextFloat(0, 1) <= AiParams.MinePlacementChance)
                                        {
                                            Properties.TargetTankRotation = new Vector2(100, 100).RotatedByRadians(GameHandler.GameRand.NextFloat(0, MathHelper.TwoPi)).ExpandZ().ToRotation();
                                            LayMine();
                                        }
                                    }
                                }
                            }
                        }
                        if (isMineNear && !isShellNear)
                        {
                            if (Behaviors[5].IsModOf(10))
                            {
                                var direction = Vector2.UnitY.RotatedByRadians(mine.Position.DirectionOf(Properties.Position, false).ToRotation());

                                Properties.TargetTankRotation = direction.ToRotation();
                            }
                        }
                        #endregion

                    }

                    #region Special Tank Behavior

                    if (Tier == TankTier.Creeper)
                    {
                        if (Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null)
                        {
                            float explosionDist = 90f;
                            if (Vector2.Distance(TargetTank.Properties.Position, Properties.Position) < explosionDist)
                            {
                                Destroy(new TankHurtContext_Mine(false, WorldId));

                                new Explosion(Properties.Position, 10f, this, 0.2f);
                            }
                        }
                    }

                    if (Tier == TankTier.Commando)
                    {
                        SpecialBehaviors[0].Value++;
                        if (SpecialBehaviors[0].Value > 500)
                        {
                            SpecialBehaviors[0].Value = 0;

                            var crate = Crate.SpawnCrate(CubeMapPosition.Convert3D(new CubeMapPosition(GameHandler.GameRand.Next(0, CubeMapPosition.MAP_WIDTH),
                                GameHandler.GameRand.Next(0, CubeMapPosition.MAP_HEIGHT))) + new Vector3(0, 500, 0), 2f);
                            crate.TankToSpawn = new TankTemplate()
                            {
                                AiTier = PickRandomTier(),
                                IsPlayer = false,
                                Team = Properties.Team
                            };

                            /*foreach (var mesh in Model.Meshes)
                            {
                                foreach (BasicEffect effect in mesh.Effects)
                                {
                                    if (mesh.Name == "Dish")
                                    {
                                        effect.AmbientLightColor = Color.Red.ToVector3();
                                        effect.SpecularColor = Color.Red.ToVector3();
                                        effect.DiffuseColor = Color.Red.ToVector3();
                                        effect.FogColor = Color.Red.ToVector3();
                                        effect.EmissiveColor = Color.Red.ToVector3();
                                    }
                                }
                            }*/
                        }
                    }

                    #endregion

                    #region TankRotation

                    var targ = Properties.TargetTankRotation + MathHelper.Pi;

                    if (doMoveTowards)
                    {
                        if (Properties.IsTurning)
                        {
                            // var real = TankRotation + MathHelper.PiOver2;
                            if (targ - Properties.TankRotation >= MathHelper.PiOver2)
                                Properties.TankRotation += MathHelper.Pi;
                            else if (targ - Properties.TankRotation <= -MathHelper.PiOver2)
                                Properties.TankRotation -= MathHelper.Pi;
                        }

                        if (Properties.TankRotation > targ - Properties.MaximalTurn - MathHelper.ToRadians(5) && Properties.TankRotation < targ + Properties.MaximalTurn + MathHelper.ToRadians(5))
                        {
                            Properties.IsTurning = false;
                            Properties.Speed += Properties.Acceleration;
                            if (Properties.Speed > Properties.MaxSpeed)
                                Properties.Speed = Properties.MaxSpeed;
                        }
                        else
                        {
                            Properties.Speed -= Properties.Deceleration;
                            if (Properties.Speed < 0)
                                Properties.Speed = 0;
                            treadPlaceTimer = (int)Math.Round(14 / Properties.TurningSpeed) != 0 ? (int)Math.Round(14 / Properties.TurningSpeed) : 1;
                            if (TankGame.GameUpdateTime % treadPlaceTimer == 0)
                            {
                                LayFootprint(Tier == TankTier.White);
                            }
                            Properties.IsTurning = true;
                        }

                        var dir = Vector2.UnitY.RotatedByRadians(Properties.TankRotation);
                        Properties.Velocity.X = dir.X;
                        Properties.Velocity.Y = dir.Y;

                        Properties.Velocity.Normalize();

                        Properties.Velocity *= Properties.Speed;
                        Properties.TankRotation = GameUtils.RoughStep(Properties.TankRotation, targ, Properties.TurningSpeed);
                    }

                    #endregion
                };
            }
            enactBehavior?.Invoke();
        }
        public override void Render()
        {
            TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            DrawExtras();
            if (Properties.Invisible && GameProperties.InMission)
                return;

            for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = i == 0 ? boneTransforms[mesh.ParentBone.Index] : boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                        effect.View = View;
                        effect.Projection = Projection;

                        effect.TextureEnabled = true;

                        if (!Properties.HasTurret)
                            if (mesh.Name == "Cannon")
                                return;

                        if (mesh.Name == "Shadow")
                        {
                            if (!Lighting.AccurateShadows)
                            {
                                effect.Texture = _shadowTexture;
                                if (Properties.IsIngame)
                                {
                                    effect.Alpha = 0.5f;
                                    mesh.Draw();
                                }
                            }
                        }
                        else
                        {
                            if (Properties.IsHoveredByMouse)
                                effect.EmissiveColor = Color.White.ToVector3();
                            else
                                effect.EmissiveColor = Color.Black.ToVector3();

                            var tex = _tankTexture;

                            effect.Texture = tex;

                            effect.Alpha = 1;
                            mesh.Draw();
                            /*var ex = new Color[1024];

                            Array.Fill(ex, new Color(GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256), GameHandler.GameRand.Next(0, 256)));

                            effect.Texture.SetData(0, new Rectangle(0, 8, 32, 15), ex, 0, 480);*/

                            /*if (Team != Team.NoTeam)
                            {
                                var ex = new Color[1024];

                                Array.Fill(ex, (Color)typeof(Color).GetProperty(Team.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null));

                                tex.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                                tex.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                            }*/
                            // removed temporarily
                        }

                        effect.SetDefaultGameLighting_IngameEntities();
                    }
                }
            }

            base.Render();
        }
        private void DrawExtras()
        {
            if (Properties.Dead)
                return;

            if (Properties.IsIngame)
            {
                if (DebugUtils.DebugLevel == 1)
                {
                    float calculation = 0f;

                    if (AiParams.PredictsPositions && TargetTank is not null)
                    {
                        calculation = Properties.Position.QuickDistance(TargetTank.Properties.Position) / (float)(Properties.ShellSpeed * 1.2f);
                    }

                    if (AiParams.SmartRicochets)
                        GetTanksInPath(Vector2.UnitY.RotatedByRadians(seekRotation), out var rayEndpoint, true, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    var poo = GetTanksInPath(Vector2.UnitY.RotatedByRadians(Properties.TurretRotation - MathHelper.Pi), out var rayEnd, true, offset: Vector2.UnitY * 20, pattern: x => x.IsSolid | x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    if (AiParams.PredictsPositions)
                    {
                        float rot = -GameUtils.DirectionOf(Properties.Position, TargetTank is not null ?
                            GeometryUtils.PredictFuturePosition(TargetTank.Properties.Position, TargetTank.Properties.Velocity, calculation) :
                            Aimtarget).ToRotation() - MathHelper.PiOver2;
                        GetTanksInPath(Vector2.UnitY.RotatedByRadians(rot), out var rayEnd2, true, Vector2.Zero, pattern: x => x.IsSolid | x.Type == Block.BlockType.Teleporter, missDist: AiParams.Inaccuracy, doBounceReset: AiParams.BounceReset);
                    }
                    DebugUtils.DrawDebugString(TankGame.SpriteRenderer, $"{Tier}: {poo.Count}", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), World, View, Projection), 1, centered: true);
                    if (!Properties.Stationary)
                    {
                        IsObstacleInWay(AiParams.BlockWarinessDistance, Vector2.UnitY.RotatedByRadians(-Properties.TargetTankRotation), out var travelPos, out var refPoints, true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "TRAVELENDPOINT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 1, centered: true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "ENDPOINT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(rayEnd.X, 11, rayEnd.Y), View, Projection), 1, centered: true);

                        /*var pos = GameUtils.DirectionOf(Position, travelPos);
                        var rot = pos.ToRotation();
                        var posNew = new Vector2(50, 0).RotatedByRadians(rot);
                        DebugUtils.DrawDebugString(TankGame.spriteBatch, "here?",
                            GeometryUtils.ConvertWorldToScreen(new(posNew.X, 11, posNew.Y), 
                            Matrix.CreateTranslation(Position.X, 
                            0, Position.Y), View, Projection), 
                            1, centered: true);*/
                        
                        foreach (var pt in refPoints)
                        {
                            DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "pt", GeometryUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.X, 0, pt.Y), View, Projection), 1, centered: true);
                        }

                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GameUtils.DirectionOf(Properties.Position, travelPos).X, 0, GameUtils.DirectionOf(Properties.Position, travelPos).Y), View, Projection), 1, centered: true);
                        DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "me", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.Identity, View, Projection/*Matrix.CreateTranslation(Position.X, 11, Position.Y), View, Projection)*/), 1, centered: true);
                        //TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new Rectangle((int)travelPos.X - 1, (int)travelPos.Y - 1, 20, 20), Color.White);

                        // draw future
                        // DebugUtils.DrawDebugString(TankGame.spriteBatch, "FUT", GeometryUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation).ExpandZ() + new Vector3(0, 11, 0)), View, Projection), 1, centered: true);
                    }

                }
            }

            if (Properties.Invisible && GameProperties.InMission)
                return;

            Properties.Armor?.Render();
        }

        public bool TryGetShellNear(float distance, out Shell shell)
        {
            shell = null;

            foreach (var bullet in Shell.AllShells)
            {
                if (bullet is not null)
                {
                    if (bullet.LifeTime > 30)
                    {
                        if (Vector2.Distance(Properties.Position, bullet.Position2D) < distance)
                        {
                            var rotationTo = GameUtils.DirectionOf(Properties.Position, bullet.Position2D).ToRotation();

                            if (Math.Abs(rotationTo - Properties.TurretRotation) < MathHelper.PiOver2 /*|| Vector2.Distance(Position, bullet.Position2D) < distance / 2*/)
                            {
                                shell = bullet;
                                return true;
                            }
                        }
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
                    if (Vector2.Distance(Properties.Position, yours.Position) < distance)
                    {
                        mine = yours;
                        return true;
                    }
                }
            }
            return false;
        }

        private static readonly TankTier[] workingTiers =
        {
            TankTier.Brown, TankTier.Marine, TankTier.Yellow, TankTier.Black, TankTier.White, TankTier.Pink, TankTier.Purple, TankTier.Green, TankTier.Ash,
            TankTier.Bronze, TankTier.Silver, TankTier.Sapphire, TankTier.Ruby, TankTier.Citrine, TankTier.Amethyst, TankTier.Emerald, TankTier.Gold, TankTier.Obsidian,
            TankTier.Granite, TankTier.Bubblegum, TankTier.Water, TankTier.Crimson, TankTier.Tiger, TankTier.Creeper, TankTier.Gamma, TankTier.Marble,
            TankTier.Assassin
        };
        public static TankTier PickRandomTier()
            => workingTiers[GameHandler.GameRand.Next(0, workingTiers.Length)];
    }
}