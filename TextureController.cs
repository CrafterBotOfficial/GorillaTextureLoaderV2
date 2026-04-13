using System;
using System.IO;
using System.Threading.Tasks;
using GorillaTextureLoader.Loader;
using UnityEngine;

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
        void ApplyTo(string objName, string key)
        {
            var atlas = UnityEngine.GameObject.Find(objName).GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.GetTexture("_BaseMap_Atlas") as Texture2DArray;
            var newAtlas = new Texture2DArray(atlas.width, atlas.height, atlas.depth, TextureFormat.DXT5, false, true);
            for (int i = 0; i < atlas.depth; i++)
            {
                if (combined[key].TryGetValue(i, out var texture))
                {
                    Main.Log("Copying modified " + i, BepInEx.Logging.LogLevel.Debug);
                    Graphics.CopyTexture(texture, 0, 0, newAtlas, i, 0);
                    continue;
                }
                // base slice
                Main.Log("Copying base " + i, BepInEx.Logging.LogLevel.Debug);
                Graphics.CopyTexture(atlas, i, 0, newAtlas, i, 0);
            }
            GameObject.Find(objName).GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", newAtlas);
        }

        ApplyTo("UnityTempFile-569358720e8bdbe48a564ee0113f0542 (combined by EdMeshCombiner)", "forestatlas");
        ApplyTo("UnityTempFile-201cbd57f079d244fa831efae4c2e050 (combined by EdMeshCombiner)", "pitground");
        ApplyTo("UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)", "pitground");

        // UnityEngine.GameObject.Find("UnityTempFile-569358720e8bdbe48a564ee0113f0542 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", newAtlas);

        // UnityEngine.GameObject.Find("UnityTempFile-201cbd57f079d244fa831efae4c2e050 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
        // UnityEngine.GameObject.Find("UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)").GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
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
