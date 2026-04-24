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
    public static RemapManager Instance = instance.Value;

    private const string BASE_URL = "https://git.crafterbot.com/Crafterbot/GorillaTextureLoader/raw/branch/v2/";

    public Dictionary<string, int> Remaps;

    public RemapManager()
    {

    }

    public async Task<int> RemapTexture(string texture_name, string compiledGameVersion)
    {
        string gameVersion = GorillaNetworking.GorillaComputer.instance.version;
        Main.Log($"Game version {gameVersion} texturepack version {compiledGameVersion}", BepInEx.Logging.LogLevel.Debug);
        if (Remaps is null)
        {
            using var stream = typeof(RemapManager).Assembly.GetManifestResourceStream("GorillaTextureLoader.Remaps.json");
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var remaps = JsonConvert.DeserializeObject<RemapsJson>(await reader.ReadToEndAsync());
                if (remaps.GameVersion == gameVersion)
                {
                    Main.Log("Using local remaps");
                    Remaps = remaps.Remaps;
                }
            }
            else
            {
                Main.Log("Fetching remote remaps");
                using var client = new HttpClient();
                var response = await client.GetAsync(BASE_URL + "Remaps.json");
                if (!response.IsSuccessStatusCode) throw new System.Exception("Failed to get remote remaps. Mod will not work. " + response.StatusCode);
                Remaps = JsonConvert.DeserializeObject<RemapsJson>(await response.Content.ReadAsStringAsync()).Remaps;
            }
        }

        return Remaps[texture_name];
    }

    private record struct RemapsJson(string GameVersion, Dictionary<string, int> Remaps);
}
