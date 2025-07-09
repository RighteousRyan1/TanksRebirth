using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.GameContent;

public enum ParticleIntensity
{
    None, // no particles at all
    Low, // Particles emit at 25% the frequency
    Medium, // ... 50%
    High, // ... 100%
}
public class Particle
{
    /// <summary>Data you may want to assign to this particle, such as assigning an owner to a particle, etc.</summary>
    public object Tag;

    /// <summary>The texture this <see cref="Particle"/> will use.</summary>
    public Texture2D Texture;

    /// <summary>The 3D model of this particle, if applicable.</summary>
    public Model Model;

    public Vector3 Position;
    /// <summary>The R, G, and B channels to use as a color for this <see cref="Particle"/>.</summary>
    public Color Color = Color.White;

    /// <summary>The X rotation of this <see cref="Particle"/>, if it's in 3D space.</summary>
    public float Roll;
    /// <summary>The Y rotation of this <see cref="Particle"/>, if it's in 3D space.</summary>
    public float Pitch;
    /// <summary>The Z rotation of this <see cref="Particle"/>, if it's in 3D space.</summary>
    public float Yaw;

    /// <summary>The scale of this <see cref="Particle"/>'s texture.</summary>
    public Vector2 TextureScale = Vector2.One;

    /// <summary>The rotation of the texture before and if this <see cref="Particle"/> exists in a 3D space.</summary>
    public float Rotation2D;

    public Vector2 Origin2D;
    /// <summary>The value from 0 (transparent) to 1 (opaque) of the alpha channel.</summary>
    public float Alpha = 1f;

    /// <summary>The unique identifier of this <see cref="Particle"/>.</summary>
    public readonly int Id;

    /// <summary>Whether to billboard this <see cref="Particle"/> towards the camera, if in a 3D space.</summary>
    public bool FaceTowardsMe;

    /// <summary>Whether or not this <see cref="Particle"/> should exist in a 2D space.</summary>
    public bool IsIn2DSpace;
    /// <summary>Whether or not this 2D particle should be translated from a 3D vector to screen space, if it exists in a 2D space.</summary>
    public bool ToScreenSpace = true;

    /// <summary>What this <see cref="Particle"/> should do every given update.</summary>
    public Action<Particle> UniqueBehavior;

    /// <summary>What this <see cref="Particle"/> should do every given draw call.</summary>
    public Action<Particle> UniqueDraw;

    /// <summary>Whether or not the color of this <see cref="Particle"/> has addative blending applied.</summary>
    public bool HasAddativeBlending = true;

    /// <summary>How to crop the texture of this particle.</summary>
    public Rectangle? TextureCrop = null; // for framing a particle's texture

    /// <summary>How long (in ticks) this <see cref="Particle"/> particle has existed.</summary>
    public float LifeTime;

    /// <summary>Whether or not this <see cref="Particle"/> uses text instead of a texture.</summary>
    public bool IsText;
    /// <summary>The text that should show, if <see cref="IsText"/> is true.</summary>
    public string Text;

    /// <summary>The 3D scaling of this <see cref="Particle"/>.</summary>
    public Vector3 Scale;
    /// <summary>Light power multiplier for this <see cref="Particle"/>.</summary>
    public float LightPower;
    /// <summary>The <see cref="ParticleSystem"/> this <see cref="Particle"/> exists in.</summary>
    public ParticleSystem System { get; }

    public float Layer;

    /* TODO:
     * Model alpha must be set!
     * 
     * billboard from 'position' to the camera.
     */

    internal Particle(Vector3 position, ParticleSystem system)
    {
        Position = position;
        System = system;
        int index = Array.IndexOf(System.CurrentParticles, null);

        Id = index;

        System.CurrentParticles[index] = this;
    }

    public void Update()
    {
        UniqueBehavior?.Invoke(this);
        LifeTime += RuntimeData.DeltaTime;
    }

    public static BasicEffect EffectHandle = new(TankGame.Instance.GraphicsDevice);

