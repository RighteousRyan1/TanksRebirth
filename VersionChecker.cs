using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth;

public class VersionChecker {
    readonly string _ghLink;
    readonly string _tag;
    readonly string _name;
    readonly string[] _assets;
    readonly Version? _versionToCheckAgainst;
    Version? _expectedVersion;
    public bool IsOutdated { get; private set; }

#pragma warning disable
    public VersionChecker(string ghLink, Version? versionToCheckAgainst) {
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
            TankGame.ClientLog.Write($"{e.Message}\n{e.StackTrace}", LogType.ErrorFatal);
            TankGame.ClientLog.Write($"An exception was thrown during TanksRebirth Version fetching process. Auto-Update backend and version checking cannot execute.", LogType.ErrorFatal);
        }
    }

    public void FetchData() {
        m_getRepo(out var tag, out var name, out var assets);
        if (!Version.TryParse(tag.Replace("-alpha", string.Empty), out _expectedVersion))
            throw new Exception("Failed to grab a recent version from GitHub.");
    }

    public Version GetRecentVersion() => _expectedVersion;
#pragma warning restore

    private static void m_getRepo(out string relTag, out string relName, out string[] assetNames) {
        var client = new GitHubClient(new("RighteousRyan1"), new Uri("https://github.com/RighteousRyan1/TanksRebirth"));
        var releases = client.Repository.Release.GetAll("RighteousRyan1", "TanksRebirth").GetAwaiter().GetResult();
        var latest = releases[0];

        relTag = latest.TagName;
        relName = latest.Name;
        assetNames = [.. latest.Assets.Select(x => x.Name)];
    }
}
