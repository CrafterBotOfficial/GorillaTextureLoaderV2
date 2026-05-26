using BepInEx.Configuration;

namespace GorillaTextureLoader;

public static class Configuration
{
    // public static ConfigEntry<string> CurrentTexturePack;
    public static ConfigEntry<bool> EnableCaching;

    public static void Initialize(ConfigFile config)
    {
        // CurrentTexturePack = config.Bind("General", "CurrentTexturePack", "");
        EnableCaching = config.Bind("General", "Enable Caching", true, "Keeps loaded texturepacks in memory. May increase ram usage.");
    }
}
