using System;
using System.IO;
using System.Threading.Tasks;
using GorillaTextureLoader.Loader;

namespace GorillaTextureLoader;

public class TextureController
{
    private static Lazy<TextureController> instance = new Lazy<TextureController>(() => new TextureController());
    public static TextureController Instance => instance.Value;

    public const string TEXTURE_PACK_FILE_PREFIX = "*.pack";

    public string TexturePackPath = Path.Combine("BepInEx", "plugins", "GorillaTextureLoader", "packs"); // Todo: Make reliable

    private readonly ILoader loaderV2 = new LoaderV2();

    private TexturePackMeta[] metadatas;

    // Todo: Add legacy loader
    public async Task<TexturePackMeta[]> LoadAllPackMetasAsync()
    {
        if (metadatas is not null) return metadatas;
        var result = await loaderV2.LoadAllMetadatas();
        metadatas = result;
        return result;
    }
}
