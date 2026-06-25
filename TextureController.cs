using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GorillaTextureLoader.Loader;
using UnityEngine;

namespace GorillaTextureLoader;

// Singleton class to control all texture related operations
// The actual texture applier is a different thing, although both share a bunch of methods
public class TextureController
{
    private static Lazy<TextureController> instance = new Lazy<TextureController>(() => new TextureController());
    public static TextureController Instance => instance.Value;

    public Task<TexturePackMeta[]> PackMetas;
    public TexturePackMeta Current;

    private readonly ILoader loaderV2 = new LoaderV2();
    private TexturePackMeta[] metadatas;

    private TextureCache textureCache;

    public void Initialize()
    {
        new GameObject().AddComponent<ExtractTemplate>(); // todo: move
        NetworkSystem.Instance.OnJoinedRoomEvent += () =>
        {
            if (!InModdedRoom()) UnloadPack();
        }; // do not load if unable to stop unmodded

        PackMetas = LoadAllPackMetasAsync();
        PackMetas.ContinueWith(t =>
        {
            Main.Log($"Loaded {t.Result.Length} path metas", BepInEx.Logging.LogLevel.Message);
            MainPage.Instance.Items = [.. t.Result];
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

        textureCache = new TextureCache();
    }

    public async Task<bool> LoadPack(TexturePackMeta meta)
    {
        Main.Log($"Loading pack {meta.Id}");
        UnloadPack();
        if (!meta.IsVerified)
        {
            Main.Log("Pack univerified");
            if (!InModdedRoom())
            {
                Main.Log("Pack not allowed in unmodded rooms", BepInEx.Logging.LogLevel.Warning);
                return false;
            }
        }

#if DEBUG
        var watch = Stopwatch.StartNew();
#endif

        var applier = new TextureApplier(textureCache);
        Main.Log($"Applying texture {(meta.ForceNew ? "New" : "Slice")}"); // force new not yet implimented fully
        applier.Start(await meta.LoadTask);

#if DEBUG
        watch.Stop();
        Main.Log($"Finished in {watch.Elapsed.Seconds} {watch.Elapsed.Milliseconds}ms");
#endif

        Current = meta;
        return true;
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
            string key = isAtlas ? Paths.MAIN_ATLAS_KEY : "_BaseMap";
            Main.Log($"Trying to revert {texturePair.Value} {isAtlas} {key}");
            var materials = textureCache.FindMaterialByTextureName(texturePair.Key, key);
            foreach (var material in materials)
            {
                material.SetTexture(key, texturePair.Value);
            }
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
    private bool InModdedRoom()
    {
        var networkSystem = NetworkSystem.Instance;
        return !networkSystem.InRoom || networkSystem.GameModeString.StartsWith("MODDED_");
    }

    public static TextureCache GetTextureCache()
    {
        return Instance.textureCache;
    }
}
