using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

#pragma warning disable
public static class AnimDebug {
    static BasicEffect _animEff;
    public static Color LineColor => ColorUtils.DiscoPartyColor;
    public static Color CrossColor => ColorUtils.Invert(LineColor);

    public static void DrawPath(GraphicsDevice device, List<KeyFrame> keyFrames, Matrix view, Matrix proj, Matrix? world = null) {
        if (keyFrames.Count == 0)
            return;

        // ensures it exists
        if (_animEff == null) {
            _animEff = new BasicEffect(device) {
                VertexColorEnabled = true
            };
        }

        _animEff.World = world ?? Matrix.Identity;
        _animEff.View = view;
        _animEff.Projection = proj;

        // is this GC hell?
        var keyFrameVertices = new List<VertexPositionColor>();
        var pathVertices = new List<VertexPositionColor>();
        var controlPointVertices = new List<VertexPositionColor>();

        // solely draw the first cross to indicate placement
        if (keyFrames.Count == 1) {
            float dist = Vector3.Distance(Matrix.Invert(view).Translation, keyFrames[0].Position);
            float scale = dist / 50;
            AddCross(keyFrameVertices, keyFrames[0].Position, scale, CrossColor);
            goto skipToDraw;
        }

        for (int i = 0; i < keyFrames.Count - 1; i++) {
            var current = keyFrames[i];
            var next = keyFrames[i + 1];

            // draw markers
            float dist = Vector3.Distance(Matrix.Invert(view).Translation, current.Position);
            float scale = dist / 50;
            AddCross(keyFrameVertices, current.Position, scale, CrossColor);

            // draws path (linear/bezier)
            if (next.BezierPoints != null && next.BezierPoints.Count > 0) {
                var safeCurve = new List<Vector3>();

                // last keyframe pos = start point
                safeCurve.Add(current.Position);

                safeCurve.AddRange(next.BezierPoints);

                // next keyframe = end point
                safeCurve.Add(next.Position);

                int steps = 60;
                var prevPos = safeCurve[0];

                for (int s = 1; s <= steps; s++) {
                    float t = s / (float)steps;

                    // Calculate position using the FULL safe list
                    var currentPos = MathUtils.Bezier3D(t, safeCurve.ToArray());

                    pathVertices.Add(new VertexPositionColor(prevPos, LineColor));
                    pathVertices.Add(new VertexPositionColor(currentPos, LineColor));

                    prevPos = currentPos;
                }

                // control points (if there are any)
                for (int b = 0; b < safeCurve.Count - 1; b++) {
                    controlPointVertices.Add(new VertexPositionColor(safeCurve[b], Color.Yellow));
                    controlPointVertices.Add(new VertexPositionColor(safeCurve[b + 1], Color.Yellow));
                }
            }
            else {
                // linear
                pathVertices.Add(new VertexPositionColor(current.Position, LineColor));
                pathVertices.Add(new VertexPositionColor(next.Position, LineColor));
            }
        }

        if (keyFrames.Count > 0) {
            float dist = Vector3.Distance(Matrix.Invert(view).Translation, keyFrames[^1].Position);
            AddCross(keyFrameVertices, keyFrames[^1].Position, dist / 50, CrossColor);
        }

    skipToDraw:

        foreach (var pass in _animEff.CurrentTechnique.Passes) {
            pass.Apply();

            if (pathVertices.Count > 0)
                device.DrawUserPrimitives(PrimitiveType.LineList, pathVertices.ToArray(), 0, pathVertices.Count / 2);

            if (controlPointVertices.Count > 0)
                device.DrawUserPrimitives(PrimitiveType.LineList, controlPointVertices.ToArray(), 0, controlPointVertices.Count / 2);

            if (keyFrameVertices.Count > 0)
                device.DrawUserPrimitives(PrimitiveType.LineList, keyFrameVertices.ToArray(), 0, keyFrameVertices.Count / 2);
        }
    }

    static void AddCross(List<VertexPositionColor> verts, Vector3 pos, float size, Color color) {
        verts.Add(new VertexPositionColor(pos - Vector3.UnitX * size, color));
        verts.Add(new VertexPositionColor(pos + Vector3.UnitX * size, color));
        verts.Add(new VertexPositionColor(pos - Vector3.UnitY * size, color));
        verts.Add(new VertexPositionColor(pos + Vector3.UnitY * size, color));
        verts.Add(new VertexPositionColor(pos - Vector3.UnitZ * size, color));
        verts.Add(new VertexPositionColor(pos + Vector3.UnitZ * size, color));
    }
}