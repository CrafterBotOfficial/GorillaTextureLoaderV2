using BepInEx.Configuration;

namespace GorillaTextureLoader;

public static class Configuration
{
    public static ConfigEntry<bool> EnableAutoLoad;
    public static ConfigEntry<string> CurrentTexturePack;

    public static ConfigEntry<bool> EnableBackgroundTextureLoading;

    public static void Initialize(ConfigFile config)
    {
        EnableAutoLoad = config.Bind("General", "EnableAutoLoad", true);
        CurrentTexturePack = config.Bind("General", "CurrentTexturePack", "");

        EnableBackgroundTextureLoading = config.Bind("Advanced", "Background Texture Loading", true, "Loads all textures immediately when the game starts. May use a lot of vram depending on the amount of textures you have.");
    }
}
