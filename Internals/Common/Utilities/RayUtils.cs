using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;

namespace TanksRebirth.Internals.Common.Utilities;

public static class RayUtils
{
    /// <summary>
    /// Create a ray on a 2D plane either covering the X and Y axes of a plane or the X and Z axes of a plane.
    /// </summary>
    /// <param name="origin">The origin of this <see cref="Ray"/>.</param>
    /// <param name="destination">The place that will be the termination of this <see cref="Ray"/>.</param>
    /// <param name="zAxis">Whether or not this <see cref="Ray"/> will go along the Y or Z axis from the X axis.</param>
    /// <returns>The ray created.</returns>
    public static Ray CreateRayFrom2D(Vector2 origin, Vector2 destination, float excludedAxisOffset = 0f, bool zAxis = true)
    {
        Ray ray;

        if (zAxis)
            ray = new Ray(new Vector3(origin.X, excludedAxisOffset, origin.Y), new Vector3(destination.X, 0, destination.Y));
        else
            ray = new Ray(new Vector3(origin.X, origin.Y, excludedAxisOffset), new Vector3(destination.X, destination.Y, 0));

        return ray;
    }
    /// <summary>
    /// Create a ray on a 2D plane either covering the X and Y axes of a plane or the X and Z axes of a plane. This creates on the 3D plane with a 3D vector and moves along a 2D axis.
    /// </summary>
    /// <param name="origin">The origin of this <see cref="Ray"/>.</param>
    /// <param name="destination">The place that will be the termination of this <see cref="Ray"/>.</param>
    /// <param name="zAxis">Whether or not this <see cref="Ray"/> will go along the Y or Z axis from the X axis.</param>
    /// <returns>The ray created.</returns>
    public static Ray CreateRayFrom2D(Vector3 origin, Vector2 destination, float excludedAxisOffset = 0f, bool zAxis = true)
    {
        Ray ray;

        if (zAxis)
            ray = new Ray(origin + new Vector3(0, excludedAxisOffset, 0), new Vector3(destination.X, 0, destination.Y));
        else
            ray = new Ray(origin + new Vector3(0, 0, excludedAxisOffset), new Vector3(destination.X, destination.Y, 0));

        return ray;
    }

    public static Ray Reflect(Ray ray, float? distanceAlongRay)
    {
        if (!distanceAlongRay.HasValue)
            throw new NullReferenceException("The distance along the ray was null.");

        var distPos = ray.Position * Vector3.Normalize(ray.Direction) * distanceAlongRay.Value;

        var reflected = Vector3.Reflect(ray.Direction, distPos);

        return new(distPos, reflected);
    }

    public static Ray Flatten(this Ray ray, bool zAxis = true)
    {
        Ray usedRay;

        if (zAxis)
            usedRay = new Ray(new Vector3(ray.Position.X, 0, ray.Position.Y), new Vector3(ray.Direction.X, 0, ray.Direction.Y));
        else
            usedRay = new Ray(new Vector3(ray.Position.X, ray.Position.Y, 0), new Vector3(ray.Direction.X, ray.Direction.Y, 0));

        return usedRay;
    }

    public static Ray GetMouseToWorldRay()
    {
        var nearPlane = MatrixUtils.ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 0), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);
        var farPlane = MatrixUtils.ConvertScreenToWorld(new Vector3(MouseUtils.MousePosition, 1), Matrix.Identity, CameraGlobals.GameView, CameraGlobals.GameProjection);

        return new Ray(nearPlane, Vector3.Normalize(farPlane - nearPlane));
    }
}
