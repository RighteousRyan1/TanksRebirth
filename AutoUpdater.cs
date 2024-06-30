using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aspose.Zip.Rar;
using DiscordRPC;
using Octokit;
using TanksRebirth.GameContent;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth;

public class AutoUpdater {
    private readonly string _ghLink;
    private readonly string _tag;
    private readonly string _name;
    private readonly string[] _assets;
    private Version? _versionToCheckAgainst;
    private Version? _expectedVersion;
    public bool IsOutdated { get; private set; }

    public event OnDownloadCompleteEvent OnDownloadComplete;

    public delegate void OnDownloadCompleteEvent(string name, string url);

#pragma warning disable
    public AutoUpdater(string ghLink, Version? versionToCheckAgainst) {
        try {
            m_getRepo(out var tag, out var name, out var assets);
            if (!Version.TryParse(tag.Replace("-alpha", string.Empty), out _expectedVersion))
                throw new Exception("Failed to grab a recent version from GitHub.");
            _tag = tag;
            _name = name;
            _assets = assets;
            _ghLink = ghLink;
            _versionToCheckAgainst = versionToCheckAgainst;
            IsOutdated = _versionToCheckAgainst < _expectedVersion;
        } catch(Exception e) {
            GameHandler.ClientLog.Write($"{e.Message}\n{e.StackTrace}", LogType.ErrorFatal);
            GameHandler.ClientLog.Write($"An exception was thrown during TanksRebirth Version fetching process. Auto-Update backend and version checking cannot execute.", LogType.ErrorFatal);
        }
    }

    public void FetchData() {
        m_getRepo(out var tag, out var name, out var assets);
        if (!Version.TryParse(tag.Replace("-alpha", string.Empty), out _expectedVersion))
            throw new Exception("Failed to grab a recent version from GitHub.");
    }

    public Version GetRecentVersion() => _expectedVersion;
#pragma warning restore
    public void DownloadUpdate() {
        var assetWeWant = _assets.First(x => x.ToLower().Contains("tanks_rebirth"));
        var linkDl = _ghLink + "/releases/download/" + _tag + "/" + assetWeWant;
        Task.Run(() => {
            var bytes = WebUtils.DownloadWebFile(linkDl, out var name1);

            File.WriteAllBytes(assetWeWant, bytes);

            m_extractRarArchiveAndDelete(linkDl);
        });
    }

    private void m_extractRarArchiveAndDelete(string file) {
        using (var archive = new RarArchive(file)) {
            archive.ExtractToDirectory(Directory.GetCurrentDirectory());
        }
        File.Delete(file);
        OnDownloadComplete?.Invoke(_name, _ghLink);
    }

    private static void m_getRepo(out string relTag, out string relName, out string[] assetNames) {
        var client = new GitHubClient(new("RighteousRyan1"), new Uri("https://github.com/RighteousRyan1/TanksRebirth"));
        var releases = client.Repository.Release.GetAll("RighteousRyan1", "TanksRebirth").GetAwaiter().GetResult();
        var latest = releases[0];

        relTag = latest.TagName;
        relName = latest.Name;
        assetNames = latest.Assets.Select(x => x.Name).ToArray();
    }
}
