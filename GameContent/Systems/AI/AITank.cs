using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.TankSystem;
using tainicom.Aether.Physics2D.Dynamics;

namespace TanksRebirth.GameContent.Systems.AI;

#pragma warning disable CA2211
public partial class AITank : Tank {
    Texture2D? _tankTexture;
    static Texture2D? _shadowTexture;

    public ModTank? ModdedData { get; private set; }
    /// <summary>A list of all active dangers on the map to <see cref="AITank"/>s. Includes <see cref="Shell"/>s, <see cref="Mine"/>s,
    /// and <see cref="Explosion"/>s by default. To make an AI Tank behave towards any thing you would like, make it inherit from <see cref="IAITankDanger"/>
    /// and change the tank's behavior when running away by hooking into <see cref="WhileDangerDetected"/>.</summary>
    public static readonly List<IAITankDanger> Dangers = [];

    public delegate void PostExecuteAI(AITank tank);
    public static event PostExecuteAI? OnPostUpdateAI;

    public delegate void DangerDetected(AITank tank, IAITankDanger danger);
    public static event DangerDetected? WhileDangerDetected;

    public delegate void InstancedDestroy();
    public event InstancedDestroy? OnDestroy;
    /// <summary>Each of these keep track of certain behaviors that take place during the AI Cycle, including, but not limited to:<para></para>
    /// Navigation, Shell/Mine avoidance, Mine Laying, Shell Shooting</summary>
    public AiBehavior[] Behaviors { get; private set; }
    /// <summary>Each of these are for super special tanks (by default). The currently unimplemented tanks have special abilities that
    /// use this.</summary>
    public AiBehavior[] SpecialBehaviors;
    /// <summary>The AI Tank Tier/Type of this <see cref="AITank"/>. For instance, a Brown tank would be <see cref="TankID.Brown"/>.</summary>
    public int AiTankType;
    /// <summary>The invoked method for performing the actions of the tank's AI.</summary>
    public Action? AIBehaviorAction;
    /// <summary>The position of this <see cref="AITank"/> in the <see cref="GameHandler.AllAITanks"/> array.</summary>
    public int AITankId { get; private set; }
    /// <summary>Only use if you know what you're doing!</summary>
    /// <param name="newId">The new ID to be assigned to this <see cref="AITank"/>.</param>
    public void ReassignId(int newId) => AITankId = newId;
    /// <summary>The colors of the explosion particles when this <see cref="AITank"/> is destroyed.</summary>
    public static Dictionary<int, Color> TankDestructionColors = new() {
        [TankID.Brown] = new(152, 96, 26),
        [TankID.Ash] = Color.Gray,
        [TankID.Marine] = Color.Teal,
        [TankID.Yellow] = Color.Yellow,
        [TankID.Pink] = Color.HotPink,
        [TankID.Green] = Color.LimeGreen,
        [TankID.Violet] = Color.Purple,
        [TankID.White] = Color.White,
        [TankID.Black] = Color.Black,
        [TankID.Bronze] = new(152, 96, 26),
        [TankID.Silver] = Color.Silver,
        [TankID.Sapphire] = Color.DeepSkyBlue,
        [TankID.Ruby] = Color.IndianRed,
        [TankID.Citrine] = Color.Yellow,
        [TankID.Amethyst] = Color.Purple,
        [TankID.Emerald] = Color.Green,
        [TankID.Gold] = Color.Gold,
        [TankID.Obsidian] = Color.Black,
    };
    /// <summary>Change the texture of this <see cref="AITank"/>.</summary>
    /// <param name="texture">The new texture.</param>
    public void SwapTankTexture(Texture2D texture) => _tankTexture = texture;
    /// <summary>The AI parameter collection of this AI Tank.</summary>
    public AiParameters AiParams { get; set; } = new();
    /// <summary>The position of the target this <see cref="AITank"/> is currently attempting to aim at.</summary>
    public Vector2 AimTarget { get; set; }
    /// <summary>Whether or not this tank sees its target. Generally should not be set, but the tank will shoot if able when this is true.</summary>
    public bool SeesTarget { get; set; }
    /// <summary>The target rotation for this tank's turret. <see cref="Tank.TurretRotation"/> will move towards this value at a rate of <see cref="AiParameters.TurretSpeed"/>.</summary>
    public float TargetTurretRotation { get; set; }
    /// <summary>The default XP value to award to players if a player destroys this <see cref="AITank"/>.</summary>
    public float BaseExpValue { get; set; }
    public bool AutoEnactAIBehavior = true;
    /// <summary>Changes this <see cref="AITank"/> to a completely different type of tank. Should only be used in special cases.</summary>
    /// <param name="tier">The new tier that this tank will be.</param>
    /// <param name="setDefaults">Whether or not to set the associated defaults of this tank in accordance to <paramref name="tier"/>.</param>
    public void Swap(int tier, bool setDefaults = true) {
        AiTankType = tier;

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();

        SwapTankTexture(Assets[$"tank_" + tierName]);

        if (!UsesCustomModel)
            Model = ModelGlobals.TankEnemy.Asset;

        if (setDefaults)
            ApplyDefaults(ref Properties);
    }

