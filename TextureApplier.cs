using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public class TextureApplier(TextureCache cache)
{
    private readonly List<Material> processedMaterials = [];

    public void Start(Loader.LoadedPack combined)
    {
        foreach (var pair in combined.Remaps)
        {
            // force new is for the upscaled textures
            // if (meta.ForceNew) ApplyFrom(pair.Key, TextureController.Instance.CreateTextureArray([.. pair.Value.OrderBy(x => x.Key).Select(x => x.Value)]));
            // else 
            ApplyTo(pair.Key, pair.Value);
        }

        ApplySingles(combined.Singles);
    }

    private void ApplySingles(Dictionary<string, Texture2D> singles)
    {
        foreach (var pair in singles)
        {
            string textureName = RemapManager.Instance.GetJson().Singles[pair.Key];
            var materials = cache.FindMaterialByTextureName(textureName, "_BaseMap");
            if (materials.Length == 0)
            {
                Main.Log($"No materials found for single {pair.Key} mat name: {textureName}", BepInEx.Logging.LogLevel.Warning);
                continue;
            }

            cache.CacheGameTextures(textureName, materials.First().GetTexture("_BaseMap") as Texture2D); // grabs random sample

            foreach (var material in materials)
            {
                material.SetTexture("_BaseMap", pair.Value);
            }
        }
    }

    private void ApplyTo(string textureName, Dictionary<int, Texture2D> combined)
    {
        var materials = cache.FindMaterialByTextureName(textureName)
            .Where(mat => !processedMaterials.Contains(mat))
            .ToArray();
        if (materials.Length == 0) return;
        processedMaterials.AddRange(materials);

        var atlas = materials.First().GetTexture(Paths.MAIN_ATLAS_KEY) as Texture2DArray;
        cache.CacheGameTextures(textureName, atlas);

        try
        {
            var newAtlas = new Texture2DArray(atlas.width, atlas.height, atlas.depth, TextureFormat.RGBA32, false, false);
            var renderTexture = new RenderTexture(atlas.width, atlas.height, 0, format: RenderTextureFormat.ARGB32);
            for (int i = 0; i < newAtlas.depth; i++)
            {
                if (combined.TryGetValue(i, out var custom))
                {
                    Graphics.CopyTexture(custom, 0, 0, newAtlas, i, 0);
                    continue;
                }
                Graphics.Blit(atlas, renderTexture, i, 0);
                var temp = new Texture2D(atlas.width, atlas.height);
                RenderTexture.active = renderTexture;
                temp.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
                temp.Apply();
                Graphics.CopyTexture(temp, 0, 0, newAtlas, i, 0);
                GameObject.Destroy(temp);
            }
            RenderTexture.active = null;
            renderTexture.Release();

            newAtlas.filterMode = FilterMode.Point;
            materials.ForEach(mat => mat.SetTexture(Paths.MAIN_ATLAS_KEY, newAtlas));
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {ex.Message}", BepInEx.Logging.LogLevel.Error);
        }
    }
}
