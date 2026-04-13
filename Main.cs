using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Utilla.Attributes;

namespace GorillaTextureLoader;

[BepInPlugin("crafterbot.dumbmonkegame.textureloader", "TextureLoader", "2.0.0")]
[BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
[ModdedGamemode]
public class Main : BaseUnityPlugin
{
    private static Main instance;

    private void Awake()
    {
        instance = this;
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(Main).Assembly);
    }

#if DEBUG
    private Task<TexturePackMeta[]> packMetas;

    private void Start()
    {
        packMetas = TextureController.Instance.LoadAllPackMetasAsync();
    }

    private void OnGUI()
    {
        if (!packMetas.IsCompleted) return;
        foreach (var pack in packMetas.Result)
            if (GUILayout.Button(pack.Name))
            {
                (_, var combined) = new Loader.LoaderV2().LoadPack(pack);

                GameObject.Find("UnityTempFile-569358720e8bdbe48a564ee0113f0542 (combined by EdMeshCombiner)").GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["forestatlas"]);
                GameObject.Find("UnityTempFile-201cbd57f079d244fa831efae4c2e050 (combined by EdMeshCombiner)").GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
                GameObject.Find("UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)").GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_BaseMap_Atlas", combined["pitground"]);
                // UnityTempFile-ff72814c43f9a964289644d8b38df711 (combined by EdMeshCombiner)
            }
    }
#endif

    [ModdedGamemodeJoin]
    private void OnJoin()
    {
    }

    [ModdedGamemodeLeave]
    private void OnLeave()
    {
    }

    public static void Log(object message, LogLevel level = LogLevel.Info)
    {
        instance.Logger.Log(level, message);
    }
}
