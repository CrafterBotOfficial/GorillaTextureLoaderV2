using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader;

// Responicble for keeping track of original game textures, and remembering already loaded packs
public class TextureCache
{
    private readonly Dictionary<string, Texture2DArray> cachedGameTextures = [];
    private readonly Dictionary<string, MeshRenderer[]> cachedMeshRenderers = [];
    private readonly Dictionary<TexturePackMeta, Dictionary<string, Texture2DArray>> cachedTexturePacks = [];

    public bool TryGetTexturePack(TexturePackMeta meta, out Dictionary<string, Texture2DArray> textures)
    {
        return cachedTexturePacks.TryGetValue(meta, out textures);
    }

    public void CacheTexturePack(TexturePackMeta meta, Dictionary<string, Texture2DArray> namedTextures)
    {
        cachedTexturePacks[meta] = namedTextures;
    }

    public MeshRenderer[] FindRenderersByTextureName(string name)
    {
        if (cachedMeshRenderers.TryGetValue(name, out var renderers))
            return renderers;

        renderers = [..GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.None)
            .Where(x => x.sharedMaterial is not null && x.sharedMaterial.HasTexture(Paths.MAIN_ATLAS_KEY))
            .Where(x => x.sharedMaterial.GetTexture(Paths.MAIN_ATLAS_KEY)?.name == name)];
        cachedMeshRenderers.Add(name, renderers);
        return renderers;
    }

    public Material[] FindMaterialByTextureName(string name)
    {
        var materials = FindRenderersByTextureName(name).Select(x => x.sharedMaterial).ToArray();
        return materials;
    }

    public Texture[] FindTextureByName(string name)
    {
        return [.. FindMaterialByTextureName(name).Select(x => x.GetTexture(Paths.MAIN_ATLAS_KEY))];
    }

    public void CacheGameTextures(string textureName, Texture2DArray atlas)
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

    public Dictionary<string, Texture2DArray> GetOriginalGameTextures()
    {
        return cachedGameTextures; // todo: add validation
    }
}
