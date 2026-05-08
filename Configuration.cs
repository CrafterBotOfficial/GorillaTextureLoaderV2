using BepInEx.Configuration;

namespace GorillaTextureLoader;

public static class Configuration
{
    public static ConfigEntry<string> CurrentTexturePack;

    public static void Initialize(ConfigFile config)
    {
        CurrentTexturePack = config.Bind("General", "CurrentTexturePack", "");
    }
}
