using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace GorillaTextureLoader;

[BepInPlugin("crafterbot.dumbmonkegame.textureloader", "TextureLoader", "2.2.0")]
[BepInDependency("tonimacaroni.computerinterface", "2.0.1")]
public class Main : BaseUnityPlugin
{
    public static Main Instance;

    private void Awake()
    {
        Instance = this;
        Configuration.Initialize(Config);
        _ = RemapManager.Instance;
        GorillaTagger.OnPlayerSpawned(() =>
        {
            new GameObject("TextureLoader", typeof(TextureController), typeof(UpdateChecker));
        });

#if DEBUG
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += async (scene, _) =>
        {
            await Task.Delay(5000);
            if (scene.name == "GorillaTag")
                GorillaTagger.OnPlayerSpawned(DumpTextures.DoDump);
        };
#endif
    }

#if DEBUG
    private void OnGUI()
    {
        if (TextureController.Instance?.PackMetas is null || !TextureController.Instance.PackMetas.IsCompleted) return;

        if (GUILayout.Button("Clear Weather"))
        {
            BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.None);
            BetterDayNightManager.instance.SetTimeOfDay(4);
        }
        if (GUILayout.Button("Reset"))
            TextureController.Instance.UnloadPack();
        foreach (var pack in TextureController.Instance.PackMetas.Result)
            if (GUILayout.Button(pack.Name))
            {
                TextureController.Instance.LoadPack(pack).ContinueWith(task =>
                 {
                     Log(task.Exception, BepInEx.Logging.LogLevel.Error);
                 }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            }
    }
#endif

    public static void Log(object message, LogLevel level = LogLevel.Info)
    {
        Instance.Logger.Log(level, message);
    }
}
