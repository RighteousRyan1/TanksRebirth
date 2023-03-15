using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class MatrixUtils
{
    public static Vector2 ConvertWorldToScreen(Vector3 position, Matrix world, Matrix view, Matrix projection) {
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        var proj = viewport.Project(position, projection, view, world);

        return new(proj.X, proj.Y);
    }
    public static Vector3 ConvertScreenToWorld(Vector3 position, Matrix world, Matrix view, Matrix projection) {
        var viewport = TankGame.Instance.GraphicsDevice.Viewport;

        var proj = viewport.Unproject(position, projection, view, world);

        return proj;
    }

    public static Vector3 GetWorldPosition(Vector2 screenCoords, float offset = 0f) {
        Plane gamePlane = new(Vector3.UnitY, offset);

        var nearPlane = ConvertScreenToWorld(new Vector3(screenCoords, 0), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);
        var farPlane = ConvertScreenToWorld(new Vector3(screenCoords, 1), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);

        var mouseRay = new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));

        var distance = mouseRay.Intersects(gamePlane);

        if (!distance.HasValue)
            return new();

        return mouseRay.Position + mouseRay.Direction * distance.Value;
    }

    public static Ray GetMouseToWorldRay() {
        var nearPlane = ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 0), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);
        var farPlane = ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 1), Matrix.Identity, TankGame.GameView, TankGame.GameProjection);

        return new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));
    }
}
