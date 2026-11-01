using Microsoft.Xna.Framework;

namespace TanksRebirth.Graphics.Cameras;

// WIP
public interface IPerspectiveCamera : ICamera {
    Vector3 Position { get; set; }
    Vector3 Rotation { get; set; }
    Vector3 Focus { get; set; }

    bool UseFocus { get; set; }

    float MinPitch { get; set; }
    float MaxPitch { get; set; }
    float FieldOfView { get; set; }
}
public interface ICamera : IRenderMatrices {
    float Near { get; set; }
    float Far { get; set; }
}

public interface IRenderMatrices {
    Matrix World { get; set; }
    Matrix View { get; set; }
    Matrix Projection { get; set; }
}