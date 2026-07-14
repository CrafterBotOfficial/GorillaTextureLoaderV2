using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaTextureLoader;

public class UpdateChecker : MonoBehaviour
{
    public volatile static bool UpdateAvailable;

    private Task checkTask;

    private void Awake()
    {
        checkTask = CheckForUpdates();
    }

    private async Task CheckForUpdates()
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
        finally
        {
            Destroy(this);
        }
    }
}

// https://docs.gitea.com/api/#tag/repository/operation/repoGetLatestRelease
public record struct GiteaApiResponse(string tag_name);
