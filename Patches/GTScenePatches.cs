using HarmonyLib;

namespace GorillaTextureLoader.Patches;

[HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager))]
public static class GTScenePatches
{
    [HarmonyPatch(nameof(UnityEngine.SceneManagement.SceneManager.LoadScene), [typeof(string)])]
    [HarmonyPrefix]
    private static void LoadAsync(string sceneName)
    {
        Main.Log("Load scenejh " + sceneName, BepInEx.Logging.LogLevel.Message);
        // Main.Log("Loading scene "  + __instance.name);
    }
}
