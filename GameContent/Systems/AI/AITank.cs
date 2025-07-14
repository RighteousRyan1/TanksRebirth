using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.GameMechanics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.Internals.Common.Framework.Collision;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.Globals.Assets;
using System.Runtime.InteropServices.Marshalling;

namespace TanksRebirth.GameContent;

#pragma warning disable CA2211
public partial class AITank : Tank {
    private Texture2D? _tankTexture;
    private static Texture2D? _shadowTexture;

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
    /// <summary>Changes this <see cref="AITank"/> to a completely different type of tank. Should only be used in special cases.</summary>
    /// <param name="tier">The new tier that this tank will be.</param>
    /// <param name="setDefaults">Whether or not to set the associated defaults of this tank in accordance to <paramref name="tier"/>.</param>
    public void Swap(int tier, bool setDefaults = true) {
        AiTankType = tier;

        var tierName = TankID.Collection.GetKey(tier)!.ToLower();

        //if (tier <= TankID.Marble)
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
    /// <param name="setTankDefaults">Whether or not to give this <see cref="AITank"/> the default values.</param>
    /// <param name="isIngame">Whether or not this <see cref="AITank"/> is a gameplay tank or a cosmetic tank (i.e: display models on menus, etc).</param>
    public AITank(int tier, bool setTankDefaults = true, bool isIngame = true) {
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

        if (setTankDefaults)
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

        // what the fuck?
        if (properties.Stationary) {
            properties.Deceleration = 0.2f;
        }

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
        if (Client.IsHost() || (!Client.IsConnected() && !Dead) || MainMenuUI.Active) {
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
            GameHandler.AllAITanks[AITankId] = null;
            GameHandler.AllTanks[WorldId] = null;
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
    private void GiveXP() {
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

    public bool IsPathBlocked;

    public bool IsEnemySpotted;

    private bool isSeeking;
    private float seekRotation;

    public bool IsNearDestructibleObstacle;

    // make a new method for just any rectangle

    // TODO: literally fix everything about these turret rotation values.
    private List<Tank> GetTanksInPath(Vector2 pathDir, out Vector2[] ricochetPoints, out Vector2[] tankCollPoints,
        bool draw = false, Vector2 offset = default, float missDist = 0f, Func<Block, bool>? pattern = null, bool doBounceReset = true) {
        const int MAX_PATH_UNITS = 1000;
        const int PATH_UNIT_LENGTH = 8;

        List<Tank> tanks = [];
        List<Vector2> ricoPoints = [];
        List<Vector2> tnkPoints = [];

        pattern ??= c => c.Properties.IsSolid || c.Type == BlockID.Teleporter;

        var whitePixel = TextureGlobals.Pixels[Color.White];
        Vector2 pathPos = Position + offset.Rotate(-TurretRotation);
        pathDir.Y *= -1;
        pathDir *= PATH_UNIT_LENGTH;

        int ricochetCount = 0;
        int uninterruptedIterations = 0;

        bool teleported = false;
        int tpTriggerIndex = -1;
        Vector2 teleportedTo = Vector2.Zero;

        var pathHitbox = new Rectangle();

        for (int i = 0; i < MAX_PATH_UNITS; i++) {
            uninterruptedIterations++;

            // World bounds check
            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                ricoPoints.Add(pathPos);
                pathDir.X *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }
            else if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                ricoPoints.Add(pathPos);
                pathDir.Y *= -1;
                ricochetCount++;
                if (doBounceReset) uninterruptedIterations = 0;
            }

            // Setup hitbox once
            pathHitbox.X = (int)pathPos.X - 5;
            pathHitbox.Y = (int)pathPos.Y - 5;
            pathHitbox.Width = 8;
            pathHitbox.Height = 8;

            Vector2 dummy = Vector2.Zero;
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummy, out var dir, out var block, out bool corner, false, pattern);
            if (corner) break;

            if (block is not null) {
                if (block.Type == BlockID.Teleporter && !teleported) {
                    var dest = Block.AllBlocks.FirstOrDefault(bl => bl != null && bl != block && bl.TpLink == block.TpLink);
                    if (dest is not null) {
                        teleported = true;
                        teleportedTo = dest.Position;
                        tpTriggerIndex = i + 1;
                    }
                }
                else if (block.Properties.AllowShotPathBounce) {
                    ricoPoints.Add(pathPos);
                    ricochetCount += block.Properties.PathBounceCount;

                    switch (dir) {
                        case CollisionDirection.Up:
                        case CollisionDirection.Down:
                            pathDir.Y *= -1;
                            break;
                        case CollisionDirection.Left:
                        case CollisionDirection.Right:
                            pathDir.X *= -1;
                            break;
                    }

                    if (doBounceReset) uninterruptedIterations = 0;
                }
            }

            // Delay teleport until next frame
            if (teleported && i == tpTriggerIndex) {
                pathPos = teleportedTo;
            }

            // Check destroy conditions
            bool hitsInstant = i == 0 && Block.AllBlocks.Any(x => x != null && x.Hitbox.Intersects(pathHitbox) && pattern(x));
            bool hitsTooEarly = i < (int)Properties.ShellSpeed / 2 && ricochetCount > 0;
            bool ricochetLimitReached = ricochetCount > Properties.RicochetCount;

            if (hitsInstant || hitsTooEarly || ricochetLimitReached)
                break;

            // Check tanks BEFORE moving
            float realMiss = 1f + (missDist * uninterruptedIterations);

            foreach (var enemy in GameHandler.AllTanks) {
                if (enemy is null || enemy.Dead || tanks.Contains(enemy)) continue;

                if (i > 15 && GameUtils.Distance_WiiTanksUnits(enemy.Position, pathPos) <= realMiss) {
                    var pathAngle = pathDir.ToRotation();
                    var toEnemy = MathUtils.DirectionTo(pathPos, enemy.Position).ToRotation();

                    if (MathUtils.AbsoluteAngleBetween(pathAngle, toEnemy) >= MathHelper.PiOver2)
                        tanks.Add(enemy);
                }

                var pathCircle = new Circle { Center = pathPos, Radius = 4 };
                if (enemy.CollisionCircle.Intersects(pathCircle)) {
                    tnkPoints.Add(pathPos);
                    tanks.Add(enemy);
                }
            }

            if (draw) {
                var screenPos = MatrixUtils.ConvertWorldToScreen(
                    Vector3.Zero,
                    Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y),
                    CameraGlobals.GameView,
                    CameraGlobals.GameProjection
                );

                TankGame.SpriteRenderer.Draw(
                    whitePixel,
                    screenPos,
                    null,
                    Color.White * 0.5f,
                    0,
                    whitePixel.Size() / 2,
                    realMiss,
                    default,
                    default
                );
            }

            pathPos += pathDir;
        }

        tankCollPoints = [.. tnkPoints];
        ricochetPoints = [.. ricoPoints];
        return tanks;
    }

