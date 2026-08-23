using TanksRebirth.GameContent.Systems;
using Xunit;

namespace TanksRebirth.Tests;

public sealed class CampaignPathTests
{
    [Fact]
    public void PreservesRootedPaths()
    {
        const string rootedPath = "/Users/example/Documents/My Games/Tanks Rebirth/Campaigns/Vanilla.campaign";

        Assert.Equal(rootedPath, Campaign.ResolveLoadPath("/Users/example/Documents/My Games/Tanks Rebirth", rootedPath));
    }

    [Fact]
    public void DoesNotDuplicateRelativeSaveDirectory()
    {
        const string relativeSaveDirectory = "My Games/Tanks Rebirth";
        const string enumeratedPath = "My Games/Tanks Rebirth/Campaigns/Vanilla.campaign";

        Assert.Equal(enumeratedPath, Campaign.ResolveLoadPath(relativeSaveDirectory, enumeratedPath));
    }

    [Fact]
    public void CombinesOrdinaryRelativePathsOnce()
    {
        Assert.Equal(
            "My Games/Tanks Rebirth/Campaigns/Vanilla.campaign",
            Campaign.ResolveLoadPath("My Games/Tanks Rebirth", "Campaigns/Vanilla.campaign"));
    }
}
