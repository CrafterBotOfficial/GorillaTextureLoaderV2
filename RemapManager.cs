using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace GorillaTextureLoader;

public class RemapManager
{
    private static Lazy<RemapManager> instance = new Lazy<RemapManager>(() => new RemapManager());
    public static RemapManager Instance => instance.Value;

    private static readonly object lockObject = new();
    private Task getRemoteRemapsTask;

    private RemapsJson json;
    private Dictionary<string, int> remaps; // without atlas names, since they aren't needed

    public RemapManager()
    {
    }

    public void Initialize()
    {
        if (remaps is not null || getRemoteRemapsTask is not null) return;
        Main.Log("Initializing remapsmanager");

        string gameVersion = NetworkSystemConfig.BundleVersion;
        SetRemapsToLocal();
        Main.Log($"Local {json.GameVersion} game {gameVersion}");
        if (json.GameVersion != gameVersion)
        {
            Main.Log("Mismatching game versions with local remaps. Trying to use external.", BepInEx.Logging.LogLevel.Warning);
            Main.Notify("Trying to use remote remaps", isWarning: true);
            getRemoteRemapsTask = SetRemapsToRemote().ContinueWith(task =>
            {
                Main.Log($"Failed to gte remote remaps " + task.Exception);
                Main.Notify("Outdated remaps, textures may not work properly. Please check internet connection or update the mod.", isError: true);
            }, TaskContinuationOptions.OnlyOnFaulted);
            SetRemapsToLocal();
        }
    }

    // todo: download remote remaps to allow offline play even when mod is outdated
    public int RemapTexture(string textureName)
    {
        if (GetRemaps().TryGetValue(textureName, out int newSliceIndex))
        {
            return newSliceIndex;
        }
        Main.Log("Improper texturepack", LogLevel.Warning);
        return -1;
    }

    public Dictionary<string, Dictionary<string, int>> GetRemapsWithAtlas()
    {
        return json.Remaps;
    }

    public Dictionary<string, int> GetRemaps()
    {
        if (remaps is null)
        {
            Main.Log("Incorrect logic flow. GetRemaps being called before remaps population task finishes " + getRemoteRemapsTask?.Status, BepInEx.Logging.LogLevel.Warning);
            if (getRemoteRemapsTask is null) Initialize();
            getRemoteRemapsTask.Wait(); // blocking, but should never occur
        }
        return remaps;
    }

    public string GetLatestVersion()
    {
        if (string.IsNullOrEmpty(json.GameVersion)) GetRemaps(); // force populate gamever
        return json.GameVersion;
    }

    private void SetRemapsToLocal()
    {
        using var stream = typeof(RemapManager).Assembly.GetManifestResourceStream("GorillaTextureLoader.Remaps.json");
        using var reader = new StreamReader(stream);
        json = JsonConvert.DeserializeObject<RemapsJson>(reader.ReadToEnd());
        remaps = JoinDictionaries([.. json.Remaps.Values]);
    }

    private async Task SetRemapsToRemote()
    {
        Main.Log("Fetching remote remaps", BepInEx.Logging.LogLevel.Debug);
        using var client = new HttpClient();
        var response = await client.GetAsync(Paths.BASE_URL + "Remaps.json");
        string text = await response.Content.ReadAsStringAsync();
        lock (lockObject)
        {
            json = JsonConvert.DeserializeObject<RemapsJson>(text);
            remaps = JoinDictionaries([.. json.Remaps.Values]);
            Main.Log("Got with version " + json.GameVersion, LogLevel.Message);
        }
    }

    private Dictionary<string, int> JoinDictionaries(Dictionary<string, int>[] dictionaries)
    {
        // return dictionaries[1];
        var result = new Dictionary<string, int>(dictionaries[0]);
        for (int i = 1; i < dictionaries.Length; i++)
        {
            foreach (var keyValuePair in dictionaries[i])
            {
                result.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        return result;
    }

    private record struct RemapsJson(string GameVersion, Dictionary<string, Dictionary<string, int>> Remaps);
}
