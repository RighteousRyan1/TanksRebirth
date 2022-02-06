using WiiPlayTanksRemake.GameContent;

namespace WiiPlayTanksRemake.Graphics
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