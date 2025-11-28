using BepInEx;
using BepInEx.Logging;
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
        // Configuration.Initialize(Config);
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(Main).Assembly);

        Utilla.Events.GameInitialized += async (_, _) =>
        {
            TextureController.Instance.LoadTexture();
        };
    }

    private void Update() { }

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
        instance?.Logger.Log(level, message);
    }
}
