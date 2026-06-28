using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

// Responicble for keeping track of original game textures, and remembering already loaded packs
public class TextureCache
{
    private readonly Dictionary<string, Texture> cachedGameTextures = [];
    private readonly Dictionary<string, MeshRenderer[]> cachedMeshRenderers = [];

    public MeshRenderer[] FindRenderersByTextureName(string name, string textureKey)
    {
        if (cachedMeshRenderers.TryGetValue(name, out var renderers))
            return renderers;

        renderers = [..GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.None)
            .Where(x => x.sharedMaterial is not null && x.sharedMaterial.HasTexture(textureKey))
            .Where(x => x.sharedMaterial.GetTexture(textureKey)?.name == name)];
        cachedMeshRenderers.Add(name, renderers);
        return renderers;
    }

    public Material[] FindMaterialByTextureName(string name, string textureKey = Paths.MAIN_ATLAS_KEY)
    {
        var materials = FindRenderersByTextureName(name, textureKey).Select(x => x.sharedMaterial).ToArray();
        return materials;
    }

    public Texture[] FindTextureByName(string name, string key = Paths.MAIN_ATLAS_KEY)
    {
        return [.. FindMaterialByTextureName(name, key).Select(x => x.GetTexture(key))];
    }

    // todo: combine with below method
    public void CacheGameTextures(string textureName, Texture2D texture)
    {
        if (cachedGameTextures.ContainsKey(textureName) || texture is null)
        {
            // Main.Log($"Not allowed. {textureName} {texture}", BepInEx.Logging.LogLevel.Warning);
            return;
        }

        Main.Log("Saving default textures for single");
        var cache = new Texture2D(texture.width, texture.height, texture.format, 0, false)
        {
            filterMode = texture.filterMode,
            anisoLevel = texture.anisoLevel,
            wrapMode = texture.wrapMode,
        };

        Graphics.CopyTexture(texture, cache);
        cachedGameTextures.Add(textureName, cache);
    }

    public void CacheGameTextures(string textureName, Texture2DArray atlas)
    {
        if (cachedGameTextures.ContainsKey(textureName) || atlas is null)
        {
            // Main.Log($"Not allowed. {textureName} {atlas}", BepInEx.Logging.LogLevel.Warning);
            return;
        }

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

    public Dictionary<string, Texture> GetOriginalGameTextures()
    {
        return cachedGameTextures; // todo: add validation
    }
}
