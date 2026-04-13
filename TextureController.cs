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

    public Task<TexturePackMeta[]> PackMetas;
    public TexturePackMeta Current;

    private readonly ILoader loaderV2 = new LoaderV2();
    private TexturePackMeta[] metadatas;

    public void Initialize()
    {
        PackMetas = LoadAllPackMetasAsync();
        PackMetas.ContinueWith(_ => MainPage.Update()); // ensure page updates when all packs are loaded
    }

    public void LoadPack(TexturePackMeta meta)
    {
        (_, var combined) = loaderV2.LoadPack(meta);

        UnityEngine.GameObject.Find("UnityTempFile-569358720e8bdbe48a564ee0113f0542 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["forestatlas"]);
        UnityEngine.GameObject.Find("UnityTempFile-201cbd57f079d244fa831efae4c2e050 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
        UnityEngine.GameObject.Find("UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
        // UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)
        Current = meta;
    }

    // Todo: Add legacy loader
    public async Task<TexturePackMeta[]> LoadAllPackMetasAsync()
    {
        if (metadatas is not null) return metadatas;
        var result = await loaderV2.LoadAllMetadatas();
        metadatas = result;
        return result;
    }
}
