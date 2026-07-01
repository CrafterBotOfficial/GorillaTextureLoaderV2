using BepInEx.Configuration;

namespace GorillaTextureLoader;

public static class Configuration
{
    public static ConfigEntry<bool> EnableAutoLoad;
    public static ConfigEntry<string> CurrentTexturePack;

    public static void Initialize(ConfigFile config)
    {
        EnableAutoLoad = config.Bind("General", "EnableAutoLoad", true);
        CurrentTexturePack = config.Bind("General", "CurrentTexturePack", "");
    }
}