    /// <summary>
    /// Creates a new <see cref="AITank"/>.
    /// </summary>
    /// <param name="tier">The tier of this <see cref="AITank"/>.</param>
    /// <param name="applyDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
    /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
    public AITank(int tier, bool applyDefaults = true, bool isIngame = true) {
        IsIngame = isIngame;
        // TargetTankRotation = MathHelper.Pi;
        if (IsIngame) {
            // looking at this code makes me want to barf.
            // maybe move this stuff to events within Difficulties.cs
            if (Difficulties.Types["BumpUp"])
                tier++;
            if (Difficulties.Types["Monochrome"])
                tier = Difficulties.MonochromeValue;
            if (Difficulties.Types["MasterModBuff"])
                tier = Difficulties.VanillaToMasterModeConversions[tier];
        }

        SpecialBehaviors = [];
        NearbyDangers = [];

        AiTankType = tier;

        Behaviors = new AiBehavior[10];

        for (int i = 0; i < Behaviors.Length; i++)
            Behaviors[i] = new();

        Behaviors[0].Label = "TankChassisMovement";
        Behaviors[1].Label = "TankTurretMovement";
        Behaviors[2].Label = "TankShellFire";
        Behaviors[3].Label = "TankMinePlacement";


        // create modded data
        for (int i = 0; i < ModLoader.ModTanks.Length; i++) {
            var modTank = ModLoader.ModTanks[i];

            // associate values properly for modded data
            if (AiTankType == modTank.Type) {
                ModdedData = modTank.Clone();
                ModdedData.AITank = this;
            }
        }

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();
        if (RuntimeData.IsMainThread) {
            if (!UsesCustomModel) {
                Model = ModelGlobals.TankEnemy.Asset;
                var tnkAsset = Assets[$"tank_" + tierName];

                var t = new Texture2D(TankGame.Instance.GraphicsDevice, tnkAsset.Width, tnkAsset.Height);

                var colors = new Color[tnkAsset.Width * tnkAsset.Height];

                tnkAsset.GetData(colors);

                t.SetData(colors);

                _tankTexture = t;
            }
        }
        else
            _tankTexture = Assets[$"tank_" + tierName];

        _shadowTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/tank_shadow");

        if (applyDefaults)
            ApplyDefaults(ref Properties);

        int index = Array.IndexOf(GameHandler.AllAITanks, null);

        if (index < 0) return;

        AITankId = index;

        GameHandler.AllAITanks[index] = this;

        int index2 = Array.IndexOf(GameHandler.AllTanks, null);

        if (index2 < 0) {
            WorldId = -1;
            GC.Collect(); // guh?
            return;
        }

        WorldId = index2;

        GameHandler.AllTanks[index2] = this;

        base.Initialize();
    }
    public override void ApplyDefaults(ref TankProperties properties) {
        properties.DestructionColor = TankDestructionColors[AiTankType];
        AiParams = AIManager.GetAiDefaults(AiTankType, out float baseXp);
        Properties = AIManager.GetAITankProperties(AiTankType);
        BaseExpValue = baseXp;

        // initialize these so we don't divide by zero in modulus operations
        CurrentRandomMove = Client.ClientRandom.Next(AiParams.RandomTimerMinMove, AiParams.RandomTimerMaxMove);
        CurrentRandomMineLay = Client.ClientRandom.Next(AiParams.RandomTimerMinMine, AiParams.RandomTimerMaxMine);
        CurrentRandomShoot = Client.ClientRandom.Next(AiParams.RandomTimerMinShoot, AiParams.RandomTimerMaxShoot);

        // unfortunately these are just miserable
        if (Difficulties.Types["TanksAreCalculators"])
            if (properties.RicochetCount >= 1)
                if (properties.HasTurret)
                    AiParams.SmartRicochets = true;

        if (Difficulties.Types["UltraMines"])
            AiParams.AwarenessHostileMine *= 3;

        if (Difficulties.Types["AllInvisible"]) {
            properties.Invisible = true;
            properties.CanLayTread = false;
        }
        if (Difficulties.Types["AllStationary"])
            properties.Stationary = true;

        if (Difficulties.Types["AllHoming"]) {
            properties.ShellHoming = new() {
                Radius = 200f,
                Speed = properties.ShellSpeed,
                Power = 0.1f * properties.ShellSpeed
            };
            // ShellHoming.isHeatSeeking = true;

            AiParams.DetectionForgivenessHostile *= 4;
        }

        if (Difficulties.Types["BulletBlocking"])
            AiParams.DeflectsBullets = true;

        if (Difficulties.Types["Armored"]) {
            if (properties.Armor is null)
                properties.Armor = new(this, 3);
            else
                properties.Armor = new(this, properties.Armor.HitPoints + 3);
        }

        if (Difficulties.Types["Predictions"])
            AiParams.PredictsPositions = true;
        properties.TreadVolume = 0.05f;

        base.ApplyDefaults(ref properties);

        ModdedData?.PostApplyDefaults();

        if (AiParams.ObstacleAwarenessMine > 0) {
            NearbyObstaclesChecker = CollisionsWorld.CreateCircle(GameUtils.Value_WiiTanksUnits(AiParams.ObstacleAwarenessMine) / 2 / UNITS_PER_METER, 0f, Physics.Position, BodyType.Static);
            NearbyObstaclesChecker.OnCollision += FindNearbyObstacles;
            NearbyObstaclesChecker.OnSeparation += OnSeparationOfNearbyObstacles;
        }
    }

