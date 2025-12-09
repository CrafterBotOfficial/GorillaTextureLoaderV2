using System.Linq;
using UnityEngine;

namespace GorillaTextureLoader.Map;

public class ForestTextureManager : IMapTextureManager
{
    public const string TEXTURE_KEY ="_BaseMap_Atlas";

    public IMapTextureManager Setup()
    {
        return this;
    }

    public void SetTextures(TexturePack pack)
    {
        Main.Log("Searching for materials");
        var textures =
            GameObject.FindObjectsByType<MeshRenderer>(sortMode: FindObjectsSortMode.InstanceID)
            .Select(x => x.sharedMaterial)
            .Where(x => x.HasTexture(TEXTURE_KEY));

#if DEBUG
        // textures.ForEach(x => Main.Log(x.GetTexture(TEXTURE_KEY)?.name));
#endif

        textures
            .First(x => { try { return x.GetTexture(TEXTURE_KEY).name == "TexArrayAtlas_2048x2048_BC7_AllScenes"; } catch {return false; }})
            .SetTexture(TEXTURE_KEY, pack.Textures[EMap.Forest]["atlas"]);

        // GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest/UnityTempFile-b30532c97cdb26948aabe54dd18c2bf2 (combined by EdMeshCombiner)")
        //     .GetComponent<MeshRenderer>().sharedMaterial
        //     .SetTexture("_BaseMap_Atlas", pack.Textures[EMap.Forest].First().Texture);
    }
}
