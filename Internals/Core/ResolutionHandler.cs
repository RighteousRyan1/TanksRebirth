using Microsoft.Xna.Framework;

namespace WiiPlayTanksRemake.Internals.Core
{
	public static class ResolutionHandler
	{
		public static int DesignedX = 1920;

		public static int DesignedY = 1080;

		internal static void Initialize(GraphicsDeviceManager graphicsDeviceManager) {
			GraphicsDeviceManager = graphicsDeviceManager;
			UpdateResolution(false, DesignedX, DesignedY);
		}

		public static GraphicsDeviceManager GraphicsDeviceManager{ get; private set; }

		public static bool FullScreened;

		public static int ScreenWidth;

		public static int ScreenHeight;

		public static Vector2 ResolutionRatio => new((float)ScreenWidth / DesignedX, (float)ScreenHeight / DesignedY);

		public static void UpdateResolution(bool fullScreened, int screenWidth, int screenHeight) {
			FullScreened = fullScreened;
			ScreenWidth = screenWidth;
			ScreenHeight = screenHeight;
			UpdateGraphicsDeviceManager(FullScreened, ScreenWidth, ScreenHeight);
		}

		private static void UpdateGraphicsDeviceManager(bool fullScreened, int screenWidth, int screenHeight) {
			GraphicsDeviceManager.IsFullScreen = fullScreened;
			GraphicsDeviceManager.PreferredBackBufferWidth = screenWidth;
			GraphicsDeviceManager.PreferredBackBufferHeight = screenHeight;
			GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
		}
	}
}