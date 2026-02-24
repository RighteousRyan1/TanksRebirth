using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Cosmetics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Animation;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

#pragma warning disable

// ikik, not in the UI namespace but whatever
public static class CosmeticsUI {
    static float _interp;
    static bool _isOpening;
    static BoundingSphere _clickSpot;
    static Particle _dispPart;
    static List<Particle> _keys = [];
    static Particle _movingKey;
    static Particle _hoveredKey;

    static Animator _keyAnimation;

    // todo: implement!
    static Animator _unboxAnimation;


    static float _prevTotalAnim;

    public static bool IsActive => MainMenuUI.MenuState == MainMenuUI.UIState.Cosmetics;
    public static RenderableChest Chest;

    internal static PlayerTank displayTank;

    public static void Initialize() {
        Chest = new(new(0, 0, 0), CameraGlobals.GameView, CameraGlobals.GameProjection);
        Chest.Rotation = new Vector3(0, 0, MathHelper.Pi + MathHelper.PiOver4);
    }

    public static void Update() {
        Chest.DrawParams.View = CameraGlobals.GameView;
        Chest.DrawParams.Projection = CameraGlobals.GameProjection;

        _interp += (_isOpening ? 0.015f : -0.015f) * RuntimeData.DeltaTime;
        _interp = MathHelper.Clamp(_interp, 0, 1);

        Chest.Scale = 0.3f;
        Chest.ChestPosition = new Vector3(-875f, 992.81537f, 2860f);

        var basePos = Chest.ChestPosition - new Vector3(15, 23, 15);
        _clickSpot = new BoundingSphere(Chest.KeySlotPos, 6);

        displayTank?.Update();

        displayTank ??= new(playerType: PlayerID.Blue, ignoreRegister: true);
        displayTank.Position = Chest.ChestPosition.FlattenZ();
        displayTank.OffsetY = Chest.ChestPosition.Y + 15 + 40;
        displayTank.ChassisRotation = MathHelper.PiOver2 * 3 - MathHelper.PiOver4;

        // like why negative... gonna shoot myself mayhaps.
        displayTank.TurretRotation = -displayTank.ChassisRotation;

        HandleInputs();
        if (_keyAnimation is not null) {
            // start things
            if (_keyAnimation.TotalProgress > 0.6f && _prevTotalAnim <= 0.6f) {
                _isOpening = true;

                var prop = FuckingGamble(VanillaCosmetics.LootPool, out float percent);
                var rarity = VanillaCosmetics.GetRarityFromFloat(percent);
                Console.WriteLine($"{prop.Name} | {rarity} | {percent:0.00}");

                displayTank.AddCosmetic(prop);



                /*Particle cosPart;

                if (prop is Prop3D p3d) {
                    cosPart = GameHandler.Particles.MakeParticle(Chest.ChestPosition, p3d.PropModel.Asset, p3d.ModelTexture);
                    cosPart.Scale = Vector3.One * prop.Scale;
                }
                else {
                    cosPart = GameHandler.Particles.MakeParticle(Chest.ChestPosition, ((Prop2D)prop).Texture);
                    cosPart.Scale = new Vector3(0.4f) * prop.Scale;
                }
                
                // for some reason lighting just... isnt applied. ok. whatever. fix later.
                cosPart.Alpha = 1f;
                cosPart.HasAdditiveBlending = false;

                // this might just need to be a way to render a cosmetic at a position rather than a particle explicitly
                cosPart.UniqueBehavior = (p) => {
                    // p.Position.Y += 0.1f * RuntimeData.DeltaTime;
                    p.Position = Chest.ChestPosition + new Vector3(0, 75, 0);
                    if (p.LifeTime > 180)
                        p.Destroy();


                };*/
            }

            _prevTotalAnim = _keyAnimation.TotalProgress;
        }
        Chest.LidRotation = new Vector3(0, Easings.ComputeEase
            (_isOpening ? EasingFunction.OutBounce : EasingFunction.OutSine, _interp) * (MathHelper.Pi + MathHelper.PiOver4 / 2), 
            0);
    }
    public static void UpdateActive() {
        // yadda yadda
    }

