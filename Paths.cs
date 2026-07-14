using System.IO;

namespace GorillaTextureLoader;

public static class Paths
{
    public const string BASE_URL = "https://raw.githubusercontent.com/CrafterBotOfficial/GorillaTextureLoaderV2/refs/heads/v2";

    public const string MAIN_KEY = "_BaseMap";
    public const string MAIN_ATLAS_KEY = "_BaseMap_Atlas";
    public const string TEXTURE_PACK_FILE_SUFFIX = "*.pack";

    private static string ModLocation => Path.GetDirectoryName(typeof(Main).Assembly.Location);

    private static string texturePackDirectory;
    public static string TexturePackDirectory
    {
        get
        {
            if (texturePackDirectory.IsNullOrEmpty())
            {
                texturePackDirectory = Path.Combine(ModLocation, "packs");
                Directory.CreateDirectory(texturePackDirectory);
            }
            return texturePackDirectory;
        }
    }
}
