using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals
{
	public static class GameResources
	{
		private static Dictionary<string, object> ResourceCache { get; set; } = new();

		private static Dictionary<string, object> QueuedResources { get; set; } = new();

		public static T GetResource<T>(this ContentManager manager, string name) where T : class
		{
			if (manager != null) {
				if (ResourceCache.TryGetValue(Path.Combine(manager.RootDirectory, name), out var val) && val is T content)
					return content;
			}
			else if (ResourceCache.TryGetValue(name, out var val) && val is T content)
				return content;

			return LoadResource<T>(manager, name);
		}
		public static T LoadResource<T>(ContentManager manager, string name) where T : class
		{
			if (ResourceCache.ContainsKey(name))
				return (T)ResourceCache[name];
			else if (typeof(T) == typeof(Texture2D))
			{
				// var texture = (Texture2D)Convert.ChangeType(result, typeof(Texture2D));
				object result = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, name);
				ResourceCache[name] = result;

				return (T)result;
			}

			T loaded = manager.Load<T>(name);

			ResourceCache[name] = loaded;
			return loaded;
		}

		public static T GetGameResource<T>(string name, bool addDotPng = true, bool addContentPrefix = true, bool premultiply = false) where T : class
		{
			var realResourceName = name + (addDotPng ? ".png" : string.Empty);
            if (TankGame.Instance is null)
				QueueAsset<T>(realResourceName);

			if (ResourceCache.ContainsKey(realResourceName))
					return (T)ResourceCache[realResourceName];
			else if (typeof(T) == typeof(Texture2D))
			{
				// var texture = (Texture2D)Convert.ChangeType(result, typeof(Texture2D));
				object result = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(addContentPrefix ? TankGame.Instance.Content.RootDirectory : string.Empty, realResourceName));
				ResourceCache[realResourceName] = result;

				if (premultiply) {
					var refUse = (Texture2D)result;
					ColorUtils.FromPremultiplied(ref refUse);
					result = refUse;
				}
				return (T)result;
			}

			return GetResource<T>(TankGame.Instance.Content, name);
        }

		public static void QueueAsset<T>(string name)
        {
			if (!QueuedResources.TryGetValue(name, out var val) || val is not T)
				QueuedResources[name] = typeof(T);
        }

		public static void LoadQueuedAssets()
		{
			Task.Run(() => { }); // rndunfsdauif fd saoidf s
			foreach (var resource in QueuedResources)
            {

			}
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

		public static string ProjectDirectory = Directory.GetCurrentDirectory().Replace("bin", "").Replace("Debug", "").Replace("net6.0", "").Replace("Release", "") + "/";
		public static void CopySrcFolderContents(string path, string extension = null, bool overWrite = true)
        {
			var files = extension != null ? Directory.GetFiles(Path.Combine(ProjectDirectory, path)).Where(file => file.EndsWith(extension)).ToArray() : Directory.GetFiles(Path.Combine(ProjectDirectory, path));
			Directory.CreateDirectory(path);

			foreach (var file in files)
            {
				var fileName = Path.GetFileName(file);
				File.Copy(file, Path.Combine(path, fileName), overWrite);
            }
        }
	}
}