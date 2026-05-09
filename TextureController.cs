using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        PackMetas.ContinueWith(_ =>
        {
            // if (Jerald.PageManager.Instance.GetPage() is MainPage) MainPage.Update(); // ensure page updates when all packs are loaded

            if (GorillaTagger.Instance.offlineVRRig is not null) AutoLoadPack();
            else GorillaTagger.OnPlayerSpawned(AutoLoadPack);
        });

        textureCache = new TextureCache();
        _ = RemapManager.Instance.GetRemaps(); // cache web response
    }

    private void AutoLoadPack()
    {
        if (PackMetas.Result.FirstOrDefault(x => x.Id == Configuration.CurrentTexturePack.Value) is TexturePackMeta meta && meta.IsVerified)
        {
            LoadPack(meta).ContinueWith(task =>
            {
                Main.Log(task.Exception, BepInEx.Logging.LogLevel.Error);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    public async Task LoadPack(TexturePackMeta meta)
    {
        UnloadPack();
        if (!meta.IsVerified)
        {
            Main.Log("Pack univerified");
            if (!InModdedRoom())
            {
                Main.Log("Pack not allowed in unmodded rooms", BepInEx.Logging.LogLevel.Warning);
                return;
            }
        }

        Main.Notify("Loading pack...");
        await Task.Delay(500); // jank

#if DEBUG
        var watch = Stopwatch.StartNew();
#endif

        var applier = new TextureApplier(textureCache);
        if (Configuration.EnableCaching.Value && textureCache.TryGetTexturePack(meta, out Dictionary<string, Texture2DArray> textures))
        {
            Main.Log("Loading textures from cache");
            applier.Start(textures);
        }
        else
        {
            Main.Log($"Applying texture {(meta.ForceNew ? "New" : "Slice")}"); // force new not yet implimented fully
            applier.Start(meta, loaderV2.LoadPack(meta));
        }

#if DEBUG
        watch.Stop();
        Main.Log($"Finished in {watch.Elapsed.Seconds} {watch.Elapsed.Milliseconds}ms");
#endif
        Current = meta;
        Main.Notify("Loaded " + Current.Name);
    }

    public void UnloadPack()
    {
        if (Current is null)
            return;
        Main.Log("Reset");
        Current = null;
        foreach (var texturePair in textureCache.GetOriginalGameTextures())
        {
            var materials = textureCache.FindMaterialByTextureName(texturePair.Key);
            foreach (var material in materials) material.SetTexture("_BaseMap_Atlas", texturePair.Value);
        }
    }


    public Texture2DArray CreateTextureArray(Texture2D[] textures)
    {
        var slice0 = textures[0];
        Main.Log($"Creating array with {textures.Length} slices {slice0.width}x{slice0.height} pixels", BepInEx.Logging.LogLevel.Debug);
        var textureArray = new Texture2DArray(
            slice0.width,
            slice0.height,
            textures.Length,
            slice0.format,
            false,
            false
        );

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i].width != slice0.width || textures[i].height != slice0.height)
            {
                Main.Log("Invalid textrurepack", BepInEx.Logging.LogLevel.Error);
                break;
            }
            Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
        }

        textureArray.filterMode = FilterMode.Point;
        textureArray.Apply(false, true);
        return textureArray;
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
    private bool InModdedRoom()
    {
        var networkSystem = NetworkSystem.Instance;
        return !networkSystem.InRoom || networkSystem.GameModeString.StartsWith("MODDED_");
    }

    public static TextureCache GetTextureCache()
    {
        if (Instance?.textureCache is null) Main.Log("Texture cache not yet initialized", BepInEx.Logging.LogLevel.Warning); // todo verify not called and delete
        return Instance?.textureCache ?? new TextureCache();
    }
}
