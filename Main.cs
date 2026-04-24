using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilla.Attributes;

namespace GorillaTextureLoader;

[BepInPlugin("crafterbot.dumbmonkegame.textureloader", "TextureLoader", "2.0.0")]
[BepInDependency("org.legoandmars.gorillatag.utilla", "1.6.0")]
[BepInDependency("crafterbot.gorillatag.computer", "1.1.0")]
[ModdedGamemode]
public class Main : BaseUnityPlugin
{
    private static Main instance;

    private void Awake()
    {
        instance = this;
        TextureController.Instance.Initialize();

#if DEBUG
        SceneManager.sceneLoaded += async (scene, _) =>
        {
            await System.Threading.Tasks.Task.Delay(5000);
            if (scene.name == "GorillaTag")
                GorillaTagger.OnPlayerSpawned(DumpTextures.DoDump);
        };
#endif
    }

#if DEBUG
    private void OnGUI()
    {
        if (!TextureController.Instance.PackMetas.IsCompleted) return;

        if (GUILayout.Button("Reset"))
            TextureController.Instance.UnloadPack();
        foreach (var pack in TextureController.Instance.PackMetas.Result)
            if (GUILayout.Button(pack.Name))
            {
                TextureController.Instance.LoadPack(pack);
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