    public Vector2 PathEndpoint;

    // reworked tahnkfully
    private bool IsObstacleInWay(uint checkDist, Vector2 pathDir, out Vector2 endpoint, out RaycastReflection[] reflections, int size = 1, bool draw = false) {
        bool hasCollided = false;

        var whitePixel = TextureGlobals.Pixels[Color.White];
        Vector2 pathPos = Position;
        endpoint = Position;
        reflections = [];

        // dynamic approach instead.... if Speed is zero then don't do anything
        // if pathing doesn't work right then blame this xd
        // if (Speed == 0) return false;

        List<RaycastReflection> reflectionList = [];

        // until fix/correction is made
        pathDir *= 8; //Speed;

        // Avoid constant allocation
        Rectangle pathHitbox = new();
        Vector2 dummyPos = Vector2.Zero;

        for (int i = 0; i < checkDist; i++) {
            // Check X bounds
            if (pathPos.X < GameScene.MIN_X || pathPos.X > GameScene.MAX_X) {
                pathDir.X *= -1;
                hasCollided = true;
                reflectionList.Add(new(pathPos, Vector2.UnitY));
            }

            // Check Y bounds
            if (pathPos.Y < GameScene.MIN_Z || pathPos.Y > GameScene.MAX_Z) {
                pathDir.Y *= -1;
                hasCollided = true;
                reflectionList.Add(new(pathPos, -Vector2.UnitY));
            }

            pathHitbox.X = (int)pathPos.X;
            pathHitbox.Y = (int)pathPos.Y;
            pathHitbox.Width = size;
            pathHitbox.Height = size;

            // Simplified collision logic
            Collision.HandleCollisionSimple_ForBlocks(pathHitbox, pathDir, ref dummyPos, out var dir, out _, out _, false);

            if (dir != CollisionDirection.None) {
                hasCollided = true;
                Vector2 normal = dir switch {
                    CollisionDirection.Up => -Vector2.UnitY,
                    CollisionDirection.Down => Vector2.UnitY,
                    CollisionDirection.Left => Vector2.UnitX,
                    CollisionDirection.Right => -Vector2.UnitX,
                    _ => Vector2.Zero
                };

                if (dir is CollisionDirection.Left or CollisionDirection.Right)
                    pathDir.X *= -1;
                else
                    pathDir.Y *= -1;

                reflectionList.Add(new(pathPos, normal));
            }

            if (draw) {
                var screenPos = MatrixUtils.ConvertWorldToScreen(
                    Vector3.Zero,
                    Matrix.CreateTranslation(pathPos.X, 11, pathPos.Y),
                    CameraGlobals.GameView,
                    CameraGlobals.GameProjection
                );

                TankGame.SpriteRenderer.Draw(
                    whitePixel,
                    screenPos,
                    null,
                    Color.White,
                    0,
                    whitePixel.Size() / 2,
                    new Vector2(size, size),
                    default,
                    default
                );
            }

            pathPos += pathDir;
        }

        reflections = [.. reflectionList];
        PathEndpoint = endpoint = pathPos;
        return hasCollided;
    }

