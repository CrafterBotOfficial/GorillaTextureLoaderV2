using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GorillaNetworking;
using GorillaTextureLoader.Loader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaTextureLoader;

// Singleton class to control all texture related operations
// The actual texture applier is a different thing, although both share a bunch of methods
public class TextureController : MonoBehaviour
{
    public static TextureController Instance;

    public Task<TexturePackMeta[]> PackMetas;
    private TexturePackMeta current;
    public TexturePackMeta Current
    {
        get => current;
        set
        {
            current = value;
            Configuration.CurrentTexturePack.Value = value?.Id ?? string.Empty;
        }
    }

    private readonly ILoader loaderV2 = new LoaderV2();
    private TexturePackMeta[] metadatas;

    private TextureCache textureCache;

    private void Awake()
    {
        if (Instance is not null)
        {
            GameObject.Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gameObject.AddComponent<ExtractTemplate>();
        NetworkSystem.Instance.OnJoinedRoomEvent += () =>
        {
            if (!Current.IsVerified && !InModdedRoom())
                UnloadPack();
        }; // do not load if unable to stop unmodded

        PackMetas = LoadAllPackMetasAsync();
        PackMetas.ContinueWith(t =>
        {
            Main.Log($"Loaded {t.Result.Length} path metas", BepInEx.Logging.LogLevel.Message);
            MainPage.Instance.Items = [.. t.Result];
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

        textureCache = new TextureCache(true);
        AutoLoad();
    }

    public async Task LoadPack(TexturePackMeta meta)
    {
        Main.Log($"Loading pack {meta.Id}");
        UnloadPack();
        if (!meta.IsVerified)
        {
            Main.Log("Pack univerified");
            if (!InModdedRoom())
            {
                Main.Log("Pack not allowed in unmodded rooms", BepInEx.Logging.LogLevel.Warning);
                throw new Exception("Pack not allowed in this room.");
            }
        }

#if DEBUG
        var watch = Stopwatch.StartNew();
#endif

        var applier = new TextureApplier(textureCache);
        applier.Start(await meta.LoadTask);

#if DEBUG
        watch.Stop();
        Main.Log($"Finished in {watch.Elapsed.Seconds} {watch.Elapsed.Milliseconds}ms");
#endif

        Current = meta;
        return;
    }

    public void UnloadPack()
    {
        if (Current is null)
            return;
        Main.Log("Reset");
        Current = null;
        foreach (var texturePair in textureCache.GetOriginalGameTextures())
        {
            bool isAtlas = texturePair.Value is Texture2DArray;
            string key = isAtlas ? Paths.MAIN_ATLAS_KEY : Paths.MAIN_KEY;
            Main.Log($"Trying to revert {texturePair.Value} {isAtlas} {key}");
            var materials = textureCache.FindMaterialByTextureName(texturePair.Key, key);
            foreach (var material in materials)
            {
                material.SetTexture(key, texturePair.Value);
            }
        }
    }

    private async void AutoLoad()
    {
        while (!GorillaTagger.Instance || !GorillaTagger.Instance.rigidbody || !GorillaComputer.instance || !GorillaComputer.instance.initialized) // ensure graphics device is initialzied to avoid MORE crashing >:(
        {
            await Task.Delay(50);
        }

        var metas = await PackMetas;
        if (Configuration.EnableAutoLoad.Value && Configuration.CurrentTexturePack.Value != string.Empty)
        {
            Main.Log("Auto loading last saved texturepack");
            var meta = metas.FirstOrDefault(x => x.Id == Configuration.CurrentTexturePack.Value);
            if (meta is not null && meta.IsVerified) await LoadPack(meta);
        }
    }


    // Todo: Add legacy loader, if reasonably possible
    public async Task<TexturePackMeta[]> LoadAllPackMetasAsync()
    {
        if (metadatas is not null) return metadatas;
        var result = await loaderV2.LoadAllMetadatas();
        metadatas = result;
        return result;
    }

    // To allow verified packs to work without utilla
    // and supports both Utilla and GroillaLibaray
    private bool InModdedRoom() =>
        !NetworkSystem.Instance.InRoom || NetworkSystem.Instance.GameModeString.StartsWith("MODDED_");

    public static TextureCache GetTextureCache()
    {
        return Instance?.textureCache ?? new TextureCache(false);
    }
}
