using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GorillaTextureLoader;

public class RemapManager
{
    private static Lazy<RemapManager> instance = new Lazy<RemapManager>(() => new RemapManager());
    public static RemapManager Instance => instance.Value;

    private const string BASE_URL = "https://git.crafterbot.com/Crafterbot/GorillaTextureLoader/raw/branch/v2/";

    private RemapsJson json;
    public Dictionary<string, int> remaps; // without atlas names, since they aren't needed

    public RemapManager()
    {

    }

    // todo: download remote remaps to allow offline play
    public async Task<int> RemapTexture(string texture_name, string compiledGameVersion)
    {
        return (await GetRemaps())[texture_name];
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

    public async Task<Dictionary<string, Dictionary<string, int>>> GetRemapsWithAtlas()
    {
        await GetRemaps();
        return json.Remaps;
    }

    public async Task<Dictionary<string, int>> GetRemaps()
    {
        string gameVersion = GorillaNetworking.GorillaComputer.instance.version;
        Main.Log($"Game version {gameVersion}", BepInEx.Logging.LogLevel.Debug);

        if (remaps is null)
        {
            using var stream = typeof(RemapManager).Assembly.GetManifestResourceStream("GorillaTextureLoader.Remaps.json");
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                json = JsonConvert.DeserializeObject<RemapsJson>(await reader.ReadToEndAsync());
                if (json.GameVersion == gameVersion)
                {
                    Main.Log("Using local remaps");
                    remaps = JoinDictionaries([.. json.Remaps.Values]);
                }
            }
            else
            {
                Main.Log("Fetching remote remaps");
                using var client = new HttpClient();
                var response = await client.GetAsync(BASE_URL + "Remaps.json");
                if (!response.IsSuccessStatusCode) throw new System.Exception("Failed to get remote remaps. Mod will not work. " + response.StatusCode);
                json = JsonConvert.DeserializeObject<RemapsJson>(await response.Content.ReadAsStringAsync());
                remaps = JoinDictionaries([.. json.Remaps.Values]);
            }
        }

        return remaps;
    }

    public async Task<string> GetLatestVersion()
    {
        if (string.IsNullOrEmpty(json.GameVersion))
        {
            await GetRemaps(); // force populate gamever
        }

        return json.GameVersion;
    }

    private record struct RemapsJson(string GameVersion, Dictionary<string, Dictionary<string, int>> Remaps);
}
