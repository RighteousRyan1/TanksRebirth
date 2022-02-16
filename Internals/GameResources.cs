using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace WiiPlayTanksRemake.Internals
{
	public static class GameResources
	{
		private static Dictionary<string, object> ResourceCache { get; set; } = new();

		public static T GetResource<T>(this ContentManager manager, string name) where T : class
		{
			if (ResourceCache.TryGetValue(Path.Combine(manager.RootDirectory, name), out var val) && val is T content)
			{
				return content;
			}
			return LoadResource<T>(manager, name);
		}
		public static T LoadResource<T>(ContentManager manager, string name) where T : class
		{
			T loaded = manager.Load<T>(name);

			ResourceCache[name] = loaded;
			return loaded;
		}

		public static T GetGameResource<T>(string name) where T : class
        {
			return GetResource<T>(TankGame.Instance.Content, name);
        }

		public static T GetRawAsset<T>(this ContentManager manager, string assetName) where T : class
        {
			var t = typeof(ContentManager).GetMethod("ReadAsset", BindingFlags.Instance | BindingFlags.NonPublic);

			var generic = t.MakeGenericMethod(typeof(T)).Invoke(manager, new object[] { assetName, null} ) as T;

			return generic;
        }

		public static T GetRawGameAsset<T>(string assetName) where T : class
		{
			var t = typeof(ContentManager).GetMethod("ReadAsset", BindingFlags.Instance | BindingFlags.NonPublic);

			var generic = t.MakeGenericMethod(typeof(T)).Invoke(TankGame.Instance.Content, new object[] { assetName, null }) as T;

			return generic;
		}
	}
}