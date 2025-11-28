using System;
using System.IO;
using GorillaTextureLoader.Loader;
using GorillaTextureLoader.Map;

namespace GorillaTextureLoader;

public class TextureController
{
    private static Lazy<TextureController> instance => new Lazy<TextureController>(() => new TextureController());
    public static TextureController Instance => instance.Value;

    public void LoadTexture()
    {
        ILoader loader = new Loader.LegacyLoader();
        var packs = loader.GetTexturePacks(Directory.GetParent(typeof(TextureController).Assembly.Location).ToString() + "/packs/");
        loader.LoadTextures(EMap.Forest, packs[0]);

        var map = new Map.ForestTextureManager().Setup();
        map.SetTextures(packs[0]);
    }
}
