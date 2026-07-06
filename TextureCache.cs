using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

// Responicble for keeping track of original game textures, and remembering already loaded packs
public class TextureCache(bool EnableGameCaching)
{
    private readonly Dictionary<string, Texture> cachedGameTextures = [];
    private readonly Dictionary<string, Material[]> cachedMaterials = [];

    /// <summary> Some materials may be null</summary>
    public Material[] FindMaterialsByTextureName<T>(string name, string textureKey = Paths.MAIN_ATLAS_KEY) where T : Texture
    {
        if (cachedMaterials.TryGetValue(name, out var cache))
            return cache;

        var sharedMaterials = new List<Material>();
        var propertyId = Shader.PropertyToID(textureKey);
        foreach (var meshRenderer in GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.None))
        {
            if (meshRenderer.sharedMaterial is null || !meshRenderer.sharedMaterial.HasProperty(propertyId)) continue;

            var texture = meshRenderer.sharedMaterial.GetTexture(propertyId);
            if (texture is null || texture.name != name) continue;
            if (texture.GetType() != typeof(T)) continue;

            if (sharedMaterials.Contains(meshRenderer.sharedMaterial)) continue;
            sharedMaterials.Add(meshRenderer.sharedMaterial);
        }

        Main.Log($"Caching {sharedMaterials.Count} items", BepInEx.Logging.LogLevel.Debug);
        var array = sharedMaterials.ToArray();
        cachedMaterials.Add(name, array);

        return array;
    }

    public T[] FindTexturesByName<T>(string name, string key = Paths.MAIN_ATLAS_KEY) where T : Texture
    {
        return [.. FindMaterialsByTextureName<T>(name, key).Select(x => x.GetTexture(key) as T)];
    }

    public void CacheGameTextures(string textureName, Texture texture)
    {
        if (!EnableGameCaching || cachedGameTextures.ContainsKey(textureName) || texture is null)
            return;
        cachedGameTextures.Add(textureName, texture);
    }

    public Dictionary<string, Texture> GetOriginalGameTextures()
    {
        return cachedGameTextures; // todo: add validation
    }
}