    private bool _predicts;
    /// <summary>The location(s) of which this tank's shot path hits an obstacle.</summary>
    public Vector2[] ShotPathRicochetPoints { get; private set; } = [];
    /// <summary>The location(s) of which this tank's shot path hits an tank.</summary>
    public Vector2[] ShotPathTankCollPoints { get; private set; } = [];

    // TODO: make view distance, and make tanks in path public
    public void UpdateAim() {
        _predicts = false;
        SeesTarget = false;

        bool tooCloseToExplosiveShell = false;

        List<Tank> tanksDef;

        if (Properties.ShellType == ShellID.Explosive) {
            tanksDef = GetTanksInPath(Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi), out var ricP, out var tnkCol, offset: Vector2.UnitY * 20, pattern: x => (!x.Properties.IsDestructible && x.Properties.IsSolid) || x.Type == BlockID.Teleporter, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            if (GameUtils.Distance_WiiTanksUnits(ricP[^1], Position) < 150f) // TODO: change from hardcode to normalcode :YES:
                tooCloseToExplosiveShell = true;
        }
        else {
            tanksDef = GetTanksInPath(
                Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi),
                out var ricP, out var tnkCol, offset: Vector2.UnitY * 20,
                missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

            TanksSpotted = [.. tanksDef];

            ShotPathRicochetPoints = ricP;
            ShotPathTankCollPoints = tnkCol;
        }
        if (AiParams.PredictsPositions) {
            if (TargetTank is not null) {
                var calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);
                float rot = -MathUtils.DirectionTo(Position,
                    GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation))
                    .ToRotation() - MathHelper.PiOver2;

                tanksDef = GetTanksInPath(
                Vector2.UnitY.Rotate(-MathUtils.DirectionTo(Position, TargetTank.Position).ToRotation() - MathHelper.PiOver2),
                out var ricP, out var tnkCol, offset: AiParams.PredictsPositions ? Vector2.Zero : Vector2.UnitY * 20,
                missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                var targ = GeometryUtils.PredictFuturePosition(TargetTank.Position, TargetTank.Velocity, calculation);
                var posPredict = GetTanksInPath(Vector2.UnitY.Rotate(rot),
                    out var ricP1, out var tnkCol2, offset: Vector2.UnitY * 20, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                if (tanksDef.Contains(TargetTank)) {
                    _predicts = true;
                    TargetTurretRotation = rot + MathHelper.Pi;
                }
            }
        }

        // TODO: is findsSelf even necessary? findsEnemy is only true if findsSelf is false. eh, whatever. my brain is fucked.
        var findsEnemy = tanksDef.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
        var findsSelf = tanksDef.Any(tnk => tnk is not null && tnk == this);
        var findsFriendly = tanksDef.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));

        if (findsEnemy && !tooCloseToExplosiveShell)
            SeesTarget = true;

        if (AiParams.SmartRicochets) {
            //if (!seeks)
            seekRotation += AiParams.TurretSpeed * 0.5f;
            var canShoot = !(CurShootCooldown > 0 || OwnedShellCount >= Properties.ShellLimit);
            if (canShoot) {
                var tanks = GetTanksInPath(Vector2.UnitY.Rotate(seekRotation), out var ricP, out var tnkCol, false, default, AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);

                var findsEnemy2 = tanks.Any(tnk => tnk is not null && (tnk.Team != Team || tnk.Team == TeamID.NoTeam) && tnk != this);
                // var findsSelf2 = tanks.Any(tnk => tnk is not null && tnk == this);
                // var findsFriendly2 = tanks.Any(tnk => tnk is not null && (tnk.Team == Team && tnk.Team != TeamID.NoTeam));
                // ChatSystem.SendMessage($"{findsEnemy2} {findsFriendly2} | seek: {seeks}", Color.White);
                if (findsEnemy2/* && !findsFriendly2*/) {
                    isSeeking = true;
                    TurretRotationMultiplier = 3f;
                    TargetTurretRotation = seekRotation - MathHelper.Pi;
                }
            }

            if (TurretRotation == TargetTurretRotation || !canShoot)
                isSeeking = false;
        }

        bool checkNoTeam = Team == TeamID.NoTeam || !TanksNear.Any(x => x.Team == Team);

        // tanks wont shoot when fleeing from a mine
        if (ClosestDanger is Mine)
            if (AiParams.CantShootWhileFleeing)
                return;

        if (Behaviors[2].IsModOf(CurrentRandomShoot)) {
            CurrentRandomShoot = Client.ClientRandom.Next(AiParams.RandomTimerMinShoot, AiParams.RandomTimerMaxShoot);

            if (AiParams.PredictsPositions) {
                if (SeesTarget && checkNoTeam)
                    if (CurShootCooldown <= 0)
                        Shoot(false);
            }
            else {
                if (SeesTarget && checkNoTeam && !findsSelf && !findsFriendly)
                    if (CurShootCooldown <= 0)
                        Shoot(false);
            }
        }
    }

    public Tank? TargetTank;

    public float TurretRotationMultiplier = 1f;

    public bool AutoEnactAIBehavior = true;

    public static int TankPathCheckSize = 3;

    public bool DoAttack = true;
    public bool DoMovements = true;
    public bool DoMoveTowards = true;

    public bool IsInDanger;

    public List<Tank> TanksNear = [];
    public List<Block> BlocksNear = [];

    public List<IAITankDanger> NearbyDangers;
    public IAITankDanger? ClosestDanger;

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
        if (ClosestDanger is Shell shell) {
            var dist = shell.IsPlayerSourced ? AiParams.AwarenessHostileShell : AiParams.AwarenessFriendlyShell;

            // hardcode heaven :)
            if (!shell.IsHeadingTowards(Position, dist, MathHelper.Pi)) {
                ClosestDanger = null;
            }
        }
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
                if (tank != this && tank is not null && !tank.Dead && GameUtils.Distance_WiiTanksUnits(tank.TurretPosition, Position) <= AiParams.DetectionRadiusShellFriendly)
                    TanksNear.Add(tank);

            foreach (var block in Block.AllBlocks)
                if (block is not null && GameUtils.Distance_WiiTanksUnits(Position, block.Position) < AiParams.ObstacleAwarenessMovement)
                    BlocksNear.Add(block);

            var isShellNear = IsInDanger && ClosestDanger is Shell;
            var isMineNear = IsInDanger && ClosestDanger is Mine;
            var isExplosionNear = IsInDanger && ClosestDanger is Explosion;

            // only use if checking the respective boolean!
            var shell = (ClosestDanger as Shell)!;
            var mine = (ClosestDanger as Mine)!;
            var explosion = (ClosestDanger as Explosion)!;

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

                // null danger so nothing exists?
                if (ClosestDanger is null /*danger is not Mine && danger is not Shell*/) {
                    DoMovement();
                }
                if (Properties.MineLimit > 0) {
                    TryMineLay();
                }

                if (isExplosionNear)
                    ExplosionAvoid(explosion);
            }

            #region TankRotation

            // i really hope to remove this hardcode.
            if (DoMoveTowards) {
                // this is repeated in AITank for less obfuscation.
                // also why the random 5 degrees?
                var negDif = TargetTankRotation - Properties.MaximalTurn - MathHelper.ToRadians(5);
                var posDif = TargetTankRotation + Properties.MaximalTurn + MathHelper.ToRadians(5);

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
    public int CurrentRandomMineLay;
    public int CurrentRandomShoot;
    public int CurrentRandomMove;
    public Tank? GetClosestTarget() {
        Tank? target = null;
        var targetPosition = new Vector2(float.MaxValue);
        foreach (var tank in GameHandler.AllTanks) {
            if (tank is not null && !tank.Dead && (tank.Team != Team || tank.Team == TeamID.NoTeam) && tank != this) {
                if (GameUtils.Distance_WiiTanksUnits(tank.Position, Position) < GameUtils.Distance_WiiTanksUnits(targetPosition, Position)) {
                    // var closeness = Vector2.Distance(tank.Position, Position);
                    if ((tank.Properties.Invisible && tank.timeSinceLastAction < 60) /*|| closeness <= Block.SIDE_LENGTH*/ || !tank.Properties.Invisible) {
                        target = tank;
                        targetPosition = tank.Position;
                    }
                }
            }
        }
        return target;
    }
    public Tank? TryOverrideTarget() {
        Tank? target = TargetTank;
        if (GameHandler.AllPlayerTanks.Any(x => x is not null && x.Team == Team)) {
            foreach (var ping in IngamePing.AllIngamePings) {
                if (ping is null) break;
                if (ping.TrackedTank is null) break;
                if (ping.TrackedTank.Team == Team) break; // no friendly fire
                target = ping.TrackedTank;
            }
        }
        return target;
    }
    public void HandleTurret() {
        TargetTurretRotation %= MathHelper.TwoPi;

        TurretRotation %= MathHelper.TwoPi;

        var diff = TargetTurretRotation - TurretRotation;

        if (diff > MathHelper.Pi)
            TargetTurretRotation -= MathHelper.TwoPi;
        else if (diff < -MathHelper.Pi)
            TargetTurretRotation += MathHelper.TwoPi;
        bool targetExists = Array.IndexOf(GameHandler.AllTanks, TargetTank) > -1 && TargetTank is not null;
        if (targetExists) {
            if (!isSeeking && !_predicts) {
                if (Behaviors[1].IsModOf(AiParams.TurretMovementTimer)) {
                    IsEnemySpotted = false;
                    if (TargetTank!.Properties.Invisible && TargetTank.timeSinceLastAction < 60) {
                        AimTarget = TargetTank.Position;
                        IsEnemySpotted = true;
                    }

                    if (!TargetTank.Properties.Invisible) {
                        AimTarget = TargetTank.Position;
                        IsEnemySpotted = true;
                    }
                }
            }
            if (DoAttack)
                UpdateAim();
        }
        if (Behaviors[1].IsModOf(AiParams.TurretMovementTimer)) {
            var dirVec = Position - AimTarget;
            TargetTurretRotation = -dirVec.ToRotation() - MathHelper.PiOver2 + Client.ClientRandom.NextFloat(-AiParams.AimOffset, AiParams.AimOffset);
        }
        TurretRotation = MathUtils.RoughStep(TurretRotation, TargetTurretRotation, AiParams.TurretSpeed * TurretRotationMultiplier * RuntimeData.DeltaTime);
    }
    public void DoMovement() {
        bool shouldMove = !IsTurning && CurMineStun <= 0 && CurShootStun <= 0 && !IsPathBlocked;
        //Console.WriteLine(shouldMove);
        if (shouldMove) {
            if (Behaviors[0].IsModOf(CurrentRandomMove)) {
                DoBlockNav(); // determines IsPathBlocked

                CurrentRandomMove = Client.ClientRandom.Next(AiParams.RandomTimerMinMove, AiParams.RandomTimerMaxMove);

                if (!IsPathBlocked) {
                    var random = Client.ClientRandom.NextFloat(-AiParams.MaxAngleRandomTurn, AiParams.MaxAngleRandomTurn);

                    TargetTankRotation += random;
                }

                foreach (var danger in NearbyDangers) {
                    //if (danger.Priority == DangerPriority.VeryHigh) {
                    //    break;
                    //}
                    //if (danger.Priority == DangerPriority.High) {
                    if (danger is Explosion expl) {
                        ExplosionAvoid(expl);
                        break;
                    }
                    if (danger is Mine mine) {
                        MineAvoid(mine);
                        break;
                    }
                    if (danger is Shell shell) {
                        ShellAvoid(shell);
                    }
                }

                // aggression/pursuit handling
                if (TargetTank is not null) {
                    var targetDirVector = Vector2.Normalize(MathUtils.DirectionTo(Position, TargetTank!.Position));
                    var dirDirVector = Vector2.Normalize(MathUtils.DirectionTo(Position, PathEndpoint));

                    var finalVector = dirDirVector + targetDirVector * AiParams.AggressivenessBias;

                    // negative plus pi/2...?
                    TargetTankRotation = finalVector.ToRotation();
                }
            }
        }
    }
    public void TryMineLay() {
        if (!TanksNear.Any(x => x.Team == Team)) {
            IsNearDestructibleObstacle = BlocksNear.Any(c => c.Properties.IsDestructible);
            if (AiParams.SmartMineLaying) {
                if (IsNearDestructibleObstacle) {
                    LayMine();
                }
            }
            else {
                // attempt via an opportunity to lay a mine
                if (Client.ClientRandom.NextFloat(0, 1) <= (IsNearDestructibleObstacle ? AiParams.ChanceMineLayNearBreakables : AiParams.ChanceMineLay)) {
                    // ensure no blocks are within iParams.MineObstacleAwareness

                    LayMine();

                    // check cardinals for a good movement direction
                    // but how...? i'll find out

                    CurrentRandomMineLay = Client.ClientRandom.Next(AiParams.RandomTimerMinMine, AiParams.RandomTimerMaxMine);
                }
            }
        }
    }
    public void ShellAvoid(Shell shell) {
        // TODO: add all shells that may or may not be near, average their position and make the tank go away from that position
        if (CurMineStun <= 0 && CurShootStun <= 0) {
            var direction = -Vector2.UnitY.Rotate(shell.Position.DirectionTo(Position).ToRotation());
            TargetTankRotation = direction.ToRotation();
        }
    }
    public void MineAvoid(Mine mine) {
        var dist = mine.IsPlayerSourced ? AiParams.AwarenessHostileMine : AiParams.AwarenessFriendlyMine;
        var direction = -Vector2.UnitY.Rotate(mine.Position.DirectionTo(Position).ToRotation());

        TargetTankRotation = direction.ToRotation();
    }
    public void ExplosionAvoid(Explosion explosion) {
        //var dist = explosion.IsPlayerSourced ? AiParams.MineWarinessRadius_PlayerLaid : AiParams.MineWarinessRadius_AILaid;
        var direction = -Vector2.UnitY.Rotate(explosion.Position.DirectionTo(Position).ToRotation());
        TargetTankRotation = direction.ToRotation();
    }
    public void DoBlockNav() {
        uint framesLookAhead = AiParams.ObstacleAwarenessMovement / 2;
        var tankDirection = Vector2.UnitY.Rotate(TargetTankRotation);
        IsPathBlocked = IsObstacleInWay(framesLookAhead, tankDirection, out var travelPath, out var reflections, TankPathCheckSize);
        if (IsPathBlocked) {
            if (reflections.Length > 0) {
                // up = PiOver2
                //var normalRotation = reflections[0].Normal.RotatedByRadians(TargetTankRotation);
                var dirOf = MathUtils.DirectionTo(travelPath, Position).ToRotation();
                var refAngle = dirOf + MathHelper.PiOver2;

                // this is a very bandaid fix....
                if (refAngle % MathHelper.PiOver2 == 0) {
                    refAngle += Client.ClientRandom.NextFloat(0, MathHelper.TwoPi);
                }
                TargetTankRotation = refAngle;
            }

            // TODO: i literally do not understand this
        }
    }
    public void DoDeflection(Shell shell) {
        var calculation = (Position.Distance(shell.Position) - 20f) / (float)(Properties.ShellSpeed * 1.2f);
        float rot = -MathUtils.DirectionTo(Position,
            GeometryUtils.PredictFuturePosition(shell.Position, shell.Velocity, calculation))
            .ToRotation() + MathHelper.PiOver2;

        TargetTurretRotation = rot;

        TurretRotationMultiplier = 4f;

        // used to be rot %=... was it necessary?
        //TargetTurretRotation %= MathHelper.Tau;

        //if ((-TurretRotation + MathHelper.PiOver2).IsInRangeOf(TargetTurretRotation, 0.15f))

        Shoot(false);
    }
    public override void Render() {
        base.Render();
        if (Dead || !GameScene.ShouldRenderAll)
            return;
        TankGame.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        DrawExtras();
        if ((MainMenuUI.Active && Properties.Invisible) || (Properties.Invisible && CampaignGlobals.InMission))
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
    
    private void DrawExtras() {
        if (Dead)
            return;

        // did i ever make any good programming choices before this past year or so?
        if (DebugManager.DebugLevel == DebugManager.Id.EntityData) {
            float calculation = 0f;

            if (AiParams.PredictsPositions && TargetTank is not null)
                calculation = Position.Distance(TargetTank.Position) / (float)(Properties.ShellSpeed * 1.2f);

            if (AiParams.SmartRicochets)
                GetTanksInPath(Vector2.UnitY.Rotate(seekRotation), out var ricP1, out var tnkCol1, true, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            // maybe not necessary. store from the cpu, draw on the gpu.
            var poo = GetTanksInPath(Vector2.UnitY.Rotate(TurretRotation - MathHelper.Pi), out var ricP2, out var tnkCol2, true, offset: Vector2.UnitY * 20, pattern: x => x.Properties.IsSolid | x.Type == BlockID.Teleporter, missDist: AiParams.DetectionForgivenessHostile, doBounceReset: AiParams.BounceReset);
            if (AiParams.PredictsPositions) {
                float rot = -MathUtils.DirectionTo(Position, TargetTank is not null ?
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
        }
        if (DebugManager.DebugLevel == DebugManager.Id.NavData && !Properties.Stationary) {
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
    // this might need to be redone completely because different dangers have difernernejakswklfsadkolf dasjkl fsadjklsaf dkjhlsfda jhknas dfjhkbsadf jhkbsadf jhkfsa djkhsa fd
    public bool TryGetDangerNear(float distance, out List<IAITankDanger> dangersNear, out IAITankDanger? dClosest) {
        IAITankDanger? closest = null;
        dangersNear = [];

        Span<IAITankDanger> dangers = Dangers.ToArray();

        ref var dangersSearchSpace = ref MemoryMarshal.GetReference(dangers);

        for (var i = 0; i < Dangers.Count; i++) {
            var currentDanger = Unsafe.Add(ref dangersSearchSpace, i);

            if (currentDanger is null) continue;

            var distanceToDanger = GameUtils.Distance_WiiTanksUnits(Position, currentDanger.Position);

            if (!(distanceToDanger < distance)) continue;

            dangersNear.Add(currentDanger);

            if (closest == null || distanceToDanger <
                GameUtils.Distance_WiiTanksUnits(Position, closest.Position)) {
                closest = currentDanger;
            }
        }

        dClosest = closest;
        return closest != null;
    }
    public static int PickRandomTier() => Server.ServerRandom.Next(0, TankID.Collection.Count);
}