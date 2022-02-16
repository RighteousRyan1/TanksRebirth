using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.Internals.Common.Framework
{
	/*public class XnbReader : IAssetReader
	{
		private class StreamContentManager : ContentManager
		{
			private Stream stream;

			public StreamContentManager(IServiceProvider gameServices) : base(gameServices, "Content") { }

			protected override Stream OpenStream(string assetName) => stream;

			public void SetStream(Stream stream)
			{
				this.stream = stream;
			}

			public void ClearLoadedAssets()
			{
				LoadedAssets.Clear();
			}
		}

		private readonly StreamContentManager manager;

		public XnbReader(IServiceProvider services)
		{
			manager = new StreamContentManager(services);
		}

		public T? FromStream<T>(Stream stream, string name) where T : class
		{
			var extension = Path.GetExtension(name);

			if (!string.IsNullOrEmpty(extension) && extension != FileExtensionRegistry.MonoGameCompiledContent)
				throw AssetLoadException.FromInvalidExtension(name, extension, new[] { FileExtensionRegistry.MonoGameCompiledContent });

			try
			{
				//Remove the extension since ContentManager.Load<T> expects it to be not there
				string path = Path.Combine(manager.RootDirectory, !string.IsNullOrEmpty(extension) ? name[..^extension.Length] : name);
				path = AssetPathHelper.CleanPath(path);

				manager.ClearLoadedAssets();
				manager.SetStream(stream);

				var asset = manager.Load<T>(path);
				if (asset is Texture2D texture)
					texture.Name = name;

				return asset;
			}
			catch (ObjectDisposedException ex)
			{
				throw new MissingResourceException("XnbReader.manager", ex);
			}
		}
	}*/
}
