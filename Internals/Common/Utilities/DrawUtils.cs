using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.Internals.Common.Utilities;

public static class DrawUtils {
    public static float StringSizeProportional(int length,
        int baseLength,
        float baseScale,
        float minScale = 0.25f) {
        if (length <= 0)
            return baseScale;

        if (length <= baseLength)
            return baseScale;

        float scale = baseScale * ((float)baseLength / length);

        return Math.Max(scale, minScale);
    }
    /// <summary>Converts a centered-orthographic coordinate to screen coordinates.</summary>
    public static Vector2 CenteredOrthoToScreen(Vector2 orthoPos) {
        // i think this works. probably not lol.
        var vector = orthoPos - WindowUtils.WindowBounds / 2;
        var negativeVector = new Vector2(vector.X, -vector.Y);
        return negativeVector;
    }

    public static void DrawBox(Vector2 start, Vector2 end, Color boxColor, Vector2 origin = default) {
        var tex = TextureGlobals.Pixels[Color.White];

        start -= origin;
        end -= origin;
        // top line
        TankGame.SpriteRenderer.Draw(tex, start, null, boxColor, 0f, Vector2.Zero, new Vector2(end.X - start.X, 1), default, 0);
        // bottom line
        TankGame.SpriteRenderer.Draw(tex, start + Vector2.UnitY * (end.Y - start.Y), null, boxColor, 0f, Vector2.Zero, new Vector2(end.X - start.X, 1), default, 0);
        // left line
        TankGame.SpriteRenderer.Draw(tex, start, null, boxColor, 0f, Vector2.Zero, new Vector2(1, end.Y - start.Y), default, 0);
        // right line
        TankGame.SpriteRenderer.Draw(tex, start + Vector2.UnitX * (end.X - start.X), null, boxColor, 0f, Vector2.Zero, new Vector2(1, end.Y - start.Y), default, 0);
    }
    public static void DrawBox(Rectangle rect, Color boxColor, float width = 1, Point origin = default) {
        var tex = TextureGlobals.Pixels[Color.White];

        rect.X -= origin.X;
        rect.Y -= origin.Y;

        var start = new Vector2(rect.X, rect.Y);
        var end = new Vector2(rect.X + rect.Width, rect.Y + rect.Height);

        // top line
        TankGame.SpriteRenderer.Draw(tex, start, null, boxColor, 0f, Vector2.Zero, new Vector2(end.X - start.X, width), default, 0);
        // bottom line
        TankGame.SpriteRenderer.Draw(tex, start + Vector2.UnitY * (end.Y - start.Y), null, boxColor, 0f, Vector2.Zero, new Vector2(end.X - start.X + width, width), default, 0);
        // left line
        TankGame.SpriteRenderer.Draw(tex, start, null, boxColor, 0f, Vector2.Zero, new Vector2(width, end.Y - start.Y), default, 0);
        // right line
        TankGame.SpriteRenderer.Draw(tex, start + Vector2.UnitX * (end.X - start.X), null, boxColor, 0f, Vector2.Zero, new Vector2(width, end.Y - start.Y + width), default, 0);
    }
    /// <summary>
    /// Draws a filled rectangle with a border.
    /// </summary>
    /// <param name="sb">The SpriteBatch to draw with.</param>
    /// <param name="rectangle">The destination rectangle.</param>
    /// <param name="color">The fill color of the box.</param>
    /// <param name="borderCol">The color of the border.</param>
    /// <param name="borderSize">The thickness of the border in pixels.</param>
    public static void DrawBoxWithOutline(SpriteBatch sb, Rectangle rectangle, Color color, Color borderCol, int borderSize) {
        // Draw the inner filled background
        sb.Draw(TextureGlobals.Pixels[Color.White], rectangle, color);

        // Draw the 4 sides of the border
        // Top
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, borderSize), borderCol);
        // Bottom
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(rectangle.X, rectangle.Bottom - borderSize, rectangle.Width, borderSize), borderCol);
        // Left
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(rectangle.X, rectangle.Y, borderSize, rectangle.Height), borderCol);
        // Right
        sb.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(rectangle.Right - borderSize, rectangle.Y, borderSize, rectangle.Height), borderCol);
    }
    public static void DrawStringWithBorder(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position, 
        Color textColor, Color borderColor, Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f, float charSpacing = 0,
        float origMeasureScale = 1f) {

        DrawStringBorderOnly(spriteBatch, font, text, position, borderColor, scale, rotation, anchor, borderThickness, charSpacing, origMeasureScale);

        spriteBatch.DrawString(font, text, position, textColor, scale, rotation, GameUtils.GetAnchor(anchor, font.MeasureString(text) * origMeasureScale), 1f, characterSpacing: charSpacing);
    }
    public static void DrawTextureWithBorder(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color textureColor, Color borderColor, 
        Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.Draw(texture, position + new Vector2(0, 2f * borderThickness).RotatedBy(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                null, borderColor, rotation, GameUtils.GetAnchor(anchor, texture.Size()), scale, default, 0f);
        spriteBatch.Draw(texture, position, null, textureColor, rotation, GameUtils.GetAnchor(anchor, texture.Size()), scale, default, 1f);
    }
    public static void DrawStringWithShadow(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir, 
        string text, Color color, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        float shadowDistScale = 1f, float shadowAlpha = 1f, float origMeasureScale = 1f, float charSpacing = 0) {

        DrawStringShadowOnly(spriteBatch, font, position, shadowDir, text, scale, alpha, anchor, shadowDistScale, shadowAlpha, origMeasureScale, charSpacing);

        spriteBatch.DrawString(font, text, position, color * alpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }
    public static void DrawTextureWithShadow(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Vector2 shadowDir, 
        Color color, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        SpriteEffects flip = SpriteEffects.None, float shadowDistScale = 1f, float shadowAlpha = 1f, float rotation = 0f, Rectangle? srcRect = null) {

        if (shadowAlpha > 0) {
            spriteBatch.Draw(texture,
                position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale),
                srcRect,
                Color.Black * alpha * shadowAlpha,
                rotation,
                anchor.GetAnchor(texture.Size()),
                scale,
                flip,
                default);
        }
        spriteBatch.Draw(texture, position, srcRect, color * alpha, rotation, anchor.GetAnchor(texture.Size()), scale, flip, default);
    }
    public static void DrawStringWithBorderAndShadow(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir, 
        string text, Color color, Color borderColor, Vector2 scale, float alpha, Anchor anchor = Anchor.Center, 
        float shadowDistScale = 1f, float shadowAlpha = 1f, float borderThickness = 1f, float charSpacing = 0, float origMeasureScale = 1f) {

        spriteBatch.DrawString(font, text, position + 
            Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale), 
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);

        DrawStringWithBorder(spriteBatch, font, text, position, color * alpha, borderColor * alpha, scale, 0f, anchor, borderThickness, charSpacing: charSpacing, origMeasureScale: origMeasureScale); 
    }

    public static void DrawStringBorderOnly(SpriteBatch spriteBatch, SpriteFontBase font, string text, Vector2 position,
        Color borderColor, Vector2 scale, float rotation, Anchor anchor = Anchor.Center, float borderThickness = 1f, float charSpacing = 0,
        float origMeasureScale = 1f) {
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, text, position + new Vector2(0, 2f * borderThickness).RotatedBy(MathHelper.PiOver2 * i + MathHelper.PiOver4).ToResolution(),
                borderColor, scale, rotation, GameUtils.GetAnchor(anchor, font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }
    public static void DrawStringShadowOnly(SpriteBatch spriteBatch, SpriteFontBase font, Vector2 position, Vector2 shadowDir,
    string text, Vector2 scale, float alpha, Anchor anchor = Anchor.Center,
    float shadowDistScale = 1f, float shadowAlpha = 1f, float origMeasureScale = 1f, float charSpacing = 0) {

        spriteBatch.DrawString(font, text, position + Vector2.Normalize(shadowDir) * (10f * shadowDistScale * scale),
            Color.Black * alpha * shadowAlpha, scale, 0f, anchor.GetAnchor(font.MeasureString(text) * origMeasureScale), 0f, characterSpacing: charSpacing);
    }

    public static float GetTextXOffsetForSpacing(string text, float spacing) {
        var multiplicand = text.Length - 2;

        // nothing to do lol
        if (multiplicand < 0)
            multiplicand = 0;

        return spacing * multiplicand;
    }
    
    public static void DrawNineSliced(SpriteBatch spriteBatch, Texture2D texture, int border, Rectangle area, Color color, Vector2 origin) {
        Point useBorder = new Vector2(border, border).ToResolution().ToPoint();

        int middleX = area.X + useBorder.X;
        int rightX = area.Right - useBorder.X;

        int middleY = area.Y + useBorder.Y;
        int bottomY = area.Bottom - useBorder.Y;

        spriteBatch.Draw(texture, new Rectangle(area.X, area.Y, useBorder.X, useBorder.Y), new Rectangle(0, 0, border, border), color);
        spriteBatch.Draw(texture, new Rectangle(middleX, area.Y, area.Width - useBorder.X * 2, useBorder.Y), new Rectangle(border, 0, texture.Width - border * 2, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, area.Y, useBorder.X, useBorder.Y), new Rectangle(texture.Width - border, 0, border, border), color, 0f, origin, default, 0f);

        spriteBatch.Draw(texture, new Rectangle(area.X, middleY, useBorder.X, area.Height - useBorder.Y * 2), new Rectangle(0, border, border, texture.Height - border * 2), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(middleX, middleY, area.Width - useBorder.X * 2, area.Height - useBorder.Y * 2), new Rectangle(border, border, texture.Width - border * 2, texture.Height - border * 2), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, middleY, useBorder.X, area.Height - useBorder.Y * 2), new Rectangle(texture.Width - border, border, border, texture.Height - border * 2), color, 0f, origin, default, 0f);

        spriteBatch.Draw(texture, new Rectangle(area.X, bottomY, useBorder.X, useBorder.Y), new Rectangle(0, texture.Height - border, border, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(middleX, bottomY, area.Width - useBorder.X * 2, useBorder.Y), new Rectangle(border, texture.Height - border, texture.Width - border * 2, border), color, 0f, origin, default, 0f);
        spriteBatch.Draw(texture, new Rectangle(rightX, bottomY, useBorder.X, useBorder.Y), new Rectangle(texture.Width - border, texture.Height - border, border, border), color, 0f, origin, default, 0f);
    }

    public static Rectangle GetOffset(this Rectangle rect, int x, int y) => new Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height);

    public static void DrawBoundingBox(
         BoundingBox box,
         Color color,
         Matrix view,
         Matrix projection,
         Matrix? world = null) {

        EnsureDebugEffect();

        var gd = TankGame.Instance.GraphicsDevice;

        _debugEff.World = world ?? Matrix.Identity;
        _debugEff.View = view;
        _debugEff.Projection = projection;

        Vector3[] corners = box.GetCorners();

        int v = 0;
        void AddEdge(int i0, int i1) {
            _bboxVertices[v++] = new VertexPositionColor(corners[i0], color);
            _bboxVertices[v++] = new VertexPositionColor(corners[i1], color);
        }

        // corner order:
        // 0: near Bottom Left
        // 1: near Top Left
        // 2: near Top Right
        // 3: near Bottom Right
        // 4: nar Bottom Left
        // 5: nar Top Left
        // 6: nar Top Right
        // 7: nar Bottom Right

        // near face
        AddEdge(0, 1);
        AddEdge(1, 2);
        AddEdge(2, 3);
        AddEdge(3, 0);

        // far face
        AddEdge(4, 5);
        AddEdge(5, 6);
        AddEdge(6, 7);
        AddEdge(7, 4);

        // connect near & far
        AddEdge(0, 4);
        AddEdge(1, 5);
        AddEdge(2, 6);
        AddEdge(3, 7);

        foreach (var pass in _debugEff.CurrentTechnique.Passes) {
            pass.Apply();
            gd.DrawUserPrimitives(PrimitiveType.LineList, _bboxVertices, 0, 12);
        }
    }
    static BasicEffect _debugEff;
    static readonly VertexPositionColor[] _bboxVertices = new VertexPositionColor[24];
    static void EnsureDebugEffect() {
        _debugEff ??= new BasicEffect(TankGame.Instance.GraphicsDevice) {
            VertexColorEnabled = true
        };
        _debugEff.World = Matrix.Identity;
        _debugEff.View = CameraGlobals.GameView;
        _debugEff.Projection = CameraGlobals.GameProjection;
    }
    const float AXIS_DRAW_LEN = 5f;
    public static void DrawAxes() {
        var gd = TankGame.Instance.GraphicsDevice;

        var cam = CameraGlobals.RebirthFreecam;

        Vector3 origin = Vector3.Zero;
        if (CameraGlobals.IsUsingFirstPersonCamera)
            origin = cam.Position + cam.World.Forward * 100;

        var vertices = new VertexPositionColor[6];

        // X+ = red
        vertices[0] = new VertexPositionColor(origin, Color.Red);
        vertices[1] = new VertexPositionColor(origin + Vector3.UnitX * AXIS_DRAW_LEN, Color.Red);

        // Y+ green
        vertices[2] = new VertexPositionColor(origin, Color.Green);
        vertices[3] = new VertexPositionColor(origin + Vector3.UnitY * AXIS_DRAW_LEN, Color.Green);

        // Z+ blue
        vertices[4] = new VertexPositionColor(origin, Color.Blue);
        vertices[5] = new VertexPositionColor(origin + Vector3.UnitZ * AXIS_DRAW_LEN, Color.Blue);

        EnsureDebugEffect();

        foreach (var pass in _debugEff.CurrentTechnique.Passes) {
            pass.Apply();
            gd.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 3);
        }
    }

    // eensy weensy bit of Tank help cuz i was programming this at 3am
    /// <summary>Draws a wireframe 3D bounding sphere.</summary>
    public static void DrawBoundingSphere(BoundingSphere sphere, Color color, Matrix view,
        Matrix projection, Matrix? world = null, int segments = 32) {

        if (segments < 4) segments = 4;

        EnsureDebugEffect();

        var gd = TankGame.Instance.GraphicsDevice;

        _debugEff.World = world ?? Matrix.Identity;
        _debugEff.View = view;
        _debugEff.Projection = projection;

        float radius = sphere.Radius;
        Vector3 center = sphere.Center;

        // latitudes and longitudes to draw
        int latitudeCount = segments / 2; // around y
        int meridianCount = segments / 2; // around x
        int circleSegments = segments;

        // should i make this publicly accessible?
        void CircleDraw(Func<float, Vector3> pointOnCircle) {
            var verts = new VertexPositionColor[circleSegments + 1];

            for (int i = 0; i <= circleSegments; i++) {
                float t = (float)i / circleSegments; // 0 -> 1
                float angle = t * MathHelper.TwoPi; // 0 -> 2pi
                verts[i] = new VertexPositionColor(
                    center + pointOnCircle(angle),
                    color);
            }

            gd.DrawUserPrimitives(
                PrimitiveType.LineStrip,
                verts,
                0,
                circleSegments);
        }

        foreach (var pass in _debugEff.CurrentTechnique.Passes) {
            pass.Apply();

            // latitudes w/o poles
            for (int lat = 1; lat < latitudeCount; lat++) {
                float v = (float)lat / latitudeCount;
                float elev = (v - 0.5f) * MathHelper.Pi;

                float y = radius * MathF.Sin(elev);
                float r = radius * MathF.Cos(elev); // radius at given y

                CircleDraw(angle => new Vector3(
                    MathF.Cos(angle) * r,
                    y,
                    MathF.Sin(angle) * r));
            }

            // meridians
            for (int m = 0; m < meridianCount; m++) {
                float phi = (float)m / meridianCount * MathHelper.TwoPi;

                CircleDraw(theta => {
                    float cosT = MathF.Cos(theta);
                    float sinT = MathF.Sin(theta);
                    float y = radius * cosT;
                    float z = radius * sinT;

                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    float x = z * sinPhi;
                    float zRot = z * cosPhi;

                    return new Vector3(x, y, zRot);
                });
            }
        }
    }
    public static void DrawFrustum(GraphicsDevice device, BoundingFrustum frustum, Color color, Matrix view, Matrix proj) {
        _debugEff ??= new BasicEffect(device) {
            VertexColorEnabled = true
        };

        _debugEff.World = Matrix.Identity;
        _debugEff.View = view;
        _debugEff.Projection = proj;

        // Get the 8 corners of the frustum
        // Indices:
        // 0-3: Near Plane (TopLeft, TopRight, BottomRight, BottomLeft)
        // 4-7: Far Plane (TopLeft, TopRight, BottomRight, BottomLeft)
        var corners = frustum.GetCorners();

        // We need 12 lines x 2 vertices per line = 24 vertices
        var verts = new VertexPositionColor[24];

        // Helper to fill the array
        int i = 0;
        void AddLine(int indexA, int indexB) {
            verts[i++] = new VertexPositionColor(corners[indexA], color);
            verts[i++] = new VertexPositionColor(corners[indexB], color);
        }

        // --- Near Plane ---
        AddLine(0, 1);
        AddLine(1, 2);
        AddLine(2, 3);
        AddLine(3, 0);

        // --- Far Plane ---
        AddLine(4, 5);
        AddLine(5, 6);
        AddLine(6, 7);
        AddLine(7, 4);

        // --- Connections (Near to Far) ---
        AddLine(0, 4);
        AddLine(1, 5);
        AddLine(2, 6);
        AddLine(3, 7);

        foreach (var pass in _debugEff.CurrentTechnique.Passes) {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, 12);
        }
    }
}
