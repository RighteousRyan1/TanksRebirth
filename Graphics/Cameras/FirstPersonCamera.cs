using Microsoft.Xna.Framework;

namespace TanksRebirth.Graphics.Cameras
{
    public class FirstPersonCamera : PerspectiveCamera
    {
        private float _xRotation, _yRotation;

        public float XRotation
        {
            get { return _xRotation; }
            set
            {
                _xRotation = MathHelper.Clamp(value, -MathHelper.PiOver2, MathHelper.PiOver2);
                SetOrientation(Quaternion.CreateFromYawPitchRoll(_yRotation, _xRotation, 0));
            }
        }

        public float YRotation
        {
            get { return _yRotation; }
            set
            {
                _yRotation = value;
                SetOrientation(Quaternion.CreateFromYawPitchRoll(_yRotation, _xRotation, 0));
            }
        }

        public FirstPersonCamera(float fieldOfView, float aspectRatio, float nearZ, float farZ)
            : base(fieldOfView, aspectRatio, nearZ, farZ)
        {
            
        }

        
    }
}