    public int NumBlocksNearby;

    private void OnSeparationOfNearbyObstacles(Fixture sender, Fixture other, tainicom.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        NumBlocksNearby--;
    }

    private bool FindNearbyObstacles(Fixture sender, Fixture other, tainicom.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        sender.Body.Tag = TankDestructionColors[AiTankType];
        NumBlocksNearby++;
        // other.Body.Tag = TankDestructionColors[AiTankType];
        // never actually collide but do return data for use.
        return false;
    }

    public override void Update() {
        if (ModLoader.Status != LoadStatus.Complete)
            return;

        base.Update();

        //CannonMesh.ParentBone.Transform = Matrix.CreateRotationY(TurretRotation + TankRotation + (Flip ? MathHelper.Pi : 0));

        if (Dead || !GameScene.ShouldRenderAll)
            return;

        if (DebugManager.SuperSecretDevOption) {
            var tnkGet = Array.FindIndex(GameHandler.AllAITanks, x => x is not null && !x.Dead && !x.Properties.Stationary);
            if (tnkGet > -1)
                if (AITankId == GameHandler.AllAITanks[tnkGet]!.AITankId)
                    TargetTankRotation = (MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection) - MouseUtils.MousePosition).ToRotation() + MathHelper.PiOver2;
        }
        // do ai only if host, and send ai across the interweb
        if (Client.IsHost() || !Client.IsConnected() && !Dead || MainMenuUI.Active) {
            timeSinceLastAction++;

            if (!MainMenuUI.Active)
                if (!CampaignGlobals.InMission || IntermissionSystem.IsAwaitingNewMission || LevelEditorUI.Active)
                    Velocity = Vector2.Zero;
            DoAi();

            if (IsIngame)
                Client.SyncAITank(this);
        }
        if (!Client.IsHost() && Client.IsConnected()) {
            HandleTankMetaData();
        }
    }
    public override void PreUpdate() {
        ModdedData?.PreUpdate();
    }
    public override void PostUpdate() {
        ModdedData?.PostUpdate();
    }
    public override void Remove(bool nullifyMe) {
        if (nullifyMe) {
            GameHandler.AllAITanks[AITankId] = null!;
            GameHandler.AllTanks[WorldId] = null!;
        }

        // remove the checker and related events 
        if (CollisionsWorld.BodyList.Contains(NearbyObstaclesChecker)) {
            CollisionsWorld.Remove(NearbyObstaclesChecker);
            NearbyObstaclesChecker!.OnCollision -= FindNearbyObstacles;
            NearbyObstaclesChecker!.OnSeparation -= OnSeparationOfNearbyObstacles;
        }

        base.Remove(nullifyMe);
    }
    public override void Destroy(ITankHurtContext context, bool netSend) {
        // might not account for level testing via the level editor?
        OnDestroy?.Invoke();

        const string tankDestroySound1 = "Assets/sounds/tnk_destroy_enemy.ogg";
        SoundPlayer.PlaySoundInstance(tankDestroySound1, SoundContext.Effect, 0.3f);

        var aiDeathMark = new TankDeathMark(TankDeathMark.CheckColor.White) {
            Position = Position3D + new Vector3(0, 0.1f, 0),
        };

        aiDeathMark.StoredTank = new TankTemplate {
            AiTier = AiTankType,
            IsPlayer = false,
            Position = aiDeathMark.Position.FlattenZ(),
            Rotation = TankRotation,
            Team = Team,
        };

        base.Destroy(context, netSend);

        if (MainMenuUI.Active) return;
        if (LevelEditorUI.Active) return;
        if (LevelEditorUI.Editing) return;
        if (context.Source is null && Client.IsConnected()) return;

        // count enemy team-kills only in single player
        if (context.Source is not PlayerTank && Client.IsConnected()) return;

        // var player = context.Source as PlayerTank;

        if (!PlayerTank.TankKills.TryGetValue(AiTankType, out int value1))
            PlayerTank.TankKills.Add(AiTankType, 1);
        else
            PlayerTank.TankKills[AiTankType] = ++value1;

        // this code lowkey hurts to read
        // check if less than certain values for different value coins

        if (context is TankHurtContextShell cxtShell) {
            TankGame.SaveFile.BulletKills++;

            // if the ricochets remaining is less than the ricochets the bullet has, it has bounced at least once.
            if (cxtShell.Shell.RicochetsRemaining < cxtShell.Shell.Ricochets)
                TankGame.SaveFile.BounceKills++;
        }
        if (context is TankHurtContextExplosion) {
            TankGame.SaveFile.MineKills++;
        }

        // pretty sure != null isn't necessary because if Source is null it can't convert
        // it knows the owner is not me but increments my kill count anyway
        if (context.Source is PlayerTank p) {
            var myId = NetPlay.GetMyClientId();

            bool isMe = p.PlayerId == myId;
            //Console.WriteLine($"p.PlayerId ({p.PlayerId}) == myId ({myId}) ----> {isMe}");
            if (isMe)
                PlayerTank.KillCounts[myId]++;
        }

        TankGame.SaveFile.TotalKills++;

        if (TankGame.SaveFile.TankKills.TryGetValue(AiTankType, out uint value))
            TankGame.SaveFile.TankKills[AiTankType] = ++value;

        GiveXP();
        // check if player id matches client id, if so, increment that player's kill count, then sync to the server
        // TODO: convert TankHurtContext into a struct and use it here
        // Will be used to track the reason of death and who caused the death, if any tank owns a shell or mine
        //
        // if (context.PlayerId == Client.PlayerId)
        // {
        //    PlayerTank.KillCount++;
        //    Client.Send(new TankKillCountUpdateMessage(PlayerTank.KillCount)); // not a bad idea actually
    }
    void GiveXP() {
        if (!LevelEditorUI.Editing) {
            var rand = Client.ClientRandom.NextFloat(-(BaseExpValue * 0.25f), BaseExpValue * 0.25f);
            var gain = BaseExpValue + rand;
            // i will keep this commented if anything else happens.
            //var gain = (BaseExpValue + rand) * GameData.UniversalExpMultiplier;
            GameHandler.ExperienceBar.GainExperience(gain);

            var p = GameHandler.Particles.MakeParticle(Position3D + new Vector3(0, 30, 0), $"+{gain * 100:0.00} XP");

            p.Scale = new(0.5f);
            p.Pitch = MathHelper.Pi + MouseUtils.Test.X;
            p.FaceTowardsMe = CameraGlobals.IsUsingFirstPresonCamera;
            p.Origin2D = FontGlobals.RebirthFont.MeasureString($"+{gain * 100:0.00} XP") / 2;

            p.UniqueBehavior = (p) => {
                p.Position.Y += 0.1f * RuntimeData.DeltaTime;

                p.Alpha -= 0.01f * RuntimeData.DeltaTime;

                if (p.Alpha <= 0)
                    p.Destroy();
            };
        }
    }
    public void HandleTankMetaData() {
        TargetTank = GetClosestTarget();

        // get ai to target a player's ping
        TargetTank = TryOverrideTarget();

        // measure the biggest WarinessRadius, player or ai, then check the larger, then do manual calculations.
        var radii = new float[] { AiParams.AwarenessFriendlyMine, AiParams.AwarenessHostileMine, AiParams.AwarenessFriendlyShell, AiParams.AwarenessHostileShell };
        var biggest = radii.Max();
        IsInDanger = TryGetDangerNear(biggest, out NearbyDangers, out ClosestDanger);

        if (IsInDanger) {
            WhileDangerDetected?.Invoke(this, ClosestDanger!);
            ModdedData?.DangerDetected(ClosestDanger!);
        }

        // if the danger is a shell, recalculate the desire to dodge
        /*if (ClosestDanger is Shell shell) {
            var dist = shell.IsPlayerSourced ? AiParams.AwarenessHostileShell : AiParams.AwarenessFriendlyShell;

            // hardcode heaven :)
            if (!shell.IsHeadingTowards(Position, dist, MathHelper.Pi)) {
                ClosestDanger = null;
            }
        }*/
    }
    public void DoAi() {
        TanksNear.Clear();
        BlocksNear.Clear();

        if (!MainMenuUI.Active && !CampaignGlobals.InMission)
            return;

        TurretRotationMultiplier = 1f;
        // AiParams.DeflectsBullets = true;
        for (int i = 0; i < Behaviors.Length; i++)
            Behaviors[i].Value += RuntimeData.DeltaTime;

        // defining an Action isn't that intensive, right?
        AIBehaviorAction = () => {
            HandleTankMetaData();

            foreach (var tank in GameHandler.AllTanks)
                if (tank != this && tank is not null && !tank.Dead && GameUtils.Distance_WiiTanksUnits(tank.TurretPosition, Position) <= AiParams.TankAwarenessShoot)
                    TanksNear.Add(tank);

            foreach (var block in Block.AllBlocks)
                if (block is not null && GameUtils.Distance_WiiTanksUnits(Position, block.Position) < AiParams.ObstacleAwarenessMovement)
                    BlocksNear.Add(block);

            var isShellNear = IsInDanger && ClosestDanger is Shell;

            // only use if checking the respective boolean!
            var shell = (ClosestDanger as Shell)!;

            if (AiParams.DeflectsBullets) {
                if (isShellNear) {
                    DoDeflection(shell);
                }
            }

            HandleTurret();
            if (DoMovements) {
                if (Properties.Stationary)
                    return;

                // facing down = 0 radians/2pi radians

                // "DoMovement" handles danger avoidance.
                DoMovement();

                // checks if it is entirely unable to lay mines first
                TryMineLay();
            }

            #region TankRotation

            // i really hope to remove this hardcode.
            if (DoMoveTowards) {
                // this is repeated in AITank for less obfuscation.
                // also why the random 5 degrees?
                var negDif = TargetTankRotation - Properties.MaximalTurn;
                var posDif = TargetTankRotation + Properties.MaximalTurn;

                IsTurning = !(TankRotation > negDif && TankRotation < posDif);

                if (!IsTurning) {
                    Speed += Properties.Acceleration * RuntimeData.DeltaTime;

                    if (Speed > Properties.MaxSpeed)
                        Speed = Properties.MaxSpeed;
                }
                else {
                    Speed *= Properties.Deceleration * RuntimeData.DeltaTime;
                }
                if (TargetTankRotation > MathHelper.Tau)
                    TargetTankRotation -= MathHelper.Tau;
                if (TargetTankRotation < 0)
                    TargetTankRotation += MathHelper.Tau;

                var dir = Vector2.UnitY.Rotate(TankRotation);
                Velocity.X = dir.X;
                Velocity.Y = dir.Y;

                Velocity.Normalize();

                Velocity *= Speed;
                TankRotation = MathUtils.RoughStep(TankRotation, TargetTankRotation, Properties.TurningSpeed * RuntimeData.DeltaTime);
            }

            #endregion
        };
        if (AutoEnactAIBehavior)
            AIBehaviorAction?.Invoke();
    }

    public BasicEffect TankBasicEffectHandler = new(TankGame.Instance.GraphicsDevice);
    public override void Render() {
        base.Render();
        if (Dead || !GameScene.ShouldRenderAll)
            return;
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        DrawExtras();
        if (MainMenuUI.Active && Properties.Invisible || Properties.Invisible && CampaignGlobals.InMission)
            return;
        if (Model is null)
            return;

        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (ModelMesh mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = i == 0 ? _boneTransforms[mesh.ParentBone.Index] : _boneTransforms[mesh.ParentBone.Index] * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;

                    if (!Properties.HasTurret)
                        if (mesh.Name == "Cannon")
                            return;

                    if (mesh.Name == "Shadow") {
                        if (!Lighting.AccurateShadows) {
                            effect.Texture = _shadowTexture;
                            effect.Alpha = 0.5f;
                            mesh.Draw();
                        }
                        continue;
                    }

                    if (IsHoveredByMouse)
                        effect.EmissiveColor = Color.White.ToVector3();
                    else
                        effect.EmissiveColor = Color.Black.ToVector3();

                    effect.Texture = _tankTexture;
                    effect.Alpha = 1;

                    // TODO: uncomment code when disabling implementation is re-implemented.

                    if (ShowTeamVisuals) {
                        if (Team != TeamID.NoTeam) {
                            var ex = new Color[1024];

                            Array.Fill(ex, TeamID.TeamColors[Team]);

                            effect.Texture?.SetData(0, new Rectangle(0, 0, 32, 9), ex, 0, 288);
                            effect.Texture?.SetData(0, new Rectangle(0, 23, 32, 9), ex, 0, 288);
                        }
                    }

                    effect.SetDefaultGameLighting_IngameEntities(0.9f);
                    mesh.Draw();
                }
            }
        }
    }

    public void DrawAwarenessCircle(BasicEffect effect, float awareness, Color color) {
        if (awareness <= 0)
            return;

        const int circleResolution = 32;

        float radius = GameUtils.Value_WiiTanksUnits(awareness + TNK_WIDTH) / 2f;
        float heightOffset = 0.2f; // Slightly above the tank Y to prevent z-fighting

        VertexPositionColor[] vertices = new VertexPositionColor[circleResolution + 1];

        for (int i = 0; i <= circleResolution; i++) {
            float angle = MathHelper.TwoPi * i / circleResolution;
            float x = MathF.Cos(angle) * radius;
            float z = MathF.Sin(angle) * radius;

            var worldPos = new Vector3(Position.X + x, heightOffset, Position.Y + z);
            vertices[i] = new VertexPositionColor(worldPos, color);
        }

        effect.World = Matrix.Identity;
        effect.View = View;
        effect.Projection = Projection;

        effect.VertexColorEnabled = true;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
            pass.Apply();
            effect.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, circleResolution);
        }
    }
    private void DrawExtras() {
        if (Dead)
            return;

        // did i ever make any good programming choices before this past year or so?
        // this code looks like it was written by a 12 year old with a broken arm - GitHub Copilot
        // even ai hates my code.
        if (DebugManager.DebugLevel == DebugManager.Id.AIData) {
            float calculation = 0f;

            var drawInfo = new Dictionary<(string Name, float Value), Color>() {
                [(nameof(AiParams.ObstacleAwarenessMine), AiParams.ObstacleAwarenessMine)] = Color.Yellow,
                [(nameof(AiParams.ObstacleAwarenessMovement), AiParams.ObstacleAwarenessMovement)] = Color.Purple,

                [(nameof(AiParams.AwarenessFriendlyShell), AiParams.AwarenessFriendlyShell)] = Color.Green,
                [(nameof(AiParams.AwarenessFriendlyMine), AiParams.AwarenessFriendlyMine)] = Color.LimeGreen,

                [(nameof(AiParams.AwarenessHostileShell), AiParams.AwarenessHostileShell)] = Color.DarkRed,
                [(nameof(AiParams.AwarenessHostileMine), AiParams.AwarenessHostileMine)] = Color.Red,

                [(nameof(AiParams.TankAwarenessShoot), AiParams.TankAwarenessShoot)] = Color.Blue,
                [(nameof(AiParams.TankAwarenessMine), AiParams.TankAwarenessMine)] = Color.Cyan,

                [(nameof(NumBlocksNearby), NumBlocksNearby)] = Color.Orange,
            };

            // NOTE: cross-product with target rotation vector and rotation vector gives you whether or not (negative or positive)
            // the tank needs to rotate clockwise or counter-clockwise

            for (int i = 0; i < drawInfo.Count; i++) {
                var info = drawInfo.ElementAt(i);

                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) - new Vector2(0, i * 20);
                DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, $"{info.Key.Name}: {info.Key.Value}", pos, info.Value, Color.White,
                    Vector2.One * 0.75f, 0f, borderThickness: 0.25f);
                DrawAwarenessCircle(TankBasicEffectHandler, info.Key.Value, info.Value);
            }

            drawInfo.Clear();

            if (AiParams.PredictsPositions && TargetTank is not null)
                calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);

            if (AiParams.SmartRicochets)
                GetTanksInPath(Vector2.UnitY.Rotate(_seekRotation), out var ricP1, out var tnkCol1, true, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            // maybe not necessary. store from the cpu, draw on the gpu.
            var poo = GetTanksInPath(Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi), out var ricP2, out var tnkCol2, true, offset: Vector2.UnitY * 20, pattern: x => x.Properties.IsSolid | x.Type == BlockID.Teleporter, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            if (AiParams.PredictsPositions) {
                float rot = -Position.DirectionTo(TargetTank is not null ?
                    GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation) :
                    AimTarget).ToRotation() - MathHelper.PiOver2;
                GetTanksInPath(Vector2.UnitY.Rotate(rot), out var ricP3, out var tnkCol3, true, Vector2.Zero, pattern: x => x.Properties.IsSolid | x.Type == BlockID.Teleporter, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            }
            for (int i = 0; i < ricP2.Length; i++) {
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"ric{i}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0),
                    Matrix.CreateTranslation(ricP2[i].X, 0, ricP2[i].Y), View, Projection), 1, centered: true);
            }
            for (int i = 0; i < tnkCol2.Length; i++) {
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, $"col{i}", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0),
                    Matrix.CreateTranslation(tnkCol2[i].X, 0, tnkCol2[i].Y), View, Projection), 1, centered: true);
            }

            /*for (int i = 0; i < info.Length; i++) {
                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Up * 20, World, View, Projection) -
                    new Vector2(0, (i * 20));
                DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, info[i], pos,
                    Color.Aqua, Color.Black, new Vector2(0.5f).ToResolution(), 0f, Anchor.TopCenter, 0.6f);
            }*/
        }
        if (DebugManager.DebugLevel == DebugManager.Id.AIData && !Properties.Stationary) {
            // magical numbers too lazy, look at update method to define
            IsObstacleInWay(AiParams.ObstacleAwarenessMovement / 2, Vector2.UnitY.Rotate(TargetTankRotation), out var travelPos, out var refPoints, TankPathCheckSize, draw: true);
            DebugManager.DrawDebugString(TankGame.SpriteRenderer, "TEP", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(travelPos.X, 11, travelPos.Y), View, Projection), 6, centered: true);
            foreach (var pt in refPoints)
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, "pt", MatrixUtils.ConvertWorldToScreen(new Vector3(0, 11, 0), Matrix.CreateTranslation(pt.ReflectionPoint.X, 0, pt.ReflectionPoint.Y), View, Projection), 6, centered: true);


            //DebugUtils.DrawDebugString(TankGame.SpriteRenderer, "end", MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(MathUtils.DirectionOf(travelPos, Position).X, 0, MathUtils.DirectionOf(travelPos, Position).Y), View, Projection), 6, centered: true);
        }

        if (Properties.Invisible && (CampaignGlobals.InMission || MainMenuUI.Active))
            return;

        Properties.Armor?.Render();
    }
    public static int PickRandomTier() => Server.ServerRandom.Next(0, TankID.Collection.Count);
}