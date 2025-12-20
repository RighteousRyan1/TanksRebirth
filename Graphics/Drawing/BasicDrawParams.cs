using Microsoft.Xna.Framework;

namespace TanksRebirth.Graphics.Drawing; 

/// <summary>Call the parameterless constructor to get the default values appropriately set.</summary>
public struct BasicDrawParams {
    /// <summary>This object in world space. Used to change the actual location of the model relative to the <see cref="View"/> and <see cref="Projection"/>.</summary>
    public Matrix World;
    /// <summary>How the object is viewed through the <see cref="Projection"/>.</summary>
    public Matrix View;
    /// <summary>The projection from the screen onto the object.</summary>
    public Matrix Projection;

    public float LightPower = 1f;
    public float AmbientPower = 1f;
    public bool UsePhong;

    public Vector3 LightDirection;

    /// <summary>The scale at which the object is drawn.</summary>
    public Vector3 Scaling;
    public BasicDrawParams() { }
}
