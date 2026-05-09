using BepInEx;
using BepInEx.Logging;
using MonkeNotificationLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaTextureLoader;

[BepInPlugin("crafterbot.dumbmonkegame.textureloader", "TextureLoader", "2.0.0")]
[BepInDependency("crafterbot.gorillatag.computer", "1.1.0")]
[BepInDependency("crafterbot.notificationlib", "1.1.0")]
public class Main : BaseUnityPlugin
{
    public static Main Instance;

    private INotifier notifier;

    private void Awake()
    {
        Instance = this;
        notifier = new Notifier("TextureLoader");
        Configuration.Initialize(Config);
        GorillaTagger.OnPlayerSpawned(() =>
        {
            TextureController.Instance.Initialize();
            RemapManager.Instance.Initialize();
        });

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

    public static void Notify(string message, bool isError = false, bool isWarning = false)
    {
        Log(message, isError ? LogLevel.Error : isWarning ? LogLevel.Warning : LogLevel.Info);
        if (isError) Instance.notifier.Error(message);
        else if (isWarning) Instance.notifier.Warning(message);
        else Instance.notifier.Message(message);
    }
}
