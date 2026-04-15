/*
 * TexArrayAtlas_2048x2048_BC7_AllScenes - forestatlas
 * TexArrayAtlas_256x256_BC7_AllScenes - pitground
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    private Dictionary<string, Texture2DArray> cachedGameTextures = [];
    private Dictionary<string, Material[]> cachedMaterials = [];

    public void Initialize()
    {
        PackMetas = LoadAllPackMetasAsync();
        PackMetas.ContinueWith(_ => { if (Jerald.PageManager.Instance.GetPage() is MainPage) MainPage.Update(); }); // ensure page updates when all packs are loaded
    }

    public void LoadPack(TexturePackMeta meta)
    {
        var combined = loaderV2.LoadPack(meta);

        // todo: add to remote server + offline for names->key
        Main.Log($"Applying texture {(meta.ForceNew ? "New" : "Slice")}");
        var watch = Stopwatch.StartNew();
        if (meta.ForceNew)
        {
            ApplyFrom("TexArrayAtlas_2048x2048_BC7_AllScenes", CreateTextureArray([.. combined["forestatlas"].OrderBy(x => x.Key).Select(x => x.Value)]));
            ApplyFrom("TexArrayAtlas_256x256_BC7_AllScenes", CreateTextureArray([.. combined["pitground"].OrderBy(x => x.Key).Select(x => x.Value)]));
        }
        else
        {
            ApplyTo("TexArrayAtlas_2048x2048_BC7_AllScenes", "forestatlas", combined);
            ApplyTo("TexArrayAtlas_256x256_BC7_AllScenes", "pitground", combined);
        }
        watch.Stop();
        Main.Log($"Finished in {watch.Elapsed.Seconds} {watch.Elapsed.Milliseconds}ms");
        Current = meta;
    }

    public void UnloadPack()
    {
        if (Current is null || cachedGameTextures.Count == 0)
            return;
        Main.Log("Reset");
        Current = null;
        foreach (var texturePair in cachedGameTextures)
        {
            var materials = FindMaterialByTextureName(texturePair.Key);
            foreach (var material in materials)
                material.SetTexture("_BaseMap_Atlas", texturePair.Value);
        }
    }

    private Texture2DArray CreateTextureArray(Texture2D[] textures)
    {
        var slice0 = textures[0];
        var textureArray = new Texture2DArray(
            slice0.width,
            slice0.height,
            textures.Length,
            slice0.format,
            false,
            false
        );

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i].width != slice0.width || textures[i].height != slice0.height)
            {
                Main.Log("Invalid textrurepack", BepInEx.Logging.LogLevel.Error);
                break;
            }

            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        textureArray.filterMode = FilterMode.Point;
        textureArray.anisoLevel = 0;
        textureArray.mipMapBias = 0f;
        textureArray.Apply(false, false);
        return textureArray;
    }

    private void ApplyFrom(string textureName, Texture2DArray textures)
    {
        var materials = FindMaterialByTextureName(textureName);
        CacheGameTextures(textureName, materials.First().GetTexture("_BaseMap_Atlas") as Texture2DArray);
        foreach (var material in materials)
        {
            material.SetTexture("_BaseMap_Atlas", textures);
        }
    }

    private void ApplyTo(string textureName, string key, Dictionary<string, Dictionary<int, Texture2D>> combined)
    {
        var materials = FindMaterialByTextureName(textureName);
        var atlas = materials.First().GetTexture("_BaseMap_Atlas") as Texture2DArray;
        CacheGameTextures(textureName, atlas);

        try
        {
            var newAtlas = new Texture2DArray(atlas.width, atlas.height, atlas.depth, TextureFormat.DXT5, false, false); // lin true, bc7 compresison TextureFormat.DXT5
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
                var renderTexture = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var sliceTexture = new Texture2D(atlas.width, atlas.height, TextureFormat.BC7, false, true);
                Graphics.CopyTexture(atlas, i, 0, sliceTexture, 0, 0);
                Graphics.Blit(sliceTexture, renderTexture);

                var readback = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
                RenderTexture.active = renderTexture;
                readback.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                readback.Apply();
                RenderTexture.ReleaseTemporary(renderTexture);
                GameObject.Destroy(sliceTexture);

                var dxt5 = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
                Graphics.CopyTexture(readback, dxt5);
                dxt5.Apply(false);
                GameObject.Destroy(readback);

                dxt5.Compress(true);
                Graphics.CopyTexture(dxt5, 0, 0, newAtlas, i, 0);
                // GameObject.Destroy(dxt5);
            }

            newAtlas.name = textureName;
            newAtlas.filterMode = FilterMode.Point;
            materials.ForEach(mat => mat.SetTexture("_BaseMap_Atlas", newAtlas));
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {key} {ex.Message}", BepInEx.Logging.LogLevel.Error);
        }
    }

    private void CacheGameTextures(string textureName, Texture2DArray atlas)
    {
        if (!cachedGameTextures.ContainsKey(textureName))
        {
            Main.Log("Saving default textures");
            var cache = new Texture2DArray(atlas.width, atlas.height, atlas.depth, atlas.format, 0, false)
            {
                filterMode = atlas.filterMode,
                anisoLevel = atlas.anisoLevel,
                wrapMode = atlas.wrapMode,
            };

            Graphics.CopyTexture(atlas, cache);
            cachedGameTextures.Add(textureName, cache);
        }
    }

    // note: cant just edit shared materials due to pitground mat
    private Material[] FindMaterialByTextureName(string name)
    {
        if (cachedMaterials.TryGetValue(name, out var cached))
            return cached;

        const string KEY = "_BaseMap_Atlas";
        var materials = GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.InstanceID)
            .Where(x => x.sharedMaterial is not null && x.sharedMaterial.HasTexture(KEY))
            .Where(x => x.sharedMaterial.GetTexture(KEY)?.name == name)
            .Select(x => x.sharedMaterial)
            .ToArray();

        cachedMaterials.Add(name, materials);
        return materials;
    }

    // Todo: Add legacy loader
    public async Task<TexturePackMeta[]> LoadAllPackMetasAsync()
    {
        if (metadatas is not null) return metadatas;
        var result = await loaderV2.LoadAllMetadatas();
        metadatas = result;
        return result;
    }

    public TextureFormat GetAtlasFormat()
    {
        var format = (FindMaterialByTextureName("TexArrayAtlas_2048x2048_BC7_AllScenes").First().GetTexture("_BaseMap_Atlas") as Texture2DArray).format;
        Main.Log($"{format.GetName()}");
        return format;
    }
}
