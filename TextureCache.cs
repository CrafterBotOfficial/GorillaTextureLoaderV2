using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

// Responicble for keeping track of original game textures, and remembering already loaded packs
public class TextureCache(bool EnableGameCaching)
{
    private readonly Dictionary<string, Texture> cachedGameTextures = [];
    private readonly Dictionary<string, MeshRenderer[]> cachedMeshRenderers = [];

    public MeshRenderer[] FindRenderersByTextureName(string name, string textureKey)
    {
        if (cachedMeshRenderers.TryGetValue(name, out var cachedRenderers))
            return cachedRenderers;

        var result = new List<MeshRenderer>();
        var propertyId = Shader.PropertyToID(textureKey);
        foreach (var meshRenderer in GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.None))
        {
            if (meshRenderer.sharedMaterial is null || !meshRenderer.sharedMaterial.HasProperty(propertyId)) continue;

            var texture = meshRenderer.sharedMaterial.GetTexture(propertyId);
            if (texture is not null && texture.name != name) continue;

            result.Add(meshRenderer);
        }

        var array = result.ToArray();
        cachedMeshRenderers.Add(name, array);

        return array;
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
        if (!EnableGameCaching || cachedGameTextures.ContainsKey(textureName) || texture is null)
            return;
        cachedGameTextures.Add(textureName, texture);
    }

    public void CacheGameTextures(string textureName, Texture2DArray atlas)
    {
        if (!EnableGameCaching || cachedGameTextures.ContainsKey(textureName) || atlas is null)
            return;
        cachedGameTextures.Add(textureName, atlas);
    }

    public Dictionary<string, Texture> GetOriginalGameTextures()
    {
        return cachedGameTextures; // todo: add validation
    }
}
