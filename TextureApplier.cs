using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

public class TextureApplier
{
    private List<Material> sharedMaterials = [];

    private Dictionary<string, Texture2DArray> cachedGameTextures = [];
    private Dictionary<string, Material[]> cachedMaterials = [];

    public TextureApplier(Dictionary<string, Texture2DArray> cachedGameTextures, Dictionary<string, Material[]> cachedMaterials)
    {
        this.cachedGameTextures = cachedGameTextures;
        this.cachedMaterials = cachedMaterials;
    }

    public void Start(TexturePackMeta meta, Dictionary<string, Dictionary<int, Texture2D>> combined)
    {
        foreach (var pair in combined)
        {
            if (meta.ForceNew) ApplyFrom(pair.Key, TextureController.Instance.CreateTextureArray([.. pair.Value.OrderBy(x => x.Key).Select(x => x.Value)]));
            else ApplyTo(pair.Key, pair.Value);
        }
    }

    private void ApplyFrom(string textureName, Texture2DArray textures)
    {
        var materials = TextureController.Instance.FindMaterialByTextureName(textureName);
        if (materials.Length == 0)
        {
            Main.Log("Failed to find textures for " + textureName, BepInEx.Logging.LogLevel.Error);
            return;
        }

        TextureController.Instance.CacheGameTextures(textureName, materials.First().GetTexture("_BaseMap_Atlas") as Texture2DArray);
        foreach (var material in materials)
        {
            Main.Log($"Applying {textureName} to {material.name}");
            material.SetTexture("_BaseMap_Atlas", textures);
        }
    }

    private void ApplyTo(string textureName, Dictionary<int, Texture2D> combined)
    {
        var materials = TextureController.Instance.FindMaterialByTextureName(textureName)
            .Where(mat => !sharedMaterials.Contains(mat))
            .ToArray();
        if (materials.Length == 0) return;
        sharedMaterials.AddRange(materials);

        var atlas = materials.First().GetTexture("_BaseMap_Atlas") as Texture2DArray;
        TextureController.Instance.CacheGameTextures(textureName, atlas);

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
                // GameObject.Destroy(dxt5);
            }

            newAtlas.name = textureName;
            newAtlas.filterMode = FilterMode.Point;
            materials.ForEach(mat => mat.SetTexture("_BaseMap_Atlas", newAtlas));
        }
        catch (Exception ex)
        {
            Main.Log($"Failed to apply texture {textureName} {ex.Message}", BepInEx.Logging.LogLevel.Error);
        }
    }
}