    internal void RenderModels() {

        // this code causes draw order malfunction. ts pmo

        //TankGame.SpriteRenderer.End();

        //if (System.Scissor.HasValue) {
        //    TankGame.Instance.GraphicsDevice.ScissorRectangle = System.Scissor.Value;
        //}

        //TankGame.SpriteRenderer.Begin(blendState: HasAddativeBlending ? BlendState.Additive : BlendState.NonPremultiplied, rasterizerState: System.Rasterizer);
        var world =
                Matrix.CreateScale(Scale) *
                Matrix.CreateFromYawPitchRoll(Yaw, Pitch, Roll) * 
                Matrix.CreateTranslation(Position);
        for (int i = 0; i < (Lighting.AccurateShadows ? 2 : 1); i++) {
            foreach (ModelMesh mesh in Model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = i == 0 ? world : world * Matrix.CreateShadow(Lighting.AccurateLightingDirection, new(Vector3.UnitY, 0)) * Matrix.CreateTranslation(0, 0.2f, 0);
                    effect.View = System.SystemView;
                    effect.Projection = System.SystemProjection;

                    effect.TextureEnabled = true;
                    effect.Texture = Texture;

                    effect.Alpha = Alpha;

                    effect.FogEnabled = false;
                    effect.EmissiveColor = Color.ToVector3() * SceneManager.GameLight.Brightness;

                    effect.SetDefaultGameLighting_IngameEntities(LightPower);
                }
                mesh.Draw();
            }
        }
        // same here
        //TankGame.SpriteRenderer.End();
        //TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
    }
    internal void Render()
    {
        if (!GameScene.ShouldRenderAll || Model is not null)
            return;

        var world = Matrix.CreateScale(Scale) *
            Matrix.CreateFromYawPitchRoll(Yaw, Pitch, Roll) * 
            Matrix.CreateTranslation(Position);
        if (!IsIn2DSpace)
        {
            if (Model is null) {
                EffectHandle.World = world;
                EffectHandle.View = System.SystemView;
                EffectHandle.Projection = System.SystemProjection;
                EffectHandle.TextureEnabled = true;
                EffectHandle.Texture = Texture;
                EffectHandle.EmissiveColor = Color.ToVector3() * SceneManager.GameLight.Brightness;

                EffectHandle.Alpha = Alpha;

                EffectHandle.SetDefaultGameLighting_IngameEntities(LightPower);

                EffectHandle.FogEnabled = false;

                TankGame.SpriteRenderer.End();
                TankGame.SpriteRenderer.Begin(SpriteSortMode.FrontToBack, HasAddativeBlending ? BlendState.Additive : BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.DepthRead, RenderGlobals.DefaultRasterizer, EffectHandle); if (!IsText)
                    TankGame.SpriteRenderer.Draw(Texture, Vector2.Zero, TextureCrop, Color * Alpha, Rotation2D, Origin2D != default ? Origin2D : Texture.Size() / 2, /*TextureScale*/ Scale.X, default, Layer);
                else
                    TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, Text, Vector2.Zero, Color * Alpha, new Vector2(Scale.X, Scale.Y), Rotation2D, Origin2D, Layer);

                TankGame.SpriteRenderer.End();
                TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
        }
        else
        {
            TankGame.SpriteRenderer.End();
            TankGame.SpriteRenderer.Begin(SpriteSortMode.FrontToBack, HasAddativeBlending ? BlendState.Additive : BlendState.NonPremultiplied, rasterizerState: RenderGlobals.DefaultRasterizer); if (!IsText)
                TankGame.SpriteRenderer.Draw(Texture, ToScreenSpace ? 
                    MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(Position), System.SystemView, System.SystemProjection) : 
                    new Vector2(Position.X, Position.Y), TextureCrop, Color * Alpha, Rotation2D, Origin2D != default ? Origin2D : Texture.Size() / 2, TextureScale, default, Layer);
            else
                TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, Text, ToScreenSpace ? 
                    MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(Position), System.SystemView, System.SystemProjection) : 
                    new Vector2(Position.X, Position.Y), Color * Alpha, TextureScale, Rotation2D, Origin2D, Layer);
            TankGame.SpriteRenderer.End();
            TankGame.SpriteRenderer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }
        UniqueDraw?.Invoke(this);
    }

    public void Destroy() {
        UniqueBehavior = null;
        System.CurrentParticles[Id] = null;
    }
}