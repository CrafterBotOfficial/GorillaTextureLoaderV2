using System;
using System.Net.Http;

namespace GorillaTextureLoader;

public class UpdateChecker
{
    public volatile static bool UpdateAvailable;

    public async void DoCheck()
    {
        try
        {
            using var client = new HttpClient();

            const string URL = "https://git.crafterbot.com/api/v1/repos/Crafterbot/GorillaTextureLoader/releases/latest";
            var releaseInfo = await client.GetAsync(URL);
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<GiteaApiResponse>(await releaseInfo.Content.ReadAsStringAsync());

            var version = Version.Parse(json.tag_name);
            if (version > Main.Instance.Info.Metadata.Version)
            {
                Main.Log("New update", BepInEx.Logging.LogLevel.Warning);
                UpdateAvailable = true;
            }

        }
        catch (Exception ex)
        {
            Main.Log($"Update check failed {ex}", BepInEx.Logging.LogLevel.Error);
        }
    }
}

// https://docs.gitea.com/api/#tag/repository/operation/repoGetLatestRelease
public record struct GiteaApiResponse(string tag_name);
