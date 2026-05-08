using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public class TextureApplier(TextureCache cache)
{
    private readonly List<Material> processedMaterials = [];

    public void Start(TexturePackMeta meta, Dictionary<string, Dictionary<int, Texture2D>> combined)
    {
        var cacheBuilder = new Dictionary<string, Texture2DArray>();
        foreach (var pair in combined)
        {
            // force new is for the upscaled textures
            // if (meta.ForceNew) ApplyFrom(pair.Key, TextureController.Instance.CreateTextureArray([.. pair.Value.OrderBy(x => x.Key).Select(x => x.Value)]));
            // else 
            (string textureName, var array) = ApplyTo(pair.Key, pair.Value);
            if (Configuration.EnableCaching.Value)
                cacheBuilder.Add(textureName, array);
        }
        cache.CacheTexturePack(meta, cacheBuilder);
    }

    // for cache
    public void Start(Dictionary<string, Texture2DArray> textures)
    {
        foreach (var pair in textures)
        {
            ApplyFrom(pair.Key, pair.Value);
        }
    }

    private void ApplyFrom(string textureName, Texture2DArray textures)
    {
        var materials = cache.FindMaterialByTextureName(textureName);
        if (materials.Length == 0)
        {
            Main.Log("Failed to find textures for " + textureName, BepInEx.Logging.LogLevel.Error);
            return;
        }

        cache.CacheGameTextures(textureName, materials.First().GetTexture(Paths.MAIN_ATLAS_KEY) as Texture2DArray);
        foreach (var material in materials)
        {
            Main.Log($"Applying {textureName} to {material.name}");
            material.SetTexture(Paths.MAIN_ATLAS_KEY, textures);
        }
    }

    private (string, Texture2DArray) ApplyTo(string textureName, Dictionary<int, Texture2D> combined)
    {
        var materials = cache.FindMaterialByTextureName(textureName)
            .Where(mat => !processedMaterials.Contains(mat))
            .ToArray();
        if (materials.Length == 0) return default;
        processedMaterials.AddRange(materials);

        var atlas = materials.First().GetTexture(Paths.MAIN_ATLAS_KEY) as Texture2DArray;
        cache.CacheGameTextures(textureName, atlas);

        try
        {
            var newAtlas = new Texture2DArray(atlas.width, atlas.height, atlas.depth, TextureFormat.DXT5, false, false); // lin true, bc7 compresison TextureFormat.DXT5
            for (int i = 0; i < atlas.depth; i++)
            {
                if (combined.TryGetValue(i, out var texture))
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
                GameObject.Destroy(dxt5);
            }

            newAtlas.name = textureName;
            newAtlas.filterMode = FilterMode.Point;
            materials.ForEach(mat => mat.SetTexture(Paths.MAIN_ATLAS_KEY, newAtlas));
            return (textureName, newAtlas);
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {ex.Message}", BepInEx.Logging.LogLevel.Error);
            return default;
        }
    }
}
