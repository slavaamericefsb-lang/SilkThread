using System;
using System.Linq;
using System.Threading.Tasks;
using SilkThread.Core.GameProfiles;
using Xunit;

namespace SilkThread.Tests;

public class GameProfileTests
{
    [Fact]
    public void Catalog_ShouldContainAll8TargetGames()
    {
        var profiles = GameProfileCatalog.GetAllProfiles();
        Assert.Equal(8, profiles.Count);

        var expectedIds = new[] { "cs2", "dota2", "valorant", "apex", "fortnite", "warzone", "pubg", "r6s" };
        foreach (var id in expectedIds)
        {
            var match = profiles.FirstOrDefault(p => p.Id == id);
            Assert.NotNull(match);
        }
    }

    [Fact]
    public void Catalog_AllGames_ShouldHaveValidContentAndSettings()
    {
        var profiles = GameProfileCatalog.GetAllProfiles();

        foreach (var profile in profiles)
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.Id), "Id is required");
            Assert.False(string.IsNullOrWhiteSpace(profile.Title), $"Title required for {profile.Id}");
            Assert.False(string.IsNullOrWhiteSpace(profile.EngineBadge), $"Engine required for {profile.Id}");
            Assert.False(string.IsNullOrWhiteSpace(profile.Launcher), $"Launcher required for {profile.Id}");
            Assert.False(string.IsNullOrWhiteSpace(profile.LaunchOptions), $"LaunchOptions required for {profile.Id}");
            Assert.NotEmpty(profile.ExecutableNames);
            Assert.NotEmpty(profile.LaunchArgs);
            Assert.NotEmpty(profile.VideoSettings);
            Assert.NotEmpty(profile.ConfigFiles);

            foreach (var arg in profile.LaunchArgs)
            {
                Assert.False(string.IsNullOrWhiteSpace(arg.Argument));
                Assert.False(string.IsNullOrWhiteSpace(arg.Purpose));
                Assert.False(string.IsNullOrWhiteSpace(arg.Impact));
            }

            foreach (var vs in profile.VideoSettings)
            {
                Assert.False(string.IsNullOrWhiteSpace(vs.Name));
                Assert.False(string.IsNullOrWhiteSpace(vs.RecommendedValue));
            }

            foreach (var cfg in profile.ConfigFiles)
            {
                Assert.False(string.IsNullOrWhiteSpace(cfg.FileName));
                Assert.False(string.IsNullOrWhiteSpace(cfg.Path));
            }
        }
    }

    [Fact]
    public void Catalog_CS2_ShouldContainEssentialEsportsFlags()
    {
        var cs2 = GameProfileCatalog.GetProfileById("cs2");
        Assert.NotNull(cs2);
        Assert.Contains("-novid", cs2.LaunchOptions);
        Assert.Contains("-fullscreen", cs2.LaunchOptions);
        Assert.Contains("+fps_max 0", cs2.LaunchOptions);
        Assert.Contains("-nojoy", cs2.LaunchOptions);
        Assert.Contains("autoexec.cfg", cs2.LaunchOptions);
        Assert.Equal("Source 2", cs2.EngineBadge);
    }

    [Fact]
    public void Catalog_Dota2_ShouldContainPreloadMapAndHighFlags()
    {
        var dota = GameProfileCatalog.GetProfileById("dota2");
        Assert.NotNull(dota);
        Assert.Contains("-map dota", dota.LaunchOptions);
        Assert.Contains("-high", dota.LaunchOptions);
        Assert.Contains("+fps_max 0", dota.LaunchOptions);
        Assert.Equal(GameGenreCategory.Moba, dota.GenreCategory);
    }

    [Fact]
    public void Catalog_Valorant_ShouldContainRiotServicesOptions()
    {
        var valorant = GameProfileCatalog.GetProfileById("valorant");
        Assert.NotNull(valorant);
        Assert.Contains("--launch-product=valorant", valorant.LaunchOptions);
        Assert.Contains("--launch-patchline=live", valorant.LaunchOptions);
        Assert.Contains("Vanguard", valorant.EngineBadge);
    }

    [Fact]
    public void Catalog_Fortnite_ShouldContainCoreAndTextureStreamingOptions()
    {
        var fortnite = GameProfileCatalog.GetProfileById("fortnite");
        Assert.NotNull(fortnite);
        Assert.Contains("-useallavailablecores", fortnite.LaunchOptions);
        Assert.Contains("-nosplash", fortnite.LaunchOptions);
        Assert.Contains("GameUserSettings.ini", fortnite.ConfigFiles.First().FileName);
    }

    [Fact]
    public void Catalog_PUBG_ShouldContainMemoryAllocAndCoreOptions()
    {
        var pubg = GameProfileCatalog.GetProfileById("pubg");
        Assert.NotNull(pubg);
        Assert.Contains("-USEALLAVAILABLECORES", pubg.LaunchOptions);
        Assert.Contains("-malloc=system", pubg.LaunchOptions);
    }

    [Fact]
    public void Service_GetProfileById_ShouldReturnNullOnMissing()
    {
        var service = new GameProfileService();
        var missing = service.GetProfileById("nonexistent_game_xyz");
        Assert.Null(missing);
    }

    [Fact]
    public async Task Service_ApplyAndRevertProfile_ShouldExecuteWithoutCrashing()
    {
        var service = new GameProfileService();
        var cs2 = service.GetProfileById("cs2");
        Assert.NotNull(cs2);

        // Applying system profile
        bool applied = await service.ApplyProfileAsync("cs2");
        // May succeed or partially succeed depending on current test process rights (IFEO requires admin)
        Assert.True(applied);

        // Reverting system profile
        bool reverted = await service.RevertProfileAsync("cs2");
        Assert.True(reverted);
    }
}
