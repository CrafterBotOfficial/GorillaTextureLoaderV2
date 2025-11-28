using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GorillaExtensions;
using UnityEngine;

namespace GorillaTextureLoader.Map;

public class ForestTextureManager : IMapTextureManager
{
    private MeshRenderer[] renderers;
    private Dictionary<Renderer, Material> defaultMaterials;

    private static string[] Exclude = [
        // "UnityTempFile-480b2104004819f43ad1122c7572395a (combined by EdMeshCombiner)",
        "UnityTempFile-9a5858b23dfbd474c8781a52a487093a (combined by EdMeshCombiner)",
        // "UnityTempFile-81229c4f07a4ec04cb7d54c66e14cebb (combined by EdMeshCombiner)",
        // "/TreeRoom/UnityTempFile-eba81af49b293a34a91823081c5283b3 (combined by EdMeshCombiner)",
        // "UnityTempFile-b30532c97cdb26948aabe54dd18c2bf2 (combined by EdMeshCombiner)",
    ];

    public IMapTextureManager Setup()
    {
        defaultMaterials = new();
        var targets = GameObject.Find("Environment Objects/LocalObjects_Prefab/").GetComponentsInChildren<MeshRenderer>().Where(IsForestRenderer).ToList();
        // Environment Objects/LocalObjects_Prefab/Forest/Terrain/newTreehouse/
        targets.Add(GameObject.Find("newTreehouse").GetComponent<MeshRenderer>());
        renderers = targets.ToArray();
        return this;
    }

    public void SetTextures(TexturePack pack)
    {
        // if (pack.Textures.Any(x => x.Value is null)) {
        //     Main.Log("Texture pack not yet loaded", BepInEx.Logging.LogLevel.Error);
        //     return;
        // }

        foreach (var renderer in renderers)
        {
            if (renderer?.material is null || Exclude.Any(x => renderer.gameObject.GetPath().Contains(x)))
                continue;

            var texture = pack.Textures[EMap.Forest];
            string name = GetTextureForObject(renderer.gameObject.name, renderer);

            if (!defaultMaterials.ContainsKey(renderer))
                defaultMaterials.Add(renderer, new Material(renderer.material));

            renderer.material?.SetTexture("_BaseMap_Atlas", texture.Where(x => Regex.Match(x.Name, name).Success).First().Texture);
        }
    }

    // TODO: Remove hardcoded names or find away to identify each one at runtime
    private string GetTextureForObject(string obj, Renderer renderer) {
        var dict = new Dictionary<string, string>() {
            { "UnityTempFile-eba81af49b293a34a91823081c5283b3 (combined by EdMeshCombiner)", "ground" },
            { "/TreeRoom/UnityTempFile-b30532c97cdb26948aabe54dd18c2bf2 (combined by EdMeshCombiner)", "treestumproom" }, // Note:  For some reason the wall renderer *always* has the same name as the tree stump
        };
        
        // todo make one big regex string
        foreach (var pair in dict) {
            if (pair.Key == obj) 
                return pair.Value;
            if (renderer.gameObject.GetPath().Contains(pair.Key)) {

                // As of now both the inner and outer treestump have hte same name, the first one is the inner, second is the outer, so a simple fix to ensure they arent mixedup
                var find = GameObject.Find("UnityTempFile-b30532c97cdb26948aabe54dd18c2bf2 (combined by EdMeshCombiner)");
                if (find != renderer.gameObject) return "treestump";

                return pair.Value;
            }
        }

        return "atlas";
    }

    private bool IsForestRenderer(MeshRenderer renderer) =>
        Regex.IsMatch(renderer.gameObject.name, "UnityTempFile-*");
}
