using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public class TextureApplier(TextureCache cache)
{
    public List<Texture> TexturepackAtlases = [];
    private readonly List<Material> processedMaterials = [];

    public void Start(Loader.LoadedPack combined)
    {
        foreach (var pair in combined.Remaps)
        {
            ApplyTo(pair.Key, pair.Value);
        }

        ApplySingles(combined.Singles);
    }

    private void ApplySingles(Dictionary<string, Texture2D> singles)
    {
        foreach (var pair in singles)
        {
            string textureName = RemapManager.Instance.GetJson().Singles[pair.Key];
            var materials = cache.FindMaterialsByTextureName<Texture2D>(textureName, Paths.MAIN_KEY);
            if (materials.Length == 0)
            {
                Main.Log($"No materials found for single {pair.Key} mat name: {textureName}", BepInEx.Logging.LogLevel.Warning);
                continue;
            }

            cache.CacheGameTextures(textureName, materials.First().GetTexture(Paths.MAIN_KEY) as Texture2D); // grabs random sample

            foreach (var material in materials)
            {
                material.SetTexture(Paths.MAIN_KEY, pair.Value);
            }
        }
    }

    private void ApplyTo(string textureName, Dictionary<int, Texture2D> combined)
    {
        var materials = cache.FindMaterialsByTextureName<Texture2DArray>(textureName)
            .Where(mat => !processedMaterials.Contains(mat))
            .ToArray();
        if (materials.Length == 0) return;
        processedMaterials.AddRange(materials);

        var atlas = materials.First().GetTexture(Paths.MAIN_ATLAS_KEY) as Texture2DArray;
        cache.CacheGameTextures(textureName, atlas);

        int maxWidth = combined.Max(x => x.Value.width);
        int maxHeight = combined.Max(x => x.Value.height);
        bool doResize = maxWidth != atlas.width || maxHeight != atlas.height;
        if (doResize) Main.Log($"Using resolution {maxWidth},{maxHeight}", BepInEx.Logging.LogLevel.Warning);

        var newAtlas = new Texture2DArray(maxWidth, maxHeight, atlas.depth, TextureFormat.BC7, false, false);
        TexturepackAtlases.Add(newAtlas);

        try
        {
            if (doResize)
                foreach (var pair in combined)
                {
                    Graphics.CopyTexture(pair.Value, 0, 0, newAtlas, pair.Key, 0);
                }
            else
                for (int i = 0; i < atlas.depth; i++)
                {
                    if (combined.TryGetValue(i, out var customTexture)) Graphics.CopyTexture(customTexture, 0, 0, newAtlas, i, 0);
                    else Graphics.CopyTexture(atlas, i, 0, newAtlas, i, 0);
                }

            newAtlas.filterMode = FilterMode.Point;
            materials.ForEach(mat => mat?.SetTexture(Paths.MAIN_ATLAS_KEY, newAtlas));
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {ex}", BepInEx.Logging.LogLevel.Error);
        }
    }

    // last resort
    ~TextureApplier()
    {
        foreach (var atlas in TexturepackAtlases)
        {
            GameObject.Destroy(atlas);
        }
    }
}
