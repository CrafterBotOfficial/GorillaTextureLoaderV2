using System.IO;

namespace GorillaTextureLoader;

public static class Paths
{
    public const string BASE_URL = "https://git.crafterbot.com/Crafterbot/GorillaTextureLoader/raw/branch/v2/"; // todo: add fall back url

    public const string MAIN_ATLAS_KEY = "_BaseMap_Atlas";
    public const string TEXTURE_PACK_FILE_SUFFIX = "*.pack";

    private static string texturePackDirectory;
    public static string TexturePackDirectory
    {
        get
        {
            if (texturePackDirectory.IsNullOrEmpty())
            {
                texturePackDirectory = Path.Combine(BepInEx.Paths.PluginPath, "GorillaTextureLoader", "packs");
                try { Directory.CreateDirectory(texturePackDirectory); } catch { }
            }
            return texturePackDirectory;
        }
    }
}
