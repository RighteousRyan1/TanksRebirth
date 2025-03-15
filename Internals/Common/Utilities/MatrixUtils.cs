using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MatrixUtils
{
    public static Vector2 ConvertWorldToScreen(Vector3 position, Matrix world, Matrix view, Matrix projection)
    {
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        var proj = viewport.Project(position, projection, view, world);

        // means the projected position is off screen.
        if (proj.Z > 1) {
            proj.Y += 10000;
        }
        // if i replace proj.Y with proj.Z it could be an indicator something is offscreen...?
        return new(proj.X, proj.Y);
    }
    public static Vector3 ConvertScreenToWorld(Vector3 position, Matrix world, Matrix view, Matrix projection)
    {
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        var proj = viewport.Unproject(position, projection, view, world);

        return proj;
    }

    public static Vector3 GetWorldPosition(Vector2 screenCoords, float offset = 0f)
    {
        Plane gamePlane = new(Vector3.UnitY, offset);

        var nearPlane = ConvertScreenToWorld(new Vector3(screenCoords, 0), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);
        var farPlane = ConvertScreenToWorld(new Vector3(screenCoords, 1), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);

        var mouseRay = new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));

        float? distance = mouseRay.Intersects(gamePlane);

        if (!distance.HasValue)
            return new();

        return mouseRay.Position + mouseRay.Direction * distance.Value;
    }

    public static Ray GetMouseToWorldRay()
    {
        var nearPlane = ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 0), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);
        var farPlane = ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 1), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);

        return new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));
    }

    public static bool AreMatricesEqual(Matrix m1, Matrix m2, float epsilon = 0.0001f) {
        return Math.Abs(m1.M11 - m2.M11) < epsilon &&
               Math.Abs(m1.M12 - m2.M12) < epsilon &&
               Math.Abs(m1.M13 - m2.M13) < epsilon &&
               Math.Abs(m1.M14 - m2.M14) < epsilon &&
               Math.Abs(m1.M21 - m2.M21) < epsilon &&
               Math.Abs(m1.M22 - m2.M22) < epsilon &&
               Math.Abs(m1.M23 - m2.M23) < epsilon &&
               Math.Abs(m1.M24 - m2.M24) < epsilon &&
               Math.Abs(m1.M31 - m2.M31) < epsilon &&
               Math.Abs(m1.M32 - m2.M32) < epsilon &&
               Math.Abs(m1.M33 - m2.M33) < epsilon &&
               Math.Abs(m1.M34 - m2.M34) < epsilon &&
               Math.Abs(m1.M41 - m2.M41) < epsilon &&
               Math.Abs(m1.M42 - m2.M42) < epsilon &&
               Math.Abs(m1.M43 - m2.M43) < epsilon &&
               Math.Abs(m1.M44 - m2.M44) < epsilon;
    }
}
