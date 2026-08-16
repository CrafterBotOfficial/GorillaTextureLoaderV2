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
    private readonly static Lazy<RemapManager> instance = new Lazy<RemapManager>(() => new RemapManager());
    public static RemapManager Instance => instance.Value;

    public Task InitializeTask;

    private RemapsJson json;
    private Dictionary<string, int> remaps; // without atlas names, since they aren't needed.

    public RemapManager()
    {
        InitializeTask = Task.Run(Initialize);
    }

    public async Task Initialize()
    {
        if (remaps is not null) return;
        Main.Log("Initializing remapsmanager");

        string gameVersion = NetworkSystemConfig.BundleVersion;
        using var stream = typeof(RemapManager).Assembly.GetManifestResourceStream("GorillaTextureLoader.Remaps.json");
        using var reader = new StreamReader(stream);
        var tempJson = JsonConvert.DeserializeObject<RemapsJson>(reader.ReadToEnd());
        Main.Log($"Local {tempJson.GameVersion} game {gameVersion}");
        if (tempJson.GameVersion == gameVersion)
        {
            json = tempJson;
            remaps = JoinDictionaries([.. json.Remaps.Values]);
            return;
        }

        Main.Log("Mismatching game versions with local remaps. Trying to use external.", BepInEx.Logging.LogLevel.Warning);
        await SetRemapsToRemote();
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

    /// <summary>
    /// Mega jank method joins both remaps and singles. 
    /// Singles are formatted as a dictionary aswell just with only 1 value and a dumby remap.
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> GetRemapAndSinglessWithAtlas() // todo: make not jank
    {
        var result = new Dictionary<string, Dictionary<string, int>>(json.Remaps);
        foreach (var single in json.Singles)
        {
            result.Add(single.Key, new Dictionary<string, int>(1){
                 { single.Value, 1 }
            });
        }
        return result;
    }

    public Dictionary<string, int> GetRemaps()
    {
        if (!InitializeTask.IsCompleted)
        {
            Main.Log("Incorrect logic flow. GetRemaps being called before remaps population task finishes " + InitializeTask?.Status, BepInEx.Logging.LogLevel.Warning);
            InitializeTask.Wait(); // blocking, but should never occur
        }
        return remaps;
    }

    public (bool found, string atlasName) GetAtlasFromTextureName(string input)
    {
        _ = GetRemaps();
        foreach (var map in json.Remaps)
        {
            foreach (var pair in map.Value)
            {
                if (pair.Key == input)
                {
                    return (true, map.Key);
                }
            }
        }

        if (json.Singles.ContainsKey(input))
        {
            return (true, string.Empty);
        }
        Main.Log("Malformed pack " + input, LogLevel.Warning);
        return (false, string.Empty);
    }

    public bool IsSingle(string input)
    {
        return json.Singles.ContainsKey(input);
    }

    public RemapsJson GetJson()
    {
        return json;
    }

    public string GetLatestVersion()
    {
        if (string.IsNullOrEmpty(json?.GameVersion)) GetRemaps(); // force populate gamever
        return json.GameVersion;
    }

    private async Task SetRemapsToRemote()
    {
        Main.Log("Fetching remote remaps", BepInEx.Logging.LogLevel.Debug);
        using var client = new HttpClient();
        var response = await client.GetAsync(Paths.BASE_URL + "Remaps.json");
        string text = await response.Content.ReadAsStringAsync();
        json = JsonConvert.DeserializeObject<RemapsJson>(text);
        remaps = JoinDictionaries([.. json.Remaps.Values]);
        Main.Log("Got with version " + json.GameVersion, LogLevel.Message);
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

    public record class RemapsJson(string GameVersion, Dictionary<string, Dictionary<string, int>> Remaps, Dictionary<string, string> Singles);
}
