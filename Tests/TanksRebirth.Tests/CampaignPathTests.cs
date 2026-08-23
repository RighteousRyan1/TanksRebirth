using TanksRebirth.GameContent.Systems;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class CampaignPathTests
{
    [Fact]
    public void PreservesRootedPaths()
    {
        var saveDirectory = Path.GetFullPath(Path.Combine("My Games", "Tanks Rebirth"));
        var rootedPath = Path.Combine(saveDirectory, "Campaigns", "Vanilla.campaign");

        Assert.Equal(rootedPath, Campaign.ResolveLoadPath(saveDirectory, rootedPath));
    }

    [Fact]
    public void DoesNotDuplicateRelativeSaveDirectory()
    {
        var relativeSaveDirectory = Path.Combine("My Games", "Tanks Rebirth");
        var enumeratedPath = Path.Combine(relativeSaveDirectory, "Campaigns", "Vanilla.campaign");

        Assert.Equal(enumeratedPath, Campaign.ResolveLoadPath(relativeSaveDirectory, enumeratedPath));
    }

    [Fact]
    public void CombinesOrdinaryRelativePathsOnce()
    {
        var relativeSaveDirectory = Path.Combine("My Games", "Tanks Rebirth");

        Assert.Equal(
            Path.Combine(relativeSaveDirectory, "Campaigns", "Vanilla.campaign"),
            Campaign.ResolveLoadPath(relativeSaveDirectory, Path.Combine("Campaigns", "Vanilla.campaign")));
    }

    [Fact]
    public void DoesNotDuplicateWindowsAlternateSeparators()
    {
        if (!OperatingSystem.IsWindows())
            return;

        const string relativeSaveDirectory = "My Games\\Tanks Rebirth";
        const string alternateSeparatorPath = "My Games/Tanks Rebirth/Campaigns/Vanilla.campaign";

        Assert.Equal(alternateSeparatorPath, Campaign.ResolveLoadPath(relativeSaveDirectory, alternateSeparatorPath));
    }

    [Fact]
    public void DoesNotDuplicateDifferentlyCasedWindowsSaveDirectory()
    {
        if (!OperatingSystem.IsWindows())
            return;

        const string relativeSaveDirectory = "My Games\\Tanks Rebirth";
        const string differentlyCasedPath = "my games\\tanks rebirth\\Campaigns\\Vanilla.campaign";

        Assert.Equal(differentlyCasedPath, Campaign.ResolveLoadPath(relativeSaveDirectory, differentlyCasedPath));
    }
}
