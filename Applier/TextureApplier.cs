using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader.Applier;

public class TextureApplier(TextureCache cache) : IApplier
{
    private readonly Dictionary<string, Dictionary<int, Texture2D>> atlasCopies = [];
    private readonly Dictionary<string, (Texture2D original, Texture2D custom, Material[] materials)> originalSingleTextures = [];

    public void Start(LoadedPack combined)
    {
        ApplySingles(combined.Singles);

        foreach (var pair in combined.Remaps)
        {
            ApplyAtlas(pair.Key, pair.Value);
        }
    }

    public void Cleanup()
    {
        foreach (var pair in atlasCopies)
        {
            var atlas = cache.FindTexturesByName<Texture2DArray>(pair.Key).First();
            foreach (var touched in pair.Value)
            {
                Graphics.CopyTexture(touched.Value, 0, atlas, touched.Key);
                GameObject.Destroy(touched.Value);
            }
        }
        atlasCopies.Clear();

        foreach (var pair in originalSingleTextures)
        {
            foreach (var material in pair.Value.materials)
                if (material is not null)
                    material.SetTexture(Paths.MAIN_KEY, pair.Value.original);
        }
        originalSingleTextures.Clear();
    }

    private void ApplySingles(Dictionary<string, Texture2D> singles)
    {
        foreach (var pair in singles)
        {
            string textureName = RemapManager.Instance.GetJson().Singles[pair.Key];
            var materials = cache.FindMaterialsByTextureName<Texture2D>(textureName, Paths.MAIN_KEY);
            var texture = cache.FindTexturesByName<Texture2D>(textureName, Paths.MAIN_KEY).First();

            foreach (var material in materials)
            {
                material.SetTexture(Paths.MAIN_KEY, pair.Value);
            }

            originalSingleTextures.Add(pair.Key, (texture, pair.Value, materials));
        }
    }

    private void ApplyAtlas(string textureName, Dictionary<int, Texture2D> combined)
    {
        var atlas = cache.FindTexturesByName<Texture2DArray>(textureName).First();
        var copyHolder = new Dictionary<int, Texture2D>(combined.Count);

        try
        {
            for (int i = 0; i < atlas.depth; i++)
            {
                if (combined.TryGetValue(i, out var customTexture))
                {
                    var temp = new Texture2D(atlas.width, atlas.height, atlas.format, true);
                    Graphics.CopyTexture(atlas, i, temp, 0);
                    copyHolder.Add(i, temp);

                    Graphics.CopyTexture(customTexture, 0, atlas, i);
                }
            }
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {ex}", BepInEx.Logging.LogLevel.Error);
        }
        finally
        {
            atlasCopies.Add(textureName, copyHolder);
        }
    }
}
