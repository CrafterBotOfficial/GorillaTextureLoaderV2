using System.IO;

namespace GorillaTextureLoader;

public static class Paths
{
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