    public static void HandleInputs() {
        // Don't interact if an animation is already playing
        if (_movingKey is not null || _keys.Count == 0) return;

        var ray = RayUtils.GetMouseToWorldRay();

        // Find the key currently under the mouse
        _hoveredKey = null;
        float closestDist = float.MaxValue;

        foreach (var key in _keys) {
            // Create a bounding sphere around the key for interaction
            // Radius 12 fits the visual scale of 40 roughly well
            var sphere = new BoundingSphere(key.Position, 12f);
            var inter = ray.Intersects(sphere);

            if (inter.HasValue && inter.Value < closestDist) {
                closestDist = inter.Value;
                _hoveredKey = key;
            }
        }

        if (InputUtils.Click() && _hoveredKey is not null) {
            var ypr = Matrix.CreateFromYawPitchRoll(Chest.Rotation.Z, Chest.Rotation.Y, Chest.Rotation.X);
            var preSlotPos = Chest.KeySlotPos + Vector3.Transform(new Vector3(0, 0, 50), ypr);
            var lookAt = MathUtils.GetLookAtEulerAngles(preSlotPos, Chest.KeySlotPos);

            float[] slotRot = [lookAt.Roll, lookAt.Pitch, lookAt.Yaw];

            // Set the moving key to the one we clicked
            _movingKey = _hoveredKey;

            _keyAnimation = Animator.Create()
                // start
                .WithFrame(new(_movingKey.Position, Vector3.One, floats: [_movingKey.Roll, _movingKey.Pitch, _movingKey.Yaw]))
                .WithFrame(new(preSlotPos, Vector3.One, duration: TimeSpan.FromSeconds(1), easing: EasingFunction.InOutQuad, floats: slotRot))
                .WithFrame(new(Chest.KeySlotPos, Vector3.One, duration: TimeSpan.FromSeconds(2), floats: slotRot, easing: EasingFunction.InOutCubic))
                .WithFrame(new(Chest.KeySlotPos, Vector3.One, duration: TimeSpan.FromSeconds(0.5), floats: slotRot, easing: EasingFunction.InOutCubic))
                // what the fuck is this rotational magic??? rotating just one axis doesn't work at all
                //.WithFrame(new(Chest.KeySlotPos, Vector3.One, duration: TimeSpan.FromSeconds(2), floats: [slotRot[0] - MathHelper.PiOver2, slotRot[1] - MathHelper.PiOver2, slotRot[2] + MathHelper.PiOver2]))
                .WithFrame(new(Chest.KeySlotPos, Vector3.One, duration: TimeSpan.FromSeconds(1), floats: slotRot, easing: EasingFunction.InOutCubic))

                // exit
                .WithFrame(new(preSlotPos, Vector3.One, duration: TimeSpan.FromSeconds(1), floats: slotRot, easing: EasingFunction.InOutCubic));

            _keyAnimation.Run();
        }
    }
    public static void EnterMenu() {
        var pos = Chest.ChestPosition;

        _dispPart = GameHandler.Particles.MakeParticle(Vector3.Zero, 
            string.Format(TankGame.GameLanguage.Misc.KeysCount, TankGame.SaveFile.CollectedKeys));

        _dispPart.IsIn2DSpace = true;
        _dispPart.ToScreenSpace = true;

        _dispPart.Color = Color.White;

        _dispPart.HasAdditiveBlending = false;
        _dispPart.Origin2D = FontGlobals.RebirthFont.MeasureString(_dispPart.Text) / 2;
        _dispPart.Scale = Vector3.One;
        _dispPart.Alpha = 0;

        _dispPart.UniqueDraw = particle => {
            particle.Position = Chest.ChestPosition + new Vector3(0, 150, 0);
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, particle.Text,
                MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(particle.Position),
                    CameraGlobals.GameView, CameraGlobals.GameProjection),
                particle.Color, Color.Black, new(particle.Scale.X, particle.Scale.Y), 0f, Anchor.Center);
        };

        SpawnKeys();
    }

    public static IProp FuckingGamble(LootBox<IProp> lootPool, out float percent) {
        var result = lootPool.Roll(out percent);

        return (IProp)result.Clone();
    }

    static void SpawnKeys() {
        _keys.Clear();
        var numKeys = TankGame.SaveFile.CollectedKeys;

        var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/chest/key");

        // TextureGlobals.Pixels[color]

        float startRads = -MathHelper.PiOver2;   // left of fan
        float endRads = MathHelper.PiOver2;      // right of fan
        var origPos = Chest.ChestPosition + new Vector3(0, 20, 0);

        var diff = (endRads - startRads) / (numKeys - 1);

        for (int i = 0; i < numKeys; i++) {
            Particle keyPart = GameHandler.Particles.MakeParticle(origPos, ModelGlobals.Key.Asset, tex);

            float k = numKeys == 1 ? 0.5f : (float)i / (numKeys - 1);
            // helps calculate the goal position
            float angle = MathHelper.Lerp(startRads, endRads, k);

            var destination = new Vector3(50, 50, 0).Rotate(Vector3.UnitZ, angle + MathHelper.PiOver4);
            destination = destination.RotateXZ(-MathHelper.PiOver4);
            // destination = destination.RotateXZ(radLerp);

            keyPart.Alpha = 1f;
            // maybe this is retarded
            keyPart.Scale = Vector3.One * 40;
            keyPart.HasAdditiveBlending = false;
            keyPart.Pitch = MathHelper.PiOver2;
            keyPart.Color = Color.White;
            keyPart.FaceTowardsMe = true;

            float t = 0;

            float phaseOffset = Client.ClientRandom.NextFloat(-100_000, 100_000);

            float moveSpeed = 0.02f;

            _keys.Add(keyPart);

            float velY = 0.8f;
            float velX = Client.ClientRandom.NextFloat(0.25f, 0.75f);
            float velZ = Client.ClientRandom.NextFloat(0, -0.5f);

            keyPart.UniqueBehavior = (p) => {
                // gives time for the camera to transition
                if (p.LifeTime < 80) return;

                if (_movingKey == keyPart) {
                    keyPart.Position = _keyAnimation.CurrentPosition;
                    keyPart.FaceTowardsMe = false;

                    var deconstructed = _keyAnimation.CurrentFloats.ToVector3();
                    keyPart.Roll = deconstructed.X + MathHelper.Pi;
                    keyPart.Pitch = deconstructed.Y - MathHelper.PiOver2; //+ MathHelper.PiOver2 * 3 + MouseUtils.Test.Y * MathHelper.Tau;
                    keyPart.Yaw = deconstructed.Z; //+ MouseUtils.Test.X * MathHelper.Tau;

                    // Console.WriteLine($"{string.Join(", ", _keyAnimation.CurrentFloats)}");

                    if (_keyAnimation.TotalProgress == 1) {
                        // if tag is false, it's "discarded"
                        keyPart.Tag = false;
                        SoundPlayer.PlaySoundInstance("Assets/sounds/menu/key_toss_away.ogg", SoundContext.Effect);
                        _movingKey = null;
                        _isOpening = false;
                        _keys.Remove(keyPart);
                    }
                    return;
                }
                if (keyPart.Tag is bool) {
                    var vec = new Vector3(velX, velY, velZ);
                    keyPart.Position += vec;

                    velY -= 0.01f;

                    keyPart.Roll += 0.025f;
                    keyPart.Pitch -= 0.025f;

                    // remove props from display tank
                    displayTank?.RemoveCosmetics();

                    if (keyPart.Position.Y < 0) {
                        keyPart.Destroy();
                    }

                    return;
                }

                // Hover Effect Logic
                float baseScale = 40f;
                float hoverScale = 55f;
                float targetScale = p == _hoveredKey ? hoverScale : baseScale;

                // Smoothly interpolate scale
                p.Scale = Vector3.Lerp(p.Scale, new Vector3(targetScale), 0.2f * RuntimeData.DeltaTime);

                if (MainMenuUI.MenuState == MainMenuUI.UIState.Cosmetics) {
                    t += moveSpeed * RuntimeData.DeltaTime;

                    if (t > 1) t = 1;
                }
                else {
                    if (t <= 0) {
                        p.Destroy();
                        _keys.Remove(keyPart);
                    }

                    t -= moveSpeed * 2.5f * RuntimeData.DeltaTime;

                    if (t < 0) t = 0;
                }
                float ease = Easings.ComputeEase(EasingFunction.InOutSine, t);

                p.Roll = ease * MathHelper.TwoPi * 3 + MathHelper.PiOver2;

                float randX = Client.ClientRandom.NextFloat(-10, 20);
                float randY = Client.ClientRandom.NextFloat(-5, 5);

                float rand = Client.ClientRandom.NextFloat(0, 40);

                // framerate independence, baby!
                if (rand < RuntimeData.DeltaTime) {
                    GameHandler.Particles.MakeShineSpot(p.Position + new Vector3(randX, randY, 0),
                        Color.White, Client.ClientRandom.NextFloat(0.3f, 0.5f));
                }

                // if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.X)) p.Destroy();
                float ySin = MathF.Sin(RuntimeData.RunTime / 50 + phaseOffset) * 0.5f * 5;
                p.Position = origPos + destination * ease + new Vector3(0, ySin, 0);
            };
        }
    }

    // non-important shit
    public static void LeaveMenu() {
        _dispPart?.Destroy();
        _movingKey = null;
    }
    public static void DrawMenu() {
        // DebugManager.DrawBoundingSphere(_clickSpot, ColorUtils.DiscoPartyColor, CameraGlobals.GameView, CameraGlobals.GameProjection);
        Chest?.Render();

        displayTank?.Render();
    }
}
