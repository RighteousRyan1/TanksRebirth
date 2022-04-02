using TanksRebirth.GameContent;

namespace TanksRebirth.Graphics
{
    public struct ThirdPerson
    {
        public Tank Host { get; set; }

        public float watchDistance;

        public Attaching CameraAttach { get; set; }

        public float FOV { get; set; }

        public enum Attaching
        {
            Soft,
            Hard
        }
    }
